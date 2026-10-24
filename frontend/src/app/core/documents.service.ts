import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from './config';
import { CreateDocument, DocumentSummary } from './models';

@Injectable({ providedIn: 'root' })
export class DocumentsService {
    private http = inject(HttpClient);
    private base = `${API_BASE}/documents`;

    list(take = 50): Observable<DocumentSummary[]> {
        const params = new HttpParams().set('take', String(take));
        return this.http.get<DocumentSummary[]>(this.base, { params });
    }

    create(body: CreateDocument): Observable<{ id: string }> {
        return this.http.post<{ id: string }>(this.base, body);
    }

    verify(id: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/verify`, {});
    }
}
