import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from './config';
import { ComplianceCheckDetail, ComplianceCheckSummary, CreateComplianceCheck } from './models';

@Injectable({ providedIn: 'root' })
export class ComplianceService {
    private http = inject(HttpClient);
    private base = `${API_BASE}/compliancechecks`;

    list(take = 50): Observable<ComplianceCheckSummary[]> {
        const params = new HttpParams().set('take', String(take));
        return this.http.get<ComplianceCheckSummary[]>(this.base, { params });
    }

    get(id: string): Observable<ComplianceCheckDetail> {
        return this.http.get<ComplianceCheckDetail>(`${this.base}/${id}`);
    }

    create(body: CreateComplianceCheck): Observable<{ id: string }> {
        return this.http.post<{ id: string }>(this.base, body);
    }

    addFinding(id: string, body: { code?: string; severity: string; message: string }): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/findings`, body);
    }

    pass(id: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/pass`, {});
    }

    fail(id: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/fail`, {});
    }

    waive(id: string, reason: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/waive`, { reason });
    }
}
