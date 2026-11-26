import { Component, effect, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService, CartService, CatalogService } from '../core/api';
import { Product } from '../core/models';
import { IconComponent, StarsComponent, btn, money } from '../ui/ui';

@Component({
  selector: 'app-product-detail',
  imports: [RouterLink, StarsComponent, IconComponent],
  template: `
  @if (p(); as p) {
  <div class="mx-auto max-w-7xl px-4 py-8">
    <nav class="mb-6 text-sm text-muted-foreground"><a routerLink="/products" class="hover:underline">Shop</a> / {{ p.name }}</nav>
    <div class="grid gap-10 md:grid-cols-2">
      <img [src]="p.imageUrl" [alt]="p.name" class="aspect-square w-full rounded-2xl bg-muted object-cover" />
      <div>
        <h1 class="text-3xl font-extrabold tracking-tight">{{ p.name }}</h1>
        <div class="mt-3 flex items-center gap-2 text-sm text-muted-foreground"><app-stars [value]="p.rating" /> {{ p.rating.toFixed(1) }} &middot; {{ p.reviewCount }} reviews</div>
        <div class="mt-6 text-4xl font-bold">{{ money(p.price, p.currency) }}</div>
        <p class="mt-6 leading-relaxed text-muted-foreground">{{ p.description }}</p>
        <div class="mt-8 flex items-center gap-4">
          <div class="flex items-center rounded-md border">
            <button class="grid size-10 place-items-center hover:bg-muted" (click)="qty.set(Math.max(1, qty() - 1))" aria-label="Less"><app-icon name="minus" [size]="14"/></button>
            <span class="w-10 text-center text-sm font-medium">{{ qty() }}</span>
            <button class="grid size-10 place-items-center hover:bg-muted" (click)="qty.set(qty() + 1)" aria-label="More"><app-icon name="plus" [size]="14"/></button>
          </div>
          <button [class]="btn({variant:'accent',size:'lg'}, 'flex-1')" (click)="add(p)">Add to cart</button>
        </div>
        <ul class="mt-8 space-y-3 text-sm">
          <li class="flex items-center gap-3"><app-icon name="truck"/> Free delivery over EGP 1,500</li>
          <li class="flex items-center gap-3"><app-icon name="refresh"/> 14-day free returns</li>
          <li class="flex items-center gap-3"><app-icon name="shield"/> 1-year warranty &middot; SKU {{ p.sku }}</li>
        </ul>
      </div>
    </div>
  </div> } @else { <div class="mx-auto h-96 max-w-7xl animate-pulse px-4"></div> }`
})
export class ProductDetailComponent {
  id = input.required<string>();
  p = signal<Product | null>(null);
  qty = signal(1);
  Math = Math; btn = btn; money = money;
  private catalog = inject(CatalogService);
  private cart = inject(CartService);
  private auth = inject(AuthService);
  private router = inject(Router);
  constructor() { effect(() => this.catalog.get(this.id()).subscribe(p => this.p.set(p))); }
  add(p: Product) {
    if (!this.auth.isAuthenticated()) { this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } }); return; }
    this.cart.add(p, this.qty()).subscribe();
  }
}
