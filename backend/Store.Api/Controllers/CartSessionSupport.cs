using Microsoft.EntityFrameworkCore;
using Store.Api.Data;
using Store.Api.Models;

namespace Store.Api.Controllers;

/// <summary>
/// Plain-cookie session handling shared by <see cref="CartController"/> and
/// <see cref="CheckoutController"/>. Deliberately not ASP.NET session-state
/// middleware - just an opaque id in a cookie, per CLAUDE.md ("Auth: none in
/// v1 - carts are anonymous, keyed by a session cookie").
/// </summary>
internal static class CartSession
{
    public const string CookieName = "session_id";

    public static string? ReadSessionId(HttpRequest request)
        => request.Cookies.TryGetValue(CookieName, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;

    public static string CreateSessionId() => Guid.NewGuid().ToString("N");

    // crossSite: true in every deployed environment - the frontend (Static
    // Site) and backend (Web Service) live on two different Render
    // subdomains, which is genuinely cross-SITE, not just cross-origin like
    // localhost:4200 vs localhost:5131 in local dev. A cross-site cookie
    // needs SameSite=None + Secure - browsers silently drop SameSite=Lax
    // cookies on cross-site fetch/XHR calls (they're only sent on top-level
    // navigation), which is exactly why the cart appeared to "forget"
    // itself on every page load once deployed. Secure=true requires HTTPS,
    // which local dev doesn't have, hence the flag rather than always-on.
    public static void SetSessionCookie(HttpResponse response, string sessionId, bool crossSite)
    {
        response.Cookies.Append(CookieName, sessionId, new CookieOptions
        {
            HttpOnly = true,
            SameSite = crossSite ? SameSiteMode.None : SameSiteMode.Lax,
            Secure = crossSite,
            IsEssential = true,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(30),
        });
    }
}

/// <summary>
/// Loads (or lazily creates) the <see cref="Cart"/> for the current request's
/// session cookie. Every cart and checkout endpoint goes through this so a
/// visitor who adds to cart or checks out without ever calling GET /api/cart
/// first still gets a session - guideline 04 only calls this out explicitly
/// for GET /api/cart, but the same behavior makes sense for every entry point.
/// </summary>
internal static class CartAccessor
{
    public static async Task<(Cart Cart, bool IsNewSession)> GetOrCreateCartAsync(
        StoreDbContext db, HttpRequest request, CancellationToken ct)
    {
        var sessionId = CartSession.ReadSessionId(request);
        var isNewSession = sessionId is null;
        sessionId ??= CartSession.CreateSessionId();

        var cart = await db.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId, ct);

        if (cart is null)
        {
            // Either a brand-new session, or a stale cookie whose cart row no
            // longer exists (e.g. local db was reset) - either way, start a
            // fresh cart under this session id.
            cart = new Cart { SessionId = sessionId, CreatedAt = DateTime.UtcNow };
            db.Carts.Add(cart);
            await db.SaveChangesAsync(ct);
        }

        return (cart, isNewSession);
    }
}
