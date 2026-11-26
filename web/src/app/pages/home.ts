import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CatalogService } from '../core/api';
import { Category, Product } from '../core/models';
import { ProductCardComponent } from '../ui/product-card';
import { IconComponent, btn } from '../ui/ui';

@Component({
  selector: 'app-home',
  imports: [RouterLink, ProductCardComponent, IconComponent],
  template: `
  <section class="relative overflow-hidden bg-gradient-to-br from-primary via-neutral-800 to-neutral-700 text-primary-foreground">
    <div class="mx-auto grid max-w-7xl items-center gap-10 px-4 py-20 md:grid-cols-2 md:py-28">
      <div>
        <span class="rounded-full bg-white/10 px-3 py-1 text-xs font-semibold tracking-wide">NEW SEASON</span>
        <h1 class="mt-5 text-5xl font-extrabold leading-[1.05] tracking-tight md:text-6xl">Everything you love,<br/>delivered fast.</h1>
        <p class="mt-5 max-w-md text-lg text-white/70">Thousands of products across electronics, fashion, home and sport. Backed by an event-driven order pipeline.</p>
        <div class="mt-8 flex gap-3">
          <a routerLink="/products" [class]="btn({variant:'accent',size:'lg'})">Shop now</a>
          <a routerLink="/products" [queryParams]="{sort:'rating'}" [class]="btn({variant:'outline',size:'lg'}, 'bg-transparent text-white border-white/30 hover:bg-white/10')">Top rated</a>
        </div>
      </div>
      <div class="hidden grid-cols-2 gap-4 md:grid">
        @for (p of featured().slice(0,4); track p.id) {
          <img [src]="p.imageUrl" [alt]="p.name" class="aspect-square w-full rounded-2xl object-cover shadow-2xl ring-1 ring-white/10 first:translate-y-6 last:-translate-y-2" />
        }
      </div>
    </div>
  </section>

  <section class="mx-auto -mt-8 grid max-w-7xl gap-4 px-4 sm:grid-cols-3">
    @for (b of benefits; track b.t) {
      <div class="flex items-center gap-4 rounded-xl border bg-card p-5 shadow-sm">
        <span class="grid size-11 place-items-center rounded-lg bg-muted"><app-icon [name]="b.i" [size]="22"/></span>
        <div><div class="font-semibold">{{ b.t }}</div><div class="text-sm text-muted-foreground">{{ b.d }}</div></div>
      </div>
    }
  </section>

  <section class="mx-auto mt-16 max-w-7xl px-4">
    <h2 class="text-2xl font-bold tracking-tight">Shop by category</h2>
    <div class="mt-5 grid grid-cols-2 gap-4 md:grid-cols-4">
      @for (c of categories(); track c.id) {
        <a routerLink="/products" [queryParams]="{categoryId: c.id}" class="group relative flex h-36 items-end overflow-hidden rounded-xl bg-muted p-5">
          <img [src]="'https://picsum.photos/seed/' + c.slug + '/500/300'" alt="" class="absolute inset-0 size-full object-cover transition-transform duration-500 group-hover:scale-105" />
          <div class="absolute inset-0 bg-gradient-to-t from-black/70 to-transparent"></div>
          <span class="relative text-lg font-bold text-white">{{ c.name }}</span>
        </a>
      }
    </div>
  </section>

  <section class="mx-auto mt-16 max-w-7xl px-4">
    <div class="flex items-end justify-between">
      <h2 class="text-2xl font-bold tracking-tight">Trending now</h2>
      <a routerLink="/products" class="text-sm font-medium underline-offset-4 hover:underline">View all</a>
    </div>
    <div class="mt-5 grid grid-cols-2 gap-5 lg:grid-cols-4">
      @for (p of featured(); track p.id) { <app-product-card [p]="p" /> }
    </div>
  </section>`
})
export class HomeComponent {
  private catalog = inject(CatalogService);
  featured = signal<Product[]>([]);
  categories = signal<Category[]>([]);
  btn = btn;
  benefits = [
    { i: 'truck', t: 'Fast delivery', d: 'Free over EGP 1,500' },
    { i: 'refresh', t: 'Easy returns', d: '14 days, no questions' },
    { i: 'shield', t: 'Secure checkout', d: 'Protected payments' }
  ];
  constructor() {
    this.catalog.search({ sort: 'relevance', pageSize: 8 }).subscribe(r => this.featured.set(r.items));
    this.catalog.categories().subscribe(c => this.categories.set(c));
  }
}
