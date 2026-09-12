import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../services/api.service';
import { Order } from '../services/models';
import { extractErrorMessage } from '../services/error';
import { formatPrice } from '../services/format';

const POLL_INTERVAL_MS = 2000;
const MAX_POLL_ATTEMPTS = 10;

@Component({
  selector: 'app-order-confirmation',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './order-confirmation.html',
  styleUrl: './order-confirmation.scss',
})
export class OrderConfirmation implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ApiService);

  protected readonly order = signal<Order | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  protected readonly formatPrice = formatPrice;

  private pollTimeout: ReturnType<typeof setTimeout> | undefined;
  private attempts = 0;

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    const id = idParam ? Number(idParam) : NaN;

    if (!idParam || Number.isNaN(id)) {
      this.error.set('Invalid order.');
      this.loading.set(false);
      return;
    }

    this.fetchOrder(id);
  }

  ngOnDestroy(): void {
    clearTimeout(this.pollTimeout);
  }

  private fetchOrder(id: number): void {
    this.api.getOrder(id).subscribe({
      next: (order) => {
        this.order.set(order);
        this.loading.set(false);

        // The webhook that marks an order "paid" runs asynchronously and can
        // arrive slightly after Stripe redirects the customer back here, so
        // poll briefly rather than showing a stale "pending" permanently.
        if (order.status === 'pending' && this.attempts < MAX_POLL_ATTEMPTS) {
          this.attempts += 1;
          this.pollTimeout = setTimeout(() => this.fetchOrder(id), POLL_INTERVAL_MS);
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(extractErrorMessage(err, 'Could not load this order.'));
      },
    });
  }
}
