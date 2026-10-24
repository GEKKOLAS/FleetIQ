import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from './config';
import { CreateInvoice, InvoiceDetail, InvoiceSummary, Paged } from './models';

@Injectable({ providedIn: 'root' })
export class BillingService {
    private http = inject(HttpClient);
    private base = `${API_BASE}/invoices`;

    list(opts: { customerId?: string; status?: string; page?: number; pageSize?: number; sort?: string } = {}): Observable<Paged<InvoiceSummary>> {
        let params = new HttpParams();
        if (opts.customerId) params = params.set('customerId', opts.customerId);
        if (opts.status) params = params.set('status', opts.status);
        params = params.set('page', String(opts.page ?? 1)).set('pageSize', String(opts.pageSize ?? 20));
        if (opts.sort) params = params.set('sort', opts.sort ?? 'created_desc');
        return this.http.get<Paged<InvoiceSummary>>(this.base, { params });
    }

    get(id: string): Observable<InvoiceDetail> {
        return this.http.get<InvoiceDetail>(`${this.base}/${id}`);
    }

    create(body: CreateInvoice): Observable<{ id: string }> {
        return this.http.post<{ id: string }>(this.base, body);
    }

    addLine(id: string, line: { description: string; quantity: number; unitPrice: number }): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/lines`, line);
    }

    issue(id: string, dueDateUtc: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/issue`, { dueDateUtc });
    }

    recordPayment(id: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/payment`, {});
    }

    cancel(id: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/cancel`, {});
    }
}
