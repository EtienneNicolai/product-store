import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../services/api.service';
import { Cart as CartModel } from '../services/models';
import { extractErrorMessage } from '../services/error';
import { formatPrice } from '../services/format';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './cart.html',
  styleUrl: './cart.scss',
})
export class Cart implements OnInit {
  private readonly api = inject(ApiService);

  protected readonly cart = signal<CartModel | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly busyItemId = signal<number | null>(null);

  protected readonly totalCents = computed(() =>
    (this.cart()?.items ?? []).reduce((sum, item) => sum + item.quantity * item.product.priceCents, 0),
  );

  protected readonly formatPrice = formatPrice;

  ngOnInit(): void {
    this.loadCart();
  }

  private loadCart(): void {
    this.loading.set(true);
    this.api.getCart().subscribe({
      next: (cart) => {
        this.cart.set(cart);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(extractErrorMessage(err, 'Could not load your cart.'));
        this.loading.set(false);
      },
    });
  }

  updateQuantity(itemId: number, quantity: number): void {
    if (quantity < 1) return;

    this.busyItemId.set(itemId);
    this.error.set(null);

    this.api.updateCartItem(itemId, quantity).subscribe({
      next: (cart) => {
        this.cart.set(cart);
        this.busyItemId.set(null);
      },
      error: (err) => {
        this.busyItemId.set(null);
        this.error.set(extractErrorMessage(err, 'Could not update that item.'));
      },
    });
  }

  removeItem(itemId: number): void {
    this.busyItemId.set(itemId);
    this.error.set(null);

    this.api.removeCartItem(itemId).subscribe({
      next: (cart) => {
        this.cart.set(cart);
        this.busyItemId.set(null);
      },
      error: (err) => {
        this.busyItemId.set(null);
        this.error.set(extractErrorMessage(err, 'Could not remove that item.'));
      },
    });
  }
}
