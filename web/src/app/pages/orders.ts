import { Component, OnDestroy, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { OrderService } from '../core/api';
import { Order } from '../core/models';
import { IconComponent, btn, cn, money } from '../ui/ui';

const STEPS = ['Pending', 'StockReserved', 'Confirmed'];
const LABEL: Record<string, string> = { Pending: 'Order placed', StockReserved: 'Stock reserved', Confirmed: 'Paid & confirmed', Cancelled: 'Cancelled' };

@Component({
  selector: 'app-orders',
  imports: [DatePipe, IconComponent],
  template: `
  <div class="mx-auto max-w-4xl px-4 py-10">
    <h1 class="text-3xl font-extrabold tracking-tight">Your orders</h1>
    @if (placed()) {
      <div class="mt-6 rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-900">
        Order received. We're reserving stock and processing payment; this page updates automatically.
      </div>
    }
    <div class="mt-8 space-y-5">
      @for (o of orders(); track o.id) {
        <article class="rounded-xl border bg-card p-6">
          <header class="flex flex-wrap items-center justify-between gap-2">
            <div><div class="text-sm text-muted-foreground">Order {{ o.id.slice(0,8) }} &middot; {{ o.createdOnUtc | date:'medium' }}</div>
              <div class="mt-1 text-lg font-bold">{{ money(o.total, o.currency) }}</div></div>
            <span [class]="badge(o.status)">{{ label(o.status) }}</span>
          </header>
          @if (o.status !== 'Cancelled') {
            <ol class="mt-6 flex items-center">
              @for (s of steps; track s; let i = $index) {
                <li class="flex flex-1 items-center last:flex-none">
                  <span [class]="cn('grid size-8 place-items-center rounded-full border text-xs font-bold', i <= idx(o) ? 'border-accent bg-accent text-accent-foreground' : 'bg-muted text-muted-foreground')">
                    @if (i <= idx(o)) { <app-icon name="check" [size]="14"/> } @else { {{ i + 1 }} }
                  </span>
                  <span class="ml-2 hidden text-xs font-medium sm:block">{{ label(s) }}</span>
                  @if (i < steps.length - 1) { <span [class]="cn('mx-3 h-0.5 flex-1', i < idx(o) ? 'bg-accent' : 'bg-border')"></span> }
                </li>
              }
            </ol>
          } @else if (o.cancellationReason) {
            <p class="mt-4 text-sm text-destructive">Reason: {{ o.cancellationReason }}</p>
          }
          <ul class="mt-6 divide-y text-sm">
            @for (i of o.items; track i.productId) {
              <li class="flex justify-between py-2"><span>{{ i.quantity }} x {{ i.productName }}</span><span>{{ money(i.unitPrice * i.quantity, o.currency) }}</span></li>
            }
          </ul>
        </article>
      } @empty { <p class="py-20 text-center text-muted-foreground">No orders yet.</p> }
    </div>
  </div>`
})
export class OrdersComponent implements OnDestroy {
  placed = input<string>();
  private svc = inject(OrderService);
  orders = signal<Order[]>([]);
  steps = STEPS; cn = cn; money = money; btn = btn;
  private timer = setInterval(() => this.load(), 2500);
  constructor() { this.load(); }
  ngOnDestroy() { clearInterval(this.timer); }
  private load() { this.svc.mine().subscribe(o => this.orders.set([...o].sort((a, b) => b.createdOnUtc.localeCompare(a.createdOnUtc)))); }
  idx = (o: Order) => STEPS.indexOf(o.status);
  label = (s: string) => LABEL[s] ?? s;
  badge = (s: string) => cn('rounded-full px-3 py-1 text-xs font-semibold',
    s === 'Confirmed' ? 'bg-emerald-100 text-emerald-800' : s === 'Cancelled' ? 'bg-red-100 text-red-800' : 'bg-amber-100 text-amber-800');
}
