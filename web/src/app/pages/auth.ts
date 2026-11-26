import { Component, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService, CartService } from '../core/api';
import { btn, inputClass } from '../ui/ui';

@Component({
  selector: 'app-auth',
  imports: [FormsModule, RouterLink],
  template: `
  <div class="mx-auto my-16 max-w-md rounded-2xl border bg-card p-8 shadow-sm">
    <h1 class="text-2xl font-extrabold tracking-tight">{{ register() ? 'Create your account' : 'Welcome back' }}</h1>
    <p class="mt-1 text-sm text-muted-foreground">{{ register() ? 'Join ShopSphere in a few seconds.' : 'Sign in to continue shopping.' }}</p>
    <form class="mt-6 space-y-4" (ngSubmit)="submit()">
      @if (register()) {
        <label class="block text-sm font-medium">Name <input name="u" [(ngModel)]="userName" required [class]="inputClass + ' mt-1.5'" /></label>
      }
      <label class="block text-sm font-medium">Email <input name="e" type="email" [(ngModel)]="email" required [class]="inputClass + ' mt-1.5'" /></label>
      <label class="block text-sm font-medium">Password <input name="p" type="password" [(ngModel)]="password" required minlength="8" [class]="inputClass + ' mt-1.5'" /></label>
      @if (error()) { <p class="text-sm text-destructive">{{ error() }}</p> }
      <button type="submit" [disabled]="busy()" [class]="btn({variant:'default',size:'lg'}, 'w-full')">{{ register() ? 'Sign up' : 'Sign in' }}</button>
    </form>
    <p class="mt-6 text-center text-sm text-muted-foreground">
      @if (register()) { Have an account? <a routerLink="/login" class="font-medium text-foreground underline">Sign in</a> }
      @else { New here? <a routerLink="/register" class="font-medium text-foreground underline">Create an account</a> }
    </p>
  </div>`
})
export class AuthComponent {
  register = input(false);
  returnUrl = input<string>();
  email = ''; password = ''; userName = '';
  busy = signal(false); error = signal('');
  btn = btn; inputClass = inputClass;
  private auth = inject(AuthService);
  private cart = inject(CartService);
  private router = inject(Router);
  submit() {
    this.busy.set(true); this.error.set('');
    const call = this.register() ? this.auth.register(this.email, this.userName, this.password) : this.auth.login(this.email, this.password);
    call.subscribe({
      next: () => { this.cart.refresh(); this.router.navigateByUrl(this.returnUrl() ?? '/'); },
      error: (e: HttpErrorResponse) => { this.busy.set(false); this.error.set(e.error?.title ?? 'Something went wrong. Try again.'); }
    });
  }
}
