import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Cart, CreatePaymentIntentResponse, Order, Product, ProductSummary } from './models';

// Every HTTP call the app makes goes through here - no component calls
// HttpClient directly (guideline 06). withCredentials is required on every
// request since the cart is identified by a session cookie, not a token.
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  getProducts(): Observable<ProductSummary[]> {
    return this.http.get<ProductSummary[]>(`${this.baseUrl}/api/products`, { withCredentials: true });
  }

  getProduct(slug: string): Observable<Product> {
    return this.http.get<Product>(`${this.baseUrl}/api/products/${slug}`, { withCredentials: true });
  }

  getCart(): Observable<Cart> {
    return this.http.get<Cart>(`${this.baseUrl}/api/cart`, { withCredentials: true });
  }

  addCartItem(productId: number, quantity: number): Observable<Cart> {
    return this.http.post<Cart>(
      `${this.baseUrl}/api/cart/items`,
      { productId, quantity },
      { withCredentials: true },
    );
  }

  updateCartItem(itemId: number, quantity: number): Observable<Cart> {
    return this.http.put<Cart>(
      `${this.baseUrl}/api/cart/items/${itemId}`,
      { quantity },
      { withCredentials: true },
    );
  }

  removeCartItem(itemId: number): Observable<Cart> {
    return this.http.delete<Cart>(`${this.baseUrl}/api/cart/items/${itemId}`, { withCredentials: true });
  }

  createPaymentIntent(email: string): Observable<CreatePaymentIntentResponse> {
    return this.http.post<CreatePaymentIntentResponse>(
      `${this.baseUrl}/api/checkout/create-payment-intent`,
      { email },
      { withCredentials: true },
    );
  }

  getOrder(id: number): Observable<Order> {
    return this.http.get<Order>(`${this.baseUrl}/api/orders/${id}`, { withCredentials: true });
  }
}
