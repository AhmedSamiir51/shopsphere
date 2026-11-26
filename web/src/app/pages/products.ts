import { Component, effect, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CatalogService } from '../core/api';
import { Category, Paged, Product } from '../core/models';
import { ProductCardComponent } from '../ui/product-card';
import { btn, cn, inputClass } from '../ui/ui';

@Component({
  selector: 'app-products',
  imports: [ProductCardComponent, FormsModule, ],
  template: `
  <div class="mx-auto max-w-7xl px-4 py-8">
    <div class="mb-6 flex flex-wrap items-end justify-between gap-4">
      <div>
        <h1 class="text-3xl font-extrabold tracking-tight">{{ q() ? 'Results for "' + q() + '"' : 'All products' }}</h1>
        <p class="mt-1 text-sm text-muted-foreground">{{ result()?.total ?? 0 }} items</p>
      </div>
      <select [class]="sortClass" [ngModel]="sort() ?? 'relevance'" (ngModelChange)="set({sort: $event, page: null})" aria-label="Sort">
        <option value="relevance">Most popular</option>
        <option value="rating">Top rated</option>
        <option value="price-asc">Price: low to high</option>
        <option value="price-desc">Price: high to low</option>
      </select>
    </div>

    <div class="grid gap-8 lg:grid-cols-[240px_1fr]">
      <aside class="space-y-8">
        <div>
          <h3 class="mb-3 text-sm font-semibold">Category</h3>
          <div class="flex flex-col gap-1">
            <button [class]="chip(!categoryId())" (click)="set({categoryId: null, page: null})">All</button>
            @for (c of categories(); track c.id) {
              <button [class]="chip(categoryId() === c.id)" (click)="set({categoryId: c.id, page: null})">{{ c.name }}</button>
            }
          </div>
        </div>
        <div>
          <h3 class="mb-3 text-sm font-semibold">Price (EGP)</h3>
          <div class="flex items-center gap-2">
            <input type="number" placeholder="Min" [class]="inputClass" [(ngModel)]="min" />
            <span class="text-muted-foreground">-</span>
            <input type="number" placeholder="Max" [class]="inputClass" [(ngModel)]="max" />
          </div>
          <button [class]="btn({variant:'outline',size:'sm'}, 'mt-3 w-full')" (click)="set({minPrice: min, maxPrice: max, page: null})">Apply</button>
        </div>
      </aside>

      <section>
        @if (loading()) {
          <div class="grid grid-cols-2 gap-5 xl:grid-cols-3">
            @for (i of [1,2,3,4,5,6]; track i) { <div class="aspect-[3/4] animate-pulse rounded-xl bg-muted"></div> }
          </div>
        } @else if (!result()?.items?.length) {
          <p class="py-24 text-center text-muted-foreground">No products match your filters.</p>
        } @else {
          <div class="grid grid-cols-2 gap-5 xl:grid-cols-3">
            @for (p of result()!.items; track p.id) { <app-product-card [p]="p" /> }
          </div>
          @if (pages() > 1) {
            <div class="mt-10 flex justify-center gap-2">
              @for (n of pageNumbers(); track n) {
                <button [class]="btn({variant: n === page() ? 'default' : 'outline', size:'sm'}, 'size-9 px-0')" (click)="set({page: n})">{{ n }}</button>
              }
            </div>
          }
        }
      </section>
    </div>
  </div>`
})
export class ProductsComponent {
  // Route query params are bound to inputs (withComponentInputBinding).
  q = input<string>();
  categoryId = input<string>();
  sort = input<string>();
  minPrice = input<string>();
  maxPrice = input<string>();
  pageParam = input<string>(undefined, { alias: 'page' });

  private catalog = inject(CatalogService);
  private router = inject(Router);
  result = signal<Paged<Product> | null>(null);
  categories = signal<Category[]>([]);
  loading = signal(true);
  min: number | null = null;
  max: number | null = null;
  btn = btn; inputClass = inputClass; sortClass = cn(inputClass, 'w-52');
  page = () => Number(this.pageParam() ?? 1);
  pages = () => Math.ceil((this.result()?.total ?? 0) / 12);
  pageNumbers = () => Array.from({ length: this.pages() }, (_, i) => i + 1);
  chip = (active: boolean) => cn('rounded-md px-3 py-2 text-left text-sm hover:bg-muted', active && 'bg-muted font-semibold');

  constructor() {
    this.catalog.categories().subscribe(c => this.categories.set(c));
    effect(() => {
      const query = {
        q: this.q(), categoryId: this.categoryId(), sort: this.sort(),
        minPrice: this.minPrice() ? Number(this.minPrice()) : undefined,
        maxPrice: this.maxPrice() ? Number(this.maxPrice()) : undefined,
        page: this.page(), pageSize: 12
      };
      this.loading.set(true);
      this.catalog.search(query).subscribe({ next: r => { this.result.set(r); this.loading.set(false); }, error: () => this.loading.set(false) });
    });
  }
  set(params: Record<string, unknown>) {
    this.router.navigate([], { queryParams: params, queryParamsHandling: 'merge' });
  }
}
