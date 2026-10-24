import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { ToolbarModule } from 'primeng/toolbar';
import { ReportDefinitionSummary } from '../../core/models';
import { AnalyticsService } from '../../core/analytics.service';

@Component({
    selector: 'app-analytics',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, DialogModule, InputTextModule, TextareaModule, TagModule, ToastModule, ToolbarModule],
    providers: [MessageService],
    template: `
        <p-toast />
        <div class="card">
            <p-toolbar styleClass="mb-4">
                <ng-template #start>
                    <div>
                        <span class="text-xl font-semibold">Analytics — report definitions</span>
                        <p class="text-muted-color text-sm m-0 mt-1">Saved queries that power scheduled reports.</p>
                    </div>
                </ng-template>
                <ng-template #end>
                    <p-button label="Refresh" icon="pi pi-refresh" [text]="true" (onClick)="load()" />
                    <p-button label="New Report" icon="pi pi-plus" (onClick)="newDialog = true" />
                </ng-template>
            </p-toolbar>

            <p-table [value]="reports" [loading]="loading" [paginator]="true" [rows]="10" dataKey="id" [rowHover]="true">
                <ng-template #header>
                    <tr><th>Name</th><th>Query</th><th>Status</th><th>Created</th><th style="width: 8rem"></th></tr>
                </ng-template>
                <ng-template #body let-r>
                    <tr>
                        <td class="font-medium">{{ r.name }}</td>
                        <td><code class="text-sm text-muted-color">{{ r.query.length > 60 ? r.query.slice(0, 60) + '…' : r.query }}</code></td>
                        <td><p-tag [value]="r.status" [severity]="r.status === 'Active' ? 'success' : 'secondary'" /></td>
                        <td>{{ r.createdOnUtc | date: 'short' }}</td>
                        <td>
                            <p-button label="Archive" icon="pi pi-inbox" [text]="true" size="small" severity="secondary"
                                      [disabled]="r.status !== 'Active'" (onClick)="archive(r)" />
                        </td>
                    </tr>
                </ng-template>
                <ng-template #emptymessage><tr><td colspan="5" class="text-center p-4 text-muted-color">No report definitions yet.</td></tr></ng-template>
            </p-table>
        </div>

        <p-dialog header="New report definition" [(visible)]="newDialog" [modal]="true" [style]="{ width: '520px' }">
            <div class="flex flex-col gap-3">
                <div><label>Name</label><input pInputText class="w-full" [(ngModel)]="form.name" placeholder="Weekly on-time delivery" /></div>
                <div>
                    <label>Query</label>
                    <textarea pTextarea class="w-full" rows="4" [(ngModel)]="form.query" placeholder="SELECT carrier, avg(delay) FROM shipments GROUP BY carrier"></textarea>
                </div>
            </div>
            <ng-template #footer>
                <p-button label="Cancel" [text]="true" (onClick)="newDialog = false" />
                <p-button label="Create" icon="pi pi-check" (onClick)="create()" [loading]="saving" />
            </ng-template>
        </p-dialog>
    `
})
export class Analytics implements OnInit {
    private api = inject(AnalyticsService);
    private toast = inject(MessageService);

    reports: ReportDefinitionSummary[] = [];
    loading = false;
    saving = false;
    newDialog = false;

    form = { name: '', query: '' };

    ngOnInit() { this.load(); }

    load() {
        this.loading = true;
        this.api.list(100).subscribe({
            next: (items) => { this.reports = items; this.loading = false; },
            error: () => { this.loading = false; this.err('Could not load report definitions'); }
        });
    }

    create() {
        this.saving = true;
        this.api.create({ ...this.form }).subscribe({
            next: () => { this.saving = false; this.newDialog = false; this.form = { name: '', query: '' }; this.ok('Report definition created'); this.load(); },
            error: (e) => { this.saving = false; this.problem(e); }
        });
    }

    archive(r: ReportDefinitionSummary) {
        this.api.archive(r.id).subscribe({ next: () => { this.ok('Report archived'); this.load(); }, error: (e) => this.problem(e) });
    }

    private ok(detail: string) { this.toast.add({ severity: 'success', summary: 'OK', detail }); }
    private err(detail: string) { this.toast.add({ severity: 'error', summary: 'Error', detail }); }
    private problem(e: any) { this.toast.add({ severity: 'warn', summary: 'Rejected', detail: e?.error?.title || e?.error?.detail || e?.message || 'Request failed' }); }
}
