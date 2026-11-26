import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CartService, OrderService } from '../core/api';
import { btn, money } from '../ui/ui';

@Component({
  selector: 'app-checkout',
  imports: [RouterLink],
  template: `
  <div class="mx-auto max-w-4xl px-4 py-10">
    <h1 class="text-3xl font-extrabold tracking-tight">Checkout</h1>
    @if (!cart.cart()?.lines?.length) {
      <p class="py-20 text-center text-muted-foreground">Your cart is empty. <a routerLink="/products" class="underline">Keep shopping</a></p>
    } @else {
      <div class="mt-8 grid gap-8 md:grid-cols-[1fr_320px]">
        <div class="divide-y rounded-xl border bg-card">
          @for (l of cart.cart()!.lines; track l.productId) {
            <div class="flex items-center gap-4 p-4">
              <img [src]="l.imageUrl" alt="" class="size-16 rounded-lg bg-muted object-cover" />
              <div class="flex-1 text-sm"><div class="font-medium">{{ l.productName }}</div><div class="text-muted-foreground">Qty {{ l.quantity }}</div></div>
              <div class="font-semibold">{{ money(l.lineTotal) }}</div>
            </div>
          }
        </div>
        <div class="h-fit rounded-xl border bg-card p-6">
          <h2 class="font-semibold">Order summary</h2>
          <dl class="mt-4 space-y-2 text-sm">
            <div class="flex justify-between"><dt class="text-muted-foreground">Subtotal</dt><dd>{{ money(cart.cart()!.total) }}</dd></div>
            <div class="flex justify-between"><dt class="text-muted-foreground">Shipping</dt><dd>{{ cart.cart()!.total >= 1500 ? 'Free' : money(60) }}</dd></div>
            <div class="flex justify-between border-t pt-3 text-base font-bold"><dt>Total</dt><dd>{{ money(cart.cart()!.total + (cart.cart()!.total >= 1500 ? 0 : 60)) }}</dd></div>
          </dl>
          <p class="mt-4 text-xs text-muted-foreground">Demo mode: payment is simulated. Totals ending in .99 are declined to demo the failure path.</p>
          @if (error()) { <p class="mt-3 text-sm text-destructive">{{ error() }}</p> }
          <button [class]="btn({variant:'accent',size:'lg'}, 'mt-5 w-full')" [disabled]="busy()" (click)="place()">{{ busy() ? 'Placing order...' : 'Place order' }}</button>
        </div>
      </div>
    }
  </div>`
})
export class CheckoutComponent {
  cart = inject(CartService);
  private orders = inject(OrderService);
  private router = inject(Router);
  busy = signal(false); error = signal('');
  btn = btn; money = money;
  place() {
    const lines = this.cart.cart()!.lines;
    this.busy.set(true);
    this.orders.place(lines.map(l => ({ productId: l.productId, productName: l.productName, quantity: l.quantity, unitPrice: l.unitPrice })))
      .subscribe({
        next: r => { this.cart.clearLocal(); this.router.navigate(['/orders'], { queryParams: { placed: r.id } }); },
        error: e => { this.busy.set(false); this.error.set(e.error?.title ?? 'Could not place the order.'); }
      });
  }
}
