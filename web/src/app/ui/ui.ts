import { Component, input } from '@angular/core';
import { cva, type VariantProps } from 'class-variance-authority';
import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

export const cn = (...i: ClassValue[]) => twMerge(clsx(i));
export const money = (n: number, c = 'EGP') => new Intl.NumberFormat('en-EG', { style: 'currency', currency: c, maximumFractionDigits: 0 }).format(n);

/** shadcn/ui-style button variants, applied with `[class]="btn({variant,size})"`. */
export const buttonVariants = cva(
  'inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-md text-sm font-medium transition-all focus-visible:ring-2 focus-visible:ring-ring/60 disabled:pointer-events-none disabled:opacity-50 cursor-pointer',
  {
    variants: {
      variant: {
        default: 'bg-primary text-primary-foreground hover:bg-primary/90 shadow-sm',
        accent: 'bg-accent text-accent-foreground hover:bg-accent/90 shadow-sm',
        outline: 'border bg-background hover:bg-muted',
        ghost: 'hover:bg-muted',
        link: 'text-primary underline-offset-4 hover:underline',
        destructive: 'bg-destructive text-white hover:bg-destructive/90'
      },
      size: { default: 'h-10 px-4', sm: 'h-8 px-3 text-xs', lg: 'h-12 px-8 text-base', icon: 'size-10' }
    },
    defaultVariants: { variant: 'default', size: 'default' }
  }
);
export type ButtonProps = VariantProps<typeof buttonVariants>;
export const btn = (p: ButtonProps = {}, extra = '') => cn(buttonVariants(p), extra);

export const inputClass =
  'flex h-10 w-full rounded-md border bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring/60 disabled:opacity-50';

const ICONS: Record<string, string> = {
  cart: 'M6 6h15l-1.5 9h-12zM6 6L5 3H2M9 20a1 1 0 100-2 1 1 0 000 2zM18 20a1 1 0 100-2 1 1 0 000 2z',
  search: 'M21 21l-4.3-4.3M11 18a7 7 0 110-14 7 7 0 010 14z',
  user: 'M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2M12 11a4 4 0 100-8 4 4 0 000 8z',
  star: 'M12 2l3.1 6.3 6.9 1-5 4.9 1.2 6.8L12 17.8 5.8 21l1.2-6.8-5-4.9 6.9-1z',
  x: 'M18 6L6 18M6 6l12 12',
  plus: 'M12 5v14M5 12h14',
  minus: 'M5 12h14',
  truck: 'M1 3h15v13H1zM16 8h4l3 3v5h-7zM5.5 21a2 2 0 100-4 2 2 0 000 4zM18.5 21a2 2 0 100-4 2 2 0 000 4z',
  shield: 'M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z',
  refresh: 'M23 4v6h-6M1 20v-6h6M3.5 9a9 9 0 0114.9-3.4L23 10M1 14l4.6 4.4A9 9 0 0020.5 15',
  check: 'M20 6L9 17l-5-5',
  package: 'M16.5 9.4L7.5 4.2M21 16V8a2 2 0 00-1-1.7l-7-4a2 2 0 00-2 0l-7 4A2 2 0 003 8v8a2 2 0 001 1.7l7 4a2 2 0 002 0l7-4a2 2 0 001-1.7zM3.3 7L12 12l8.7-5M12 22V12',
  trash: 'M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6',
  chevron: 'M9 18l6-6-6-6'
};

@Component({
  selector: 'app-icon',
  template: `<svg [attr.width]="size()" [attr.height]="size()" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path [attr.d]="path()" [attr.fill]="name() === 'star' && filled() ? 'currentColor' : 'none'"/></svg>`,
  host: { class: 'inline-flex' }
})
export class IconComponent {
  name = input.required<string>();
  size = input(18);
  filled = input(false);
  path = () => ICONS[this.name()] ?? '';
}

@Component({
  selector: 'app-stars',
  imports: [IconComponent],
  template: `<span class="inline-flex items-center gap-0.5 text-amber-500">
    @for (i of [1,2,3,4,5]; track i) { <app-icon name="star" [size]="14" [filled]="i <= rounded()" [class.opacity-30]="i > rounded()" /> }
  </span>`
})
export class StarsComponent {
  value = input.required<number>();
  rounded = () => Math.round(this.value());
}
