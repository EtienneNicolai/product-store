import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../services/api.service';
import { Product } from '../services/models';
import { extractErrorMessage } from '../services/error';
import { formatPrice } from '../services/format';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.scss',
})
export class ProductDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ApiService);

  protected readonly product = signal<Product | null>(null);
  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly quantity = signal(1);
  protected readonly adding = signal(false);
  protected readonly added = signal(false);

  protected readonly formatPrice = formatPrice;

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug');
    if (!slug) {
      this.notFound.set(true);
      this.loading.set(false);
      return;
    }

    this.api.getProduct(slug).subscribe({
      next: (product) => {
        this.product.set(product);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (err.status === 404) {
          this.notFound.set(true);
        } else {
          this.error.set(extractErrorMessage(err, 'Could not load this product.'));
        }
      },
    });
  }

  addToCart(): void {
    const product = this.product();
    if (!product) return;

    this.adding.set(true);
    this.added.set(false);
    this.error.set(null);

    this.api.addCartItem(product.id, this.quantity()).subscribe({
      next: () => {
        this.adding.set(false);
        this.added.set(true);
      },
      error: (err) => {
        this.adding.set(false);
        this.error.set(extractErrorMessage(err, 'Could not add that item to your cart.'));
      },
    });
  }
}
