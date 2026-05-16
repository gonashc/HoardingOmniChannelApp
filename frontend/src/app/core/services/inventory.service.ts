import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CartLineItem,
  CreateBookingRequest,
  CreateBookingResult,
  InventoryDetail,
  InventoryListItem,
  InventorySearchCriteria,
  PagedResult,
  QuoteResponse,
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private http = inject(HttpClient);
  private base = `${environment.apiBaseUrl}/inventory`;

  search(criteria: InventorySearchCriteria): Observable<PagedResult<InventoryListItem>> {
    let params = new HttpParams();
    Object.entries(criteria).forEach(([k, v]) => {
      if (v === undefined || v === null || v === '') return;
      if (Array.isArray(v)) v.forEach(x => (params = params.append(k, String(x))));
      else params = params.set(k, String(v));
    });
    return this.http.get<PagedResult<InventoryListItem>>(this.base, { params });
  }

  trending(channel?: string, cityId?: number, limit = 10): Observable<InventoryListItem[]> {
    let params = new HttpParams().set('limit', limit);
    if (channel) params = params.set('channel', channel);
    if (cityId) params = params.set('cityId', cityId);
    return this.http.get<InventoryListItem[]>(`${this.base}/trending`, { params });
  }

  detail(id: string): Observable<InventoryDetail> {
    return this.http.get<InventoryDetail>(`${this.base}/${id}`);
  }

  quote(
    id: string,
    startDate: string,
    endDate: string,
    deliverableSpec?: string,
    includeSetupCost = true,
  ): Observable<QuoteResponse> {
    let params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate)
      .set('includeSetupCost', includeSetupCost);
    if (deliverableSpec) params = params.set('deliverableSpec', deliverableSpec);
    return this.http.get<QuoteResponse>(`${this.base}/${id}/quote`, { params });
  }
}

@Injectable({ providedIn: 'root' })
export class BookingService {
  private http = inject(HttpClient);
  private base = `${environment.apiBaseUrl}/bookings`;

  create(req: CreateBookingRequest): Observable<CreateBookingResult> {
    return this.http.post<CreateBookingResult>(this.base, req);
  }
}

@Injectable({ providedIn: 'root' })
export class CartService {
  private items = signal<CartLineItem[]>(this.load());
  readonly cart = this.items.asReadonly();

  add(line: CartLineItem) {
    const next = [...this.items(), line];
    this.items.set(next);
    this.persist(next);
  }
  remove(unitId: string) {
    const next = this.items().filter(x => x.inventoryUnit.id !== unitId);
    this.items.set(next);
    this.persist(next);
  }
  clear() {
    this.items.set([]);
    this.persist([]);
  }
  total(): number {
    return this.items().reduce((sum, x) => sum + (x.quotedPrice ?? 0), 0);
  }
  count(): number {
    return this.items().length;
  }

  private persist(v: CartLineItem[]) {
    if (typeof localStorage !== 'undefined') localStorage.setItem('cart_v2', JSON.stringify(v));
  }
  private load(): CartLineItem[] {
    if (typeof localStorage === 'undefined') return [];
    try {
      return JSON.parse(localStorage.getItem('cart_v2') ?? '[]');
    } catch {
      return [];
    }
  }
}
