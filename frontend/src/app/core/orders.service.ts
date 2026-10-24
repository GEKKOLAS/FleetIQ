import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from './config';
import { CreateOrder, OrderDetail, OrderSummary, Paged } from './models';

@Injectable({ providedIn: 'root' })
export class OrdersService {
    private http = inject(HttpClient);
    private base = `${API_BASE}/orders`;

    list(opts: { customerId?: string; status?: string; page?: number; pageSize?: number; sort?: string } = {}): Observable<Paged<OrderSummary>> {
        let params = new HttpParams();
        if (opts.customerId) params = params.set('customerId', opts.customerId);
        if (opts.status) params = params.set('status', opts.status);
        params = params.set('page', String(opts.page ?? 1)).set('pageSize', String(opts.pageSize ?? 20));
        if (opts.sort) params = params.set('sort', opts.sort);
        return this.http.get<Paged<OrderSummary>>(this.base, { params });
    }

    get(id: string): Observable<OrderDetail> {
        return this.http.get<OrderDetail>(`${this.base}/${id}`);
    }

    create(body: CreateOrder): Observable<{ id: string }> {
        return this.http.post<{ id: string }>(this.base, body);
    }

    addItem(id: string, item: { description: string; quantity: number; weightKg: number }): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/items`, item);
    }

    confirm(id: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/confirm`, {});
    }

    readyForPlanning(id: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/ready-for-planning`, {});
    }

    cancel(id: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/cancel`, {});
    }
}
