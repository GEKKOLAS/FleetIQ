import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from './config';
import { CreateInventoryItem, InventoryItemDetail, InventoryItemSummary, StockOperation } from './models';

@Injectable({ providedIn: 'root' })
export class InventoryService {
    private http = inject(HttpClient);
    private base = `${API_BASE}/inventoryitems`;

    list(opts: { warehouse?: string; lowStock?: boolean; take?: number } = {}): Observable<InventoryItemSummary[]> {
        let params = new HttpParams().set('take', String(opts.take ?? 200));
        if (opts.warehouse) params = params.set('warehouse', opts.warehouse);
        if (opts.lowStock) params = params.set('lowStock', 'true');
        return this.http.get<InventoryItemSummary[]>(this.base, { params });
    }

    get(id: string): Observable<InventoryItemDetail> {
        return this.http.get<InventoryItemDetail>(`${this.base}/${id}`);
    }

    create(body: CreateInventoryItem): Observable<{ id: string }> {
        return this.http.post<{ id: string }>(this.base, body);
    }

    move(id: string, operation: StockOperation, quantity: number, reference: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/${operation}`, { quantity, reference });
    }

    adjust(id: string, countedQuantity: number, reason: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/adjustments`, { countedQuantity, reason });
    }

    discontinue(id: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/discontinue`, {});
    }
}
