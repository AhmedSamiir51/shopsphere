export interface Product {
  id: string; name: string; sku: string; description: string; price: number; currency: string;
  imageUrl: string; categoryId: string; rating: number; reviewCount: number;
}
export interface Paged<T> { items: T[]; total: number; page: number; pageSize: number; }
export interface Category { id: string; name: string; slug: string; }
export interface CartLine { productId: string; productName: string; unitPrice: number; imageUrl: string; quantity: number; lineTotal: number; }
export interface Cart { userId: string; lines: CartLine[]; total: number; }
export interface AuthResponse { userId: string; email: string; userName: string; role: string; accessToken: string; expiresOnUtc: string; }
export interface OrderItem { productId: string; productName: string; quantity: number; unitPrice: number; }
export interface Order {
  id: string; userId: string; status: string; total: number; currency: string; createdOnUtc: string;
  cancellationReason: string | null; items: OrderItem[];
}
export interface ProductQuery { q?: string; categoryId?: string; minPrice?: number; maxPrice?: number; sort?: string; page?: number; pageSize?: number; }
