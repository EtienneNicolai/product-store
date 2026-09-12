import { Component, ElementRef, Injector, afterNextRender, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { loadStripe, Stripe, StripeElements } from '@stripe/stripe-js';
import { ApiService } from '../services/api.service';
import { extractErrorMessage } from '../services/error';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './checkout.html',
  styleUrl: './checkout.scss',
})
export class Checkout {
  private readonly api = inject(ApiService);
  private readonly injector = inject(Injector);

  protected readonly paymentElementRef = viewChild<ElementRef<HTMLDivElement>>('paymentElement');

  protected readonly email = signal('');
  protected readonly stage = signal<'email' | 'payment'>('email');
  protected readonly submittingEmail = signal(false);
  protected readonly submittingPayment = signal(false);
  protected readonly paymentElementReady = signal(false);
  protected readonly error = signal<string | null>(null);
  // No real Stripe account exists in this environment yet - see CLAUDE.md and
  // guideline 06's completion notes. Shown as a real, honest state rather
  // than pretending checkout works end to end.
  protected readonly stripeConfigured = !!environment.stripePublishableKey;

  private stripe: Stripe | null = null;
  private elements: StripeElements | null = null;
  private orderId: number | null = null;

  submitEmail(): void {
    if (!this.email().trim()) {
      this.error.set('Enter an email address.');
      return;
    }

    this.submittingEmail.set(true);
    this.error.set(null);

    this.api.createPaymentIntent(this.email()).subscribe({
      next: (response) => {
        this.orderId = response.orderId;
        this.submittingEmail.set(false);
        this.stage.set('payment');

        // The #paymentElement container only exists once Angular renders the
        // 'payment' stage above - afterNextRender waits for exactly that
        // before Stripe.js tries to mount into it.
        afterNextRender(() => this.mountPaymentElement(response.clientSecret), {
          injector: this.injector,
        });
      },
      error: (err) => {
        this.submittingEmail.set(false);
        this.error.set(extractErrorMessage(err, 'Could not start checkout.'));
      },
    });
  }

  private async mountPaymentElement(clientSecret: string): Promise<void> {
    const stripe = await loadStripe(environment.stripePublishableKey);
    if (!stripe) {
      this.error.set('Could not load Stripe.js.');
      return;
    }

    const container = this.paymentElementRef()?.nativeElement;
    if (!container) {
      this.error.set('Payment form container is missing.');
      return;
    }

    this.stripe = stripe;
    this.elements = stripe.elements({ clientSecret });
    const paymentElement = this.elements.create('payment');
    paymentElement.mount(container);
    paymentElement.on('ready', () => this.paymentElementReady.set(true));
  }

  async confirmPayment(): Promise<void> {
    if (!this.stripe || !this.elements || this.orderId === null) return;

    this.submittingPayment.set(true);
    this.error.set(null);

    const { error } = await this.stripe.confirmPayment({
      elements: this.elements,
      confirmParams: {
        return_url: `${window.location.origin}/order-confirmation/${this.orderId}`,
      },
    });

    // This line is only reached if confirmation failed immediately (e.g. an
    // invalid card caught client-side). A successful payment, or one needing
    // 3D Secure, navigates the browser away to return_url instead - that's
    // the Payment Element's default behavior and guideline 06 says not to
    // fight it by trying to keep the customer on this page unconditionally.
    if (error) {
      this.submittingPayment.set(false);
      this.error.set(error.message ?? 'Payment failed. Please try again.');
    }
  }
}
