import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it, beforeEach } from 'vitest';
import { AuthService, CatalogService, authInterceptor } from './api';

describe('api services', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting()]
    });
  });

  it('search sends only defined query params', () => {
    const http = TestBed.inject(HttpTestingController);
    TestBed.inject(CatalogService).search({ q: 'watch', sort: 'rating', categoryId: undefined }).subscribe();
    const req = http.expectOne(r => r.url === '/api/products');
    expect(req.request.params.get('q')).toBe('watch');
    expect(req.request.params.has('categoryId')).toBe(false);
    req.flush({ items: [], total: 0, page: 1, pageSize: 12 });
  });

  it('attaches the bearer token after login', () => {
    const http = TestBed.inject(HttpTestingController);
    const auth = TestBed.inject(AuthService);
    auth.login('a@b.c', 'password1').subscribe();
    http.expectOne('/api/auth/login').flush({
      userId: '1', email: 'a@b.c', userName: 'a', role: 'Customer', accessToken: 'tkn',
      expiresOnUtc: new Date(Date.now() + 3600_000).toISOString()
    });
    TestBed.inject(CatalogService).categories().subscribe();
    const req = http.expectOne('/api/categories');
    expect(req.request.headers.get('Authorization')).toBe('Bearer tkn');
    req.flush([]);
  });
});
