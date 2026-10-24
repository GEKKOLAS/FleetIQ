import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from './config';
import { CreateIntegrationConnector, IntegrationConnectorSummary } from './models';

@Injectable({ providedIn: 'root' })
export class IntegrationService {
    private http = inject(HttpClient);
    private base = `${API_BASE}/integrationconnectors`;

    list(take = 50): Observable<IntegrationConnectorSummary[]> {
        const params = new HttpParams().set('take', String(take));
        return this.http.get<IntegrationConnectorSummary[]>(this.base, { params });
    }

    create(body: CreateIntegrationConnector): Observable<{ id: string }> {
        return this.http.post<{ id: string }>(this.base, body);
    }

    activate(id: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/activate`, {});
    }

    disable(id: string, reason: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/disable`, { reason });
    }

    recordSync(id: string, succeeded: boolean, error?: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/syncs`, { succeeded, error });
    }
}
