import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from './config';
import { CreatePredictionRequest, PredictionRequestSummary } from './models';

@Injectable({ providedIn: 'root' })
export class IntelligenceService {
    private http = inject(HttpClient);
    private base = `${API_BASE}/predictionrequests`;

    list(take = 50): Observable<PredictionRequestSummary[]> {
        const params = new HttpParams().set('take', String(take));
        return this.http.get<PredictionRequestSummary[]>(this.base, { params });
    }

    create(body: CreatePredictionRequest): Observable<{ id: string }> {
        return this.http.post<{ id: string }>(this.base, body);
    }

    start(id: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/start`, {});
    }

    complete(id: string, resultJson: string, confidence: number): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/complete`, { resultJson, confidence });
    }

    fail(id: string, error: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/fail`, { error });
    }
}
