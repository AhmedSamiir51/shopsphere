import { describe, expect, it } from 'vitest';
import { btn, cn, money } from './ui';

describe('ui helpers', () => {
  it('cn merges conflicting tailwind classes, last wins', () => {
    expect(cn('px-2 py-1', 'px-4')).toBe('py-1 px-4');
  });
  it('btn applies variant classes', () => {
    expect(btn({ variant: 'accent', size: 'lg' })).toContain('bg-accent');
    expect(btn({ variant: 'accent', size: 'lg' })).toContain('h-12');
  });
  it('money formats EGP without decimals', () => {
    expect(money(1299)).toContain('1,299');
  });
});
