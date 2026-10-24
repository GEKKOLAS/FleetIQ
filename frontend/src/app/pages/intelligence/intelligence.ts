import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { ToolbarModule } from 'primeng/toolbar';
import { PredictionRequestSummary } from '../../core/models';
import { IntelligenceService } from '../../core/intelligence.service';

type Sev = 'success' | 'info' | 'warn' | 'danger' | 'secondary';

@Component({
    selector: 'app-intelligence',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, DialogModule, InputTextModule, InputNumberModule, SelectModule, TagModule, TextareaModule, ToastModule, TooltipModule, ToolbarModule],
    providers: [MessageService],
    template: `
        <p-toast />
        <div class="card">
            <p-toolbar styleClass="mb-4">
                <ng-template #start>
                    <div>
                        <span class="text-xl font-semibold">Intelligence — predictions</span>
                        <p class="text-muted-color text-sm m-0 mt-1">ETA, delay, price and anomaly predictions with confidence scores.</p>
                    </div>
                </ng-template>
                <ng-template #end>
                    <p-button label="Refresh" icon="pi pi-refresh" [text]="true" (onClick)="load()" />
                    <p-button label="New Prediction" icon="pi pi-plus" (onClick)="newDialog = true" />
                </ng-template>
            </p-toolbar>

            <p-table [value]="predictions" [loading]="loading" [paginator]="true" [rows]="10" dataKey="id" [rowHover]="true">
                <ng-template #header>
                    <tr><th>Model</th><th>Type</th><th>Status</th><th>Confidence</th><th>Finished</th><th style="width: 14rem"></th></tr>
                </ng-template>
                <ng-template #body let-p>
                    <tr>
                        <td class="font-medium"><code class="text-sm">{{ p.modelId }}</code></td>
                        <td>{{ p.predictionType }}</td>
                        <td>
                            <p-tag [value]="p.status" [severity]="severity(p.status)" />
                            @if (p.error) {
                                <i class="pi pi-info-circle text-muted-color ml-1" [pTooltip]="p.error"></i>
                            }
                        </td>
                        <td style="font-variant-numeric: tabular-nums">{{ p.confidence != null ? (p.confidence | percent: '1.0-1') : '—' }}</td>
                        <td>{{ p.finishedOnUtc ? (p.finishedOnUtc | date: 'short') : '—' }}</td>
                        <td class="flex gap-1">
                            <p-button label="Start" [text]="true" size="small" [disabled]="p.status !== 'Requested'" (onClick)="startPrediction(p)" />
                            <p-button label="Complete" [text]="true" size="small" severity="success" [disabled]="p.status !== 'Running'" (onClick)="askComplete(p)" />
                            <p-button label="Fail" [text]="true" size="small" severity="danger" [disabled]="p.status !== 'Requested' && p.status !== 'Running'" (onClick)="fail(p)" />
                        </td>
                    </tr>
                </ng-template>
                <ng-template #emptymessage><tr><td colspan="6" class="text-center p-4 text-muted-color">No prediction requests yet.</td></tr></ng-template>
            </p-table>
        </div>

        <p-dialog header="New prediction request" [(visible)]="newDialog" [modal]="true" [style]="{ width: '440px' }">
            <div class="flex flex-col gap-3">
                <div><label>Model id</label><input pInputText class="w-full" [(ngModel)]="form.modelId" placeholder="eta-v2" /></div>
                <div><label>Type</label><p-select [options]="types" [(ngModel)]="form.predictionType" styleClass="w-full" /></div>
            </div>
            <ng-template #footer>
                <p-button label="Cancel" [text]="true" (onClick)="newDialog = false" />
                <p-button label="Create" icon="pi pi-check" (onClick)="create()" [loading]="saving" />
            </ng-template>
        </p-dialog>

        <p-dialog header="Complete prediction" [(visible)]="completeDialog" [modal]="true" [style]="{ width: '520px' }">
            <div class="flex flex-col gap-3">
                <div>
                    <label>Result JSON</label>
                    <textarea pTextarea class="w-full" rows="4" [(ngModel)]="cForm.resultJson"></textarea>
                </div>
                <div>
                    <label>Confidence (0–1)</label>
                    <p-inputnumber [(ngModel)]="cForm.confidence" [min]="0" [max]="1" [step]="0.05" [minFractionDigits]="2" styleClass="w-full" />
                </div>
            </div>
            <ng-template #footer>
                <p-button label="Cancel" [text]="true" (onClick)="completeDialog = false" />
                <p-button label="Complete" icon="pi pi-check" severity="success" (onClick)="complete()" />
            </ng-template>
        </p-dialog>
    `
})
export class Intelligence implements OnInit {
    private api = inject(IntelligenceService);
    private toast = inject(MessageService);

    predictions: PredictionRequestSummary[] = [];
    loading = false;
    saving = false;
    newDialog = false;
    completeDialog = false;
    private completeTarget: PredictionRequestSummary | null = null;

    types = ['Eta', 'Delay', 'Price', 'Anomaly'];
    form = { modelId: '', predictionType: 'Eta' };
    cForm = { resultJson: '{"etaUtc":"2026-07-05T10:00:00Z"}', confidence: 0.9 };

    ngOnInit() { this.load(); }

    load() {
        this.loading = true;
        this.api.list(100).subscribe({
            next: (items) => { this.predictions = items; this.loading = false; },
            error: () => { this.loading = false; this.err('Could not load predictions'); }
        });
    }

    create() {
        this.saving = true;
        this.api.create({ ...this.form }).subscribe({
            next: () => { this.saving = false; this.newDialog = false; this.form.modelId = ''; this.ok('Prediction requested'); this.load(); },
            error: (e) => { this.saving = false; this.problem(e); }
        });
    }

    startPrediction(p: PredictionRequestSummary) {
        this.api.start(p.id).subscribe({ next: () => { this.ok('Prediction running'); this.load(); }, error: (e) => this.problem(e) });
    }

    askComplete(p: PredictionRequestSummary) {
        this.completeTarget = p;
        this.completeDialog = true;
    }

    complete() {
        if (!this.completeTarget) return;
        this.api.complete(this.completeTarget.id, this.cForm.resultJson, this.cForm.confidence).subscribe({
            next: () => { this.completeDialog = false; this.ok('Prediction completed'); this.load(); },
            error: (e) => this.problem(e)
        });
    }

    fail(p: PredictionRequestSummary) {
        this.api.fail(p.id, 'Manually failed from UI').subscribe({ next: () => { this.ok('Prediction failed'); this.load(); }, error: (e) => this.problem(e) });
    }

    severity(status: string): Sev {
        switch (status) {
            case 'Completed': return 'success';
            case 'Running': return 'info';
            case 'Requested': return 'warn';
            case 'Failed': return 'danger';
            default: return 'secondary';
        }
    }

    private ok(detail: string) { this.toast.add({ severity: 'success', summary: 'OK', detail }); }
    private err(detail: string) { this.toast.add({ severity: 'error', summary: 'Error', detail }); }
    private problem(e: any) { this.toast.add({ severity: 'warn', summary: 'Rejected', detail: e?.error?.description || e?.error?.title || e?.message || 'Request failed' }); }
}
