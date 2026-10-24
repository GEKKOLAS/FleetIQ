import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from './config';
import { CreateNotification, NotificationSummary } from './models';

@Injectable({ providedIn: 'root' })
export class NotificationsService {
    private http = inject(HttpClient);
    private base = `${API_BASE}/notifications`;

    list(take = 50): Observable<NotificationSummary[]> {
        const params = new HttpParams().set('take', String(take));
        return this.http.get<NotificationSummary[]>(this.base, { params });
    }

    create(body: CreateNotification): Observable<{ id: string }> {
        return this.http.post<{ id: string }>(this.base, body);
    }

    markSent(id: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/marksent`, {});
    }
}
