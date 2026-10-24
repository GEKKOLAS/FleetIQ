import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from './config';
import { CarrierSummary, LoadDetail, LoadSummary, Paged, RouteDetail } from './models';

@Injectable({ providedIn: 'root' })
export class PlanningService {
    private http = inject(HttpClient);
    private loads = `${API_BASE}/loads`;
    private routes = `${API_BASE}/routes`;

    list(opts: { status?: string; page?: number; pageSize?: number } = {}): Observable<Paged<LoadSummary>> {
        let params = new HttpParams().set('page', String(opts.page ?? 1)).set('pageSize', String(opts.pageSize ?? 100));
        if (opts.status) params = params.set('status', opts.status);
        return this.http.get<Paged<LoadSummary>>(this.loads, { params });
    }

    /** Verified carriers (from the Carriers service) usable for assignment. */
    listVerifiedCarriers(): Observable<Paged<CarrierSummary>> {
        return this.http.get<Paged<CarrierSummary>>(`${API_BASE}/carriers`, { params: { status: 'Verified', pageSize: '100' } });
    }

    get(id: string): Observable<LoadDetail> {
        return this.http.get<LoadDetail>(`${this.loads}/${id}`);
    }

    create(originCity: string, destinationCity: string): Observable<{ id: string }> {
        return this.http.post<{ id: string }>(this.loads, { originCity, destinationCity });
    }

    addOrder(id: string, body: { orderId: string; description: string; weightKg: number }): Observable<void> {
        return this.http.post<void>(`${this.loads}/${id}/orders`, body);
    }

    assignCarrier(id: string, carrierId: string): Observable<void> {
        return this.http.post<void>(`${this.loads}/${id}/assign-carrier`, { carrierId });
    }

    attachRoute(id: string, routeId: string): Observable<void> {
        return this.http.post<void>(`${this.loads}/${id}/attach-route`, { routeId });
    }

    plan(id: string): Observable<void> {
        return this.http.post<void>(`${this.loads}/${id}/plan`, {});
    }

    confirm(id: string): Observable<void> {
        return this.http.post<void>(`${this.loads}/${id}/confirm`, {});
    }

    cancel(id: string): Observable<void> {
        return this.http.post<void>(`${this.loads}/${id}/cancel`, {});
    }

    createRoute(loadId: string): Observable<{ id: string }> {
        return this.http.post<{ id: string }>(this.routes, { loadId });
    }

    addStop(routeId: string, body: { kind: string; city: string; address: string; latitude: number; longitude: number }): Observable<void> {
        return this.http.post<void>(`${this.routes}/${routeId}/stops`, body);
    }

    finalizeRoute(routeId: string): Observable<void> {
        return this.http.post<void>(`${this.routes}/${routeId}/finalize`, {});
    }

    getRoute(id: string): Observable<RouteDetail> {
        return this.http.get<RouteDetail>(`${this.routes}/${id}`);
    }
}
