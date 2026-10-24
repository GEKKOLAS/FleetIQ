import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { ToolbarModule } from 'primeng/toolbar';
import { IntegrationConnectorSummary } from '../../core/models';
import { IntegrationService } from '../../core/integration.service';

type Sev = 'success' | 'info' | 'warn' | 'danger' | 'secondary';

@Component({
    selector: 'app-integration',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, DialogModule, InputTextModule, TagModule, ToastModule, TooltipModule, ToolbarModule],
    providers: [MessageService],
    template: `
        <p-toast />
        <div class="card">
            <p-toolbar styleClass="mb-4">
                <ng-template #start>
                    <div>
                        <span class="text-xl font-semibold">Integration connectors</span>
                        <p class="text-muted-color text-sm m-0 mt-1">3 consecutive sync failures auto-disable a connector.</p>
                    </div>
                </ng-template>
                <ng-template #end>
                    <p-button label="Refresh" icon="pi pi-refresh" [text]="true" (onClick)="load()" />
                    <p-button label="New Connector" icon="pi pi-plus" (onClick)="newDialog = true" />
                </ng-template>
            </p-toolbar>

            <p-table [value]="connectors" [loading]="loading" [paginator]="true" [rows]="10" dataKey="id" [rowHover]="true">
                <ng-template #header>
                    <tr><th>System</th><th>Endpoint</th><th>Status</th><th>Last sync</th><th>Failures</th><th style="width: 16rem"></th></tr>
                </ng-template>
                <ng-template #body let-c>
                    <tr>
                        <td class="font-medium">{{ c.systemName }}</td>
                        <td class="text-muted-color">{{ c.endpoint }}</td>
                        <td>
                            <p-tag [value]="c.status" [severity]="severity(c.status)" />
                            @if (c.disabledReason) {
                                <i class="pi pi-info-circle text-muted-color ml-1" [pTooltip]="c.disabledReason"></i>
                            }
                        </td>
                        <td>{{ c.lastSyncOnUtc ? (c.lastSyncOnUtc | date: 'short') : '—' }}</td>
                        <td>{{ c.consecutiveFailures }}</td>
                        <td class="flex gap-1">
                            <p-button label="Activate" [text]="true" size="small" severity="success" [disabled]="c.status === 'Active'" (onClick)="activate(c)" />
                            <p-button label="Sync OK" [text]="true" size="small" [disabled]="c.status !== 'Active'" (onClick)="sync(c, true)" />
                            <p-button label="Sync fail" [text]="true" size="small" severity="danger" [disabled]="c.status !== 'Active'" (onClick)="sync(c, false)" />
                            <p-button label="Disable" [text]="true" size="small" severity="secondary" [disabled]="c.status === 'Disabled'" (onClick)="askDisable(c)" />
                        </td>
                    </tr>
                </ng-template>
                <ng-template #emptymessage><tr><td colspan="6" class="text-center p-4 text-muted-color">No connectors yet.</td></tr></ng-template>
            </p-table>
        </div>

        <p-dialog header="New connector" [(visible)]="newDialog" [modal]="true" [style]="{ width: '460px' }">
            <div class="flex flex-col gap-3">
                <div><label>System name</label><input pInputText class="w-full" [(ngModel)]="form.systemName" placeholder="SAP ERP" /></div>
                <div><label>Endpoint</label><input pInputText class="w-full" [(ngModel)]="form.endpoint" placeholder="https://erp.example.com/api" /></div>
            </div>
            <ng-template #footer>
                <p-button label="Cancel" [text]="true" (onClick)="newDialog = false" />
                <p-button label="Create" icon="pi pi-check" (onClick)="create()" [loading]="saving" />
            </ng-template>
        </p-dialog>

        <p-dialog header="Disable connector" [(visible)]="disableDialog" [modal]="true" [style]="{ width: '420px' }">
            <div><label>Reason</label><input pInputText class="w-full" [(ngModel)]="disableReason" placeholder="Maintenance window" /></div>
            <ng-template #footer>
                <p-button label="Cancel" [text]="true" (onClick)="disableDialog = false" />
                <p-button label="Disable" icon="pi pi-ban" severity="danger" (onClick)="disable()" />
            </ng-template>
        </p-dialog>
    `
})
export class Integration implements OnInit {
    private api = inject(IntegrationService);
    private toast = inject(MessageService);

    connectors: IntegrationConnectorSummary[] = [];
    loading = false;
    saving = false;
    newDialog = false;
    disableDialog = false;
    disableReason = '';
    private disableTarget: IntegrationConnectorSummary | null = null;

    form = { systemName: '', endpoint: '' };

    ngOnInit() { this.load(); }

    load() {
        this.loading = true;
        this.api.list(100).subscribe({
            next: (items) => { this.connectors = items; this.loading = false; },
            error: () => { this.loading = false; this.err('Could not load connectors'); }
        });
    }

    create() {
        this.saving = true;
        this.api.create({ ...this.form }).subscribe({
            next: () => { this.saving = false; this.newDialog = false; this.form = { systemName: '', endpoint: '' }; this.ok('Connector registered'); this.load(); },
            error: (e) => { this.saving = false; this.problem(e); }
        });
    }

    activate(c: IntegrationConnectorSummary) {
        this.api.activate(c.id).subscribe({ next: () => { this.ok('Connector activated'); this.load(); }, error: (e) => this.problem(e) });
    }

    sync(c: IntegrationConnectorSummary, succeeded: boolean) {
        this.api.recordSync(c.id, succeeded, succeeded ? undefined : 'Manual test failure').subscribe({
            next: () => { this.ok(succeeded ? 'Sync recorded' : 'Failure recorded'); this.load(); },
            error: (e) => this.problem(e)
        });
    }

    askDisable(c: IntegrationConnectorSummary) {
        this.disableTarget = c;
        this.disableReason = '';
        this.disableDialog = true;
    }

    disable() {
        if (!this.disableTarget) return;
        this.api.disable(this.disableTarget.id, this.disableReason).subscribe({
            next: () => { this.disableDialog = false; this.ok('Connector disabled'); this.load(); },
            error: (e) => this.problem(e)
        });
    }

    severity(status: string): Sev {
        switch (status) {
            case 'Active': return 'success';
            case 'Registered': return 'warn';
            case 'Disabled': return 'danger';
            default: return 'secondary';
        }
    }

    private ok(detail: string) { this.toast.add({ severity: 'success', summary: 'OK', detail }); }
    private err(detail: string) { this.toast.add({ severity: 'error', summary: 'Error', detail }); }
    private problem(e: any) { this.toast.add({ severity: 'warn', summary: 'Rejected', detail: e?.error?.description || e?.error?.title || e?.message || 'Request failed' }); }
}
