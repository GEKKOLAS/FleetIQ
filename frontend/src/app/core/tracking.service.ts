import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from './config';
import { Paged, ShipmentSummary } from './models';

@Injectable({ providedIn: 'root' })
export class TrackingService {
    private http = inject(HttpClient);
    private base = `${API_BASE}/shipments`;

    list(opts: { status?: string; page?: number; pageSize?: number } = {}): Observable<Paged<ShipmentSummary>> {
        let params = new HttpParams().set('page', String(opts.page ?? 1)).set('pageSize', String(opts.pageSize ?? 100));
        if (opts.status) params = params.set('status', opts.status);
        return this.http.get<Paged<ShipmentSummary>>(this.base, { params });
    }

    create(loadId: string, carrierId: string): Observable<{ id: string }> {
        return this.http.post<{ id: string }>(this.base, { loadId, carrierId });
    }

    dispatch(id: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/dispatch`, {});
    }

    deliver(id: string, receivedBy: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/deliver`, { receivedBy });
    }
}
