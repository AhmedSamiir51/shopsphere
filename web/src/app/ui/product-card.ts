import { Component, inject, input } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService, CartService } from '../core/api';
import { Product } from '../core/models';
import { IconComponent, StarsComponent, btn, money } from './ui';

@Component({
  selector: 'app-product-card',
  imports: [RouterLink, StarsComponent, IconComponent],
  template: `
  <article class="group flex flex-col overflow-hidden rounded-xl border bg-card transition-shadow hover:shadow-lg">
    <a [routerLink]="['/products', p().id]" class="relative block aspect-square overflow-hidden bg-muted">
      <img [src]="p().imageUrl" [alt]="p().name" loading="lazy" class="size-full object-cover transition-transform duration-500 group-hover:scale-105" />
      @if (p().reviewCount > 2000) { <span class="absolute left-3 top-3 rounded-full bg-accent px-2.5 py-1 text-[11px] font-bold text-accent-foreground">Best seller</span> }
    </a>
    <div class="flex flex-1 flex-col gap-2 p-4">
      <a [routerLink]="['/products', p().id]" class="line-clamp-2 text-sm font-medium leading-snug hover:underline">{{ p().name }}</a>
      <div class="flex items-center gap-1.5 text-xs text-muted-foreground"><app-stars [value]="p().rating" /> {{ p().rating.toFixed(1) }} ({{ p().reviewCount }})</div>
      <div class="mt-auto flex items-center justify-between pt-2">
        <span class="text-lg font-bold">{{ money(p().price, p().currency) }}</span>
        <button [class]="btn({variant:'default',size:'sm'})" (click)="add()"><app-icon name="plus" [size]="14"/> Add</button>
      </div>
    </div>
  </article>`
})
export class ProductCardComponent {
  p = input.required<Product>();
  private cart = inject(CartService);
  private auth = inject(AuthService);
  private router = inject(Router);
  btn = btn; money = money;
  add() {
    if (!this.auth.isAuthenticated()) { this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } }); return; }
    this.cart.add(this.p()).subscribe();
  }
}
