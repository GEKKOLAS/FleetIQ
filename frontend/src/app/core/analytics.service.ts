import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from './config';
import { CreateReportDefinition, ReportDefinitionSummary } from './models';

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
    private http = inject(HttpClient);
    private base = `${API_BASE}/reportdefinitions`;

    list(take = 50): Observable<ReportDefinitionSummary[]> {
        const params = new HttpParams().set('take', String(take));
        return this.http.get<ReportDefinitionSummary[]>(this.base, { params });
    }

    create(body: CreateReportDefinition): Observable<{ id: string }> {
        return this.http.post<{ id: string }>(this.base, body);
    }

    archive(id: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/archive`, {});
    }
}
