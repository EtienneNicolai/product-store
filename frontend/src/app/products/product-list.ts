import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../services/api.service';
import { ProductSummary } from '../services/models';
import { extractErrorMessage } from '../services/error';
import { formatPrice } from '../services/format';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './product-list.html',
  styleUrl: './product-list.scss',
})
export class ProductList implements OnInit {
  private readonly api = inject(ApiService);

  protected readonly products = signal<ProductSummary[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly addingId = signal<number | null>(null);
  protected readonly addedId = signal<number | null>(null);

  ngOnInit(): void {
    this.api.getProducts().subscribe({
      next: (products) => {
        this.products.set(products);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(extractErrorMessage(err, 'Could not load products.'));
        this.loading.set(false);
      },
    });
  }

  quickAdd(product: ProductSummary): void {
    this.addingId.set(product.id);
    this.addedId.set(null);
    this.api.addCartItem(product.id, 1).subscribe({
      next: () => {
        this.addingId.set(null);
        this.addedId.set(product.id);
      },
      error: (err) => {
        this.addingId.set(null);
        this.error.set(extractErrorMessage(err, 'Could not add that item to your cart.'));
      },
    });
  }

  protected readonly formatPrice = formatPrice;
}
