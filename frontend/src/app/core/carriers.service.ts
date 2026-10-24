import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from './config';
import { CarrierDetail, CarrierSummary, Paged } from './models';

@Injectable({ providedIn: 'root' })
export class CarriersService {
    private http = inject(HttpClient);
    private base = `${API_BASE}/carriers`;

    list(opts: { status?: string; page?: number; pageSize?: number } = {}): Observable<Paged<CarrierSummary>> {
        let params = new HttpParams().set('page', String(opts.page ?? 1)).set('pageSize', String(opts.pageSize ?? 100));
        if (opts.status) params = params.set('status', opts.status);
        return this.http.get<Paged<CarrierSummary>>(this.base, { params });
    }

    get(id: string): Observable<CarrierDetail> {
        return this.http.get<CarrierDetail>(`${this.base}/${id}`);
    }

    create(body: { legalName: string; registrationNumber: string; country: string }): Observable<{ id: string }> {
        return this.http.post<{ id: string }>(this.base, body);
    }

    addVehicle(id: string, body: { plate: string; type: string; capacityKg: number }): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/vehicles`, body);
    }

    addDriver(id: string, body: { fullName: string; licenceNumber: string }): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/drivers`, body);
    }

    verify(id: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/verify`, {});
    }

    suspend(id: string, reason: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/suspend`, { reason });
    }

    reactivate(id: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/reactivate`, {});
    }
}
