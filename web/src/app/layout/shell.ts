import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService, CartService } from '../core/api';
import { IconComponent, btn, inputClass, money } from '../ui/ui';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, FormsModule, IconComponent],
  template: `
  <div class="bg-primary text-primary-foreground text-xs text-center py-2 tracking-wide">
    Free delivery on orders over EGP 1,500 &middot; 14-day returns
  </div>
  <header class="sticky top-0 z-40 border-b bg-background/85 backdrop-blur">
    <div class="mx-auto flex h-16 max-w-7xl items-center gap-6 px-4">
      <a routerLink="/" class="flex items-center gap-2 text-xl font-extrabold tracking-tight">
        <span class="grid size-8 place-items-center rounded-lg bg-accent text-accent-foreground"><app-icon name="package" [size]="18"/></span>
        ShopSphere
      </a>
      <nav class="hidden md:flex items-center gap-1 text-sm font-medium">
        <a routerLink="/products" routerLinkActive="bg-muted" [routerLinkActiveOptions]="{exact:true}" class="rounded-md px-3 py-2 hover:bg-muted">Shop all</a>
        <a routerLink="/orders" routerLinkActive="bg-muted" class="rounded-md px-3 py-2 hover:bg-muted">Orders</a>
      </nav>
      <form class="relative hidden flex-1 max-w-xl sm:block" (ngSubmit)="search()">
        <app-icon name="search" class="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
        <input name="q" [(ngModel)]="q" placeholder="Search products, brands and more" [class]="inputClass + ' pl-10 bg-muted border-transparent'" />
      </form>
      <div class="ml-auto flex items-center gap-1">
        @if (auth.session(); as s) {
          <span class="hidden lg:block text-sm text-muted-foreground mr-1">Hi, {{ s.userName }}</span>
          <button [class]="btn({variant:'ghost',size:'sm'})" (click)="auth.logout()">Sign out</button>
        } @else {
          <a routerLink="/login" [class]="btn({variant:'ghost',size:'sm'})"><app-icon name="user" /> Sign in</a>
        }
        <button [class]="btn({variant:'ghost',size:'icon'}, 'relative')" (click)="cart.open.set(true)" aria-label="Open cart">
          <app-icon name="cart" [size]="22" />
          @if (cart.count() > 0) {
            <span class="absolute -right-0.5 -top-0.5 grid min-w-5 h-5 place-items-center rounded-full bg-accent px-1 text-[11px] font-bold text-accent-foreground">{{ cart.count() }}</span>
          }
        </button>
      </div>
    </div>
  </header>

  <main class="min-h-[70vh]"><router-outlet /></main>

  <footer class="mt-24 border-t bg-muted/50">
    <div class="mx-auto grid max-w-7xl gap-8 px-4 py-12 text-sm md:grid-cols-4">
      <div><div class="text-lg font-extrabold">ShopSphere</div>
        <p class="mt-2 text-muted-foreground">A microservices e-commerce showcase: .NET 10, RabbitMQ, Angular.</p></div>
      <div><div class="font-semibold">Shop</div><ul class="mt-3 space-y-2 text-muted-foreground"><li>Electronics</li><li>Fashion</li><li>Home &amp; Kitchen</li></ul></div>
      <div><div class="font-semibold">Help</div><ul class="mt-3 space-y-2 text-muted-foreground"><li>Shipping</li><li>Returns</li><li>Contact</li></ul></div>
      <div><div class="font-semibold">Built with</div><ul class="mt-3 space-y-2 text-muted-foreground"><li>DDD + Clean Architecture</li><li>Saga over RabbitMQ</li><li>Angular 22 + Tailwind 4</li></ul></div>
    </div>
  </footer>

  @if (cart.open()) {
    <div class="fixed inset-0 z-50">
      <div class="absolute inset-0 bg-black/40" (click)="cart.open.set(false)"></div>
      <aside class="absolute right-0 top-0 flex h-full w-full max-w-md flex-col bg-background shadow-2xl">
        <div class="flex items-center justify-between border-b px-5 py-4">
          <h2 class="text-lg font-bold">Your cart ({{ cart.count() }})</h2>
          <button [class]="btn({variant:'ghost',size:'icon'})" (click)="cart.open.set(false)" aria-label="Close"><app-icon name="x" /></button>
        </div>
        <div class="flex-1 overflow-y-auto px-5">
          @if (!auth.isAuthenticated()) {
            <p class="py-16 text-center text-muted-foreground">Sign in to start your cart.</p>
          } @else if (!cart.cart()?.lines?.length) {
            <p class="py-16 text-center text-muted-foreground">Your cart is empty.</p>
          } @else {
            @for (l of cart.cart()!.lines; track l.productId) {
              <div class="flex gap-4 border-b py-4">
                <img [src]="l.imageUrl" alt="" class="size-20 rounded-lg bg-muted object-cover" />
                <div class="flex-1">
                  <div class="text-sm font-medium leading-snug">{{ l.productName }}</div>
                  <div class="mt-1 text-sm text-muted-foreground">{{ money(l.unitPrice) }}</div>
                  <div class="mt-2 flex items-center gap-2">
                    <button [class]="btn({variant:'outline',size:'sm'}, 'size-8 px-0')" (click)="cart.setQuantity(l.productId, l.quantity - 1)"><app-icon name="minus" [size]="14"/></button>
                    <span class="w-6 text-center text-sm">{{ l.quantity }}</span>
                    <button [class]="btn({variant:'outline',size:'sm'}, 'size-8 px-0')" (click)="cart.setQuantity(l.productId, l.quantity + 1)"><app-icon name="plus" [size]="14"/></button>
                    <button class="ml-auto text-muted-foreground hover:text-destructive" (click)="cart.remove(l.productId)" aria-label="Remove"><app-icon name="trash" /></button>
                  </div>
                </div>
              </div>
            }
          }
        </div>
        @if (cart.cart()?.lines?.length) {
          <div class="border-t p-5">
            <div class="mb-4 flex justify-between text-base font-semibold"><span>Subtotal</span><span>{{ money(cart.cart()!.total) }}</span></div>
            <a routerLink="/checkout" (click)="cart.open.set(false)" [class]="btn({variant:'accent',size:'lg'}, 'w-full')">Checkout</a>
          </div>
        }
      </aside>
    </div>
  }`
})
export class ShellComponent {
  auth = inject(AuthService);
  cart = inject(CartService);
  private router = inject(Router);
  q = '';
  btn = btn; inputClass = inputClass; money = money;
  constructor() { this.cart.refresh(); }
  search() { this.router.navigate(['/products'], { queryParams: { q: this.q || null } }); }
}
