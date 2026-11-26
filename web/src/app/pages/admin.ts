import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService, CatalogService } from '../core/api';
import { Category, Product } from '../core/models';
import { btn, inputClass, money } from '../ui/ui';

@Component({
  selector: 'app-admin',
  imports: [FormsModule],
  template: `
  <div class="mx-auto max-w-6xl px-4 py-10">
    <h1 class="text-3xl font-extrabold tracking-tight">Catalog admin</h1>
    <p class="mt-1 text-sm text-muted-foreground">Changes publish events (ProductCreated, PriceChanged, Discontinued). Inventory and Ordering react, and the Redis catalog cache is invalidated.</p>

    <form class="mt-8 grid gap-3 rounded-xl border bg-card p-5 md:grid-cols-3" (ngSubmit)="create()">
      <input name="n" [(ngModel)]="f.name" required placeholder="Name" [class]="inputClass" />
      <input name="s" [(ngModel)]="f.sku" required placeholder="SKU" [class]="inputClass" />
      <input name="p" type="number" min="1" [(ngModel)]="f.price" required placeholder="Price (EGP)" [class]="inputClass" />
      <input name="i" [(ngModel)]="f.imageUrl" placeholder="Image URL" [class]="inputClass" />
      <select name="c" [(ngModel)]="f.categoryId" required [class]="inputClass">
        <option value="" disabled>Category</option>
        @for (c of categories(); track c.id) { <option [value]="c.id">{{ c.name }}</option> }
      </select>
      <button type="submit" [class]="btn({variant:'accent'})">Add product</button>
      <textarea name="d" [(ngModel)]="f.description" placeholder="Description" [class]="inputClass + ' md:col-span-3 h-20'"></textarea>
    </form>
    @if (message()) { <p class="mt-3 text-sm" [class.text-destructive]="isError()">{{ message() }}</p> }

    <div class="mt-8 overflow-hidden rounded-xl border">
      <table class="w-full text-sm">
        <thead class="bg-muted text-left text-xs uppercase tracking-wide text-muted-foreground">
          <tr><th class="p-3">Product</th><th class="p-3">SKU</th><th class="p-3">Price</th><th class="p-3 text-right">Actions</th></tr>
        </thead>
        <tbody class="divide-y">
          @for (p of products(); track p.id) {
            <tr>
              <td class="p-3"><div class="flex items-center gap-3"><img [src]="p.imageUrl" alt="" class="size-10 rounded-md bg-muted object-cover" />{{ p.name }}</div></td>
              <td class="p-3 text-muted-foreground">{{ p.sku }}</td>
              <td class="p-3">
                <div class="flex items-center gap-2">
                  <input type="number" min="1" [ngModel]="p.price" (ngModelChange)="edits[p.id] = $event" class="h-8 w-28 rounded-md border px-2" [name]="'pr' + p.id" />
                  <button [class]="btn({variant:'outline',size:'sm'})" (click)="save(p)">Save</button>
                  <span class="text-xs text-muted-foreground">{{ money(p.price) }}</span>
                </div>
              </td>
              <td class="p-3 text-right"><button [class]="btn({variant:'destructive',size:'sm'})" (click)="remove(p)">Discontinue</button></td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  </div>`
})
export class AdminComponent {
  private catalog = inject(CatalogService);
  private admin = inject(AdminService);
  products = signal<Product[]>([]);
  categories = signal<Category[]>([]);
  message = signal(''); isError = signal(false);
  edits: Record<string, number> = {};
  f = { name: '', sku: '', description: '', price: null as number | null, currency: 'EGP', imageUrl: '', categoryId: '' };
  btn = btn; inputClass = inputClass; money = money;
  constructor() {
    this.catalog.categories().subscribe(c => this.categories.set(c));
    this.reload();
  }
  reload() { this.catalog.search({ pageSize: 60, sort: 'relevance' }).subscribe(r => this.products.set(r.items)); }
  private done(msg: string, err = false) { this.message.set(msg); this.isError.set(err); }
  create() {
    this.admin.create({ ...this.f, price: this.f.price!, imageUrl: this.f.imageUrl || "/products/default.svg" }).subscribe({
      next: () => { this.done('Product created.'); this.reload(); },
      error: e => this.done(e.error?.title ?? 'Could not create product.', true)
    });
  }
  save(p: Product) {
    const price = this.edits[p.id] ?? p.price;
    this.admin.changePrice(p.id, price).subscribe({
      next: () => { this.done(`Price updated to ${price}.`); this.reload(); },
      error: e => this.done(e.error?.title ?? 'Could not update price.', true)
    });
  }
  remove(p: Product) {
    this.admin.discontinue(p.id).subscribe({ next: () => { this.done(`${p.name} discontinued.`); this.reload(); }, error: () => this.done('Failed.', true) });
  }
}
