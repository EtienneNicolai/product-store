import { Routes } from '@angular/router';
import { ProductList } from './products/product-list';
import { ProductDetail } from './products/product-detail';
import { Cart } from './cart/cart';
import { Checkout } from './checkout/checkout';
import { OrderConfirmation } from './order-confirmation/order-confirmation';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'products' },
  { path: 'products', component: ProductList },
  { path: 'products/:slug', component: ProductDetail },
  { path: 'cart', component: Cart },
  { path: 'checkout', component: Checkout },
  { path: 'order-confirmation/:id', component: OrderConfirmation },
  { path: '**', redirectTo: 'products' },
];
