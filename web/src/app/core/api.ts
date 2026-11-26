import { HttpClient, HttpInterceptorFn, HttpParams } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { AuthResponse, Cart, Category, Order, Paged, Product, ProductQuery } from './models';

const API = '/api';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private readonly key = 'shopsphere.auth';
  readonly session = signal<AuthResponse | null>(this.restore());
  readonly isAuthenticated = computed(() => !!this.session());
  readonly isAdmin = computed(() => this.session()?.role === 'Admin');
  get token() { return this.session()?.accessToken ?? null; }

  login(email: string, password: string) {
    return this.http.post<AuthResponse>(`${API}/auth/login`, { email, password }).pipe(tap(r => this.store(r)));
  }
  register(email: string, userName: string, password: string) {
    return this.http.post<AuthResponse>(`${API}/auth/register`, { email, userName, password }).pipe(tap(r => this.store(r)));
  }
  logout() { localStorage.removeItem(this.key); this.session.set(null); this.router.navigateByUrl('/'); }
  private store(r: AuthResponse) { localStorage.setItem(this.key, JSON.stringify(r)); this.session.set(r); }
  private restore(): AuthResponse | null {
    try {
      const r = JSON.parse(localStorage.getItem(this.key) ?? 'null') as AuthResponse | null;
      return r && new Date(r.expiresOnUtc) > new Date() ? r : null;
    } catch { return null; }
  }
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).token;
  return next(token && req.url.startsWith(API) ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req);
};

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private http = inject(HttpClient);
  search(q: ProductQuery) {
    let params = new HttpParams();
    for (const [k, v] of Object.entries(q)) if (v !== undefined && v !== '' && v !== null) params = params.set(k, String(v));
    return this.http.get<Paged<Product>>(`${API}/products`, { params });
  }
  get(id: string) { return this.http.get<Product>(`${API}/products/${id}`); }
  categories() { return this.http.get<Category[]>(`${API}/categories`); }
}

@Injectable({ providedIn: 'root' })
export class CartService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  readonly cart = signal<Cart | null>(null);
  readonly open = signal(false);
  readonly count = computed(() => this.cart()?.lines.reduce((n, l) => n + l.quantity, 0) ?? 0);

  refresh() {
    if (!this.auth.isAuthenticated()) { this.cart.set(null); return; }
    this.http.get<Cart>(`${API}/cart`).subscribe({ next: c => this.cart.set(c), error: () => this.cart.set(null) });
  }
  add(p: Product, quantity = 1) {
    return this.http.post<Cart>(`${API}/cart/items`, {
      productId: p.id, productName: p.name, unitPrice: p.price, imageUrl: p.imageUrl, quantity
    }).pipe(tap(c => { this.cart.set(c); this.open.set(true); }));
  }
  setQuantity(productId: string, quantity: number) {
    this.http.put<Cart>(`${API}/cart/items/${productId}`, { quantity }).subscribe(c => this.cart.set(c));
  }
  remove(productId: string) { this.http.delete<Cart>(`${API}/cart/items/${productId}`).subscribe(c => this.cart.set(c)); }
  clearLocal() { this.cart.set(null); }
}

@Injectable({ providedIn: 'root' })
export class OrderService {
  private http = inject(HttpClient);
  place(items: { productId: string; productName: string; quantity: number; unitPrice: number }[]) {
    return this.http.post<{ id: string }>(`${API}/orders`, { currency: 'EGP', items });
  }
  mine() { return this.http.get<Order[]>(`${API}/orders`); }
  get(id: string) { return this.http.get<Order>(`${API}/orders/${id}`); }
  cancel(id: string) { return this.http.post<void>(`${API}/orders/${id}/cancel`, {}); }
}
