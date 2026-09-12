using System.Net.Http.Json;

namespace Store.Api.Tests;

// Wraps HttpClient with curl-cookie-jar-style session handling: HttpClient
// itself doesn't persist cookies across requests, but every cart/checkout
// endpoint depends on the session_id cookie round-tripping the way a real
// browser would.
internal sealed class TestClient
{
    private readonly HttpClient _http;
    private string? _sessionCookie;

    public TestClient(HttpClient http) => _http = http;

    public Task<HttpResponseMessage> GetAsync(string url) => SendAsync(new HttpRequestMessage(HttpMethod.Get, url));

    public Task<HttpResponseMessage> PostAsync(string url, object body) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) });

    public Task<HttpResponseMessage> PutAsync(string url, object body) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Put, url) { Content = JsonContent.Create(body) });

    public Task<HttpResponseMessage> DeleteAsync(string url) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Delete, url));

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        if (_sessionCookie is not null)
        {
            request.Headers.Add("Cookie", _sessionCookie);
        }

        var response = await _http.SendAsync(request);

        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            var sessionSetCookie = setCookies.FirstOrDefault(c => c.StartsWith("session_id="));
            if (sessionSetCookie is not null)
            {
                _sessionCookie = sessionSetCookie.Split(';')[0];
            }
        }

        return response;
    }
}
