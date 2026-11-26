import { Routes } from '@angular/router';
import { authGuard } from './core/guards';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/home').then(m => m.HomeComponent), title: 'ShopSphere' },
  { path: 'products', loadComponent: () => import('./pages/products').then(m => m.ProductsComponent), title: 'Shop | ShopSphere' },
  { path: 'products/:id', loadComponent: () => import('./pages/product-detail').then(m => m.ProductDetailComponent) },
  { path: 'login', loadComponent: () => import('./pages/auth').then(m => m.AuthComponent), title: 'Sign in' },
  { path: 'register', loadComponent: () => import('./pages/auth').then(m => m.AuthComponent), data: { register: true }, title: 'Sign up' },
  { path: 'checkout', canActivate: [authGuard], loadComponent: () => import('./pages/checkout').then(m => m.CheckoutComponent) },
  { path: 'orders', canActivate: [authGuard], loadComponent: () => import('./pages/orders').then(m => m.OrdersComponent), title: 'Orders' },
  { path: '**', redirectTo: '' }
];
