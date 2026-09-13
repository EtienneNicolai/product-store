// Mirrors the backend's actual JSON shapes exactly (Store.Api/Dtos and the
// Product entity's default camelCase serialization) - see guideline 01's API
// Contracts section and Store.Api/Controllers for the source of truth.

export interface ProductSummary {
  id: number;
  slug: string;
  name: string;
  description: string;
  priceCents: number;
  currency: string;
  imageUrl: string;
  stockQuantity: number;
}

export interface Product extends ProductSummary {
  isActive: boolean;
}

export interface CartItemProduct {
  name: string;
  priceCents: number;
}

export interface CartItem {
  id: number;
  productId: number;
  quantity: number;
  product: CartItemProduct;
}

export interface Cart {
  id: number;
  items: CartItem[];
}

export interface CreatePaymentIntentResponse {
  clientSecret: string;
  totalCents: number;
  orderId: number;
}

export interface OrderItem {
  productId: number;
  productName: string;
  unitPriceCents: number;
  quantity: number;
}

export interface Order {
  id: number;
  status: 'pending' | 'paid' | 'failed';
  totalCents: number;
  customerEmail: string;
  createdAt: string;
  items: OrderItem[];
}
