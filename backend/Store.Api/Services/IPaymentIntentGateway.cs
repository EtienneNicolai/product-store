using Stripe;

namespace Store.Api.Services;

// A thin wrapper around Stripe.net's PaymentIntentService, which has no
// interface of its own to mock in this Stripe.net version (52.4.2) - earlier
// versions exposed IPaymentIntentService, this one doesn't. Owning this
// abstraction ourselves also means tests aren't coupled to Stripe.net's
// specific service class shape at all.
public interface IPaymentIntentGateway
{
    Task<PaymentIntent> CreateAsync(PaymentIntentCreateOptions options, string apiKey, CancellationToken cancellationToken);
}

public class StripePaymentIntentGateway : IPaymentIntentGateway
{
    public Task<PaymentIntent> CreateAsync(PaymentIntentCreateOptions options, string apiKey, CancellationToken cancellationToken)
    {
        var service = new PaymentIntentService();
        return service.CreateAsync(options, new RequestOptions { ApiKey = apiKey }, cancellationToken);
    }
}
