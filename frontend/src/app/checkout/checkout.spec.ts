import { TestBed, ComponentFixture } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import { Checkout } from './checkout';
import { ApiService } from '../services/api.service';

// loadStripe is a free function import, not an injectable - mocking the
// module is the only way to keep this test from trying to load real
// Stripe.js (and failing, since no publishable key exists in this
// environment - see CLAUDE.md and guideline 06's completion notes).
vi.mock('@stripe/stripe-js', () => ({
  loadStripe: vi.fn().mockResolvedValue({
    elements: vi.fn().mockReturnValue({
      create: vi.fn().mockReturnValue({
        mount: vi.fn(),
        on: vi.fn(),
      }),
    }),
    confirmPayment: vi.fn(),
  }),
}));

describe('Checkout', () => {
  let fixture: ComponentFixture<Checkout>;
  let component: Checkout;
  let createPaymentIntent: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    createPaymentIntent = vi.fn();

    await TestBed.configureTestingModule({
      imports: [Checkout],
      providers: [{ provide: ApiService, useValue: { createPaymentIntent } }],
    }).compileComponents();

    fixture = TestBed.createComponent(Checkout);
    component = fixture.componentInstance;
  });

  it('does not call the API when the email field is empty', () => {
    component.email.set('');

    component.submitEmail();

    expect(createPaymentIntent).not.toHaveBeenCalled();
    expect(component.error()).toBeTruthy();
  });

  it('calls createPaymentIntent with the entered email and moves to the payment stage on success', () => {
    createPaymentIntent.mockReturnValue(of({ clientSecret: 'pi_secret', totalCents: 1000, orderId: 42 }));
    component.email.set('test@example.com');

    component.submitEmail();

    expect(createPaymentIntent).toHaveBeenCalledWith('test@example.com');
    expect(component.stage()).toBe('payment');
    expect(component.error()).toBeNull();
  });

  it('shows the backend error and stays on the email stage when createPaymentIntent fails', () => {
    createPaymentIntent.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 500,
            error: { detail: 'Stripe is not configured: Stripe:SecretKey is missing.' },
          }),
      ),
    );
    component.email.set('test@example.com');

    component.submitEmail();

    expect(component.stage()).toBe('email');
    expect(component.error()).toContain('Stripe is not configured');
    expect(component.submittingEmail()).toBe(false);
  });
});
