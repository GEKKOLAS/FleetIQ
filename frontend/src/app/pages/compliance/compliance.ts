import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ToolbarModule } from 'primeng/toolbar';
import { ComplianceCheckDetail, ComplianceCheckSummary } from '../../core/models';
import { ComplianceService } from '../../core/compliance.service';

type Sev = 'success' | 'info' | 'warn' | 'danger' | 'secondary';

@Component({
    selector: 'app-compliance',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, DialogModule, InputTextModule, SelectModule, TagModule, ToastModule, ToolbarModule],
    providers: [MessageService],
    template: `
        <p-toast />
        <div class="card">
            <p-toolbar styleClass="mb-4">
                <ng-template #start>
                    <div>
                        <span class="text-xl font-semibold">Compliance checks</span>
                        <p class="text-muted-color text-sm m-0 mt-1">High/Critical findings block a check from passing.</p>
                    </div>
                </ng-template>
                <ng-template #end>
                    <p-button label="Refresh" icon="pi pi-refresh" [text]="true" (onClick)="load()" />
                    <p-button label="New Check" icon="pi pi-plus" (onClick)="newDialog = true" />
                </ng-template>
            </p-toolbar>

            <p-table [value]="checks" [loading]="loading" [paginator]="true" [rows]="10" dataKey="id" [rowHover]="true">
                <ng-template #header>
                    <tr><th>Subject</th><th>Rule</th><th>Jurisdiction</th><th>Findings</th><th>Status</th><th>Created</th><th style="width: 8rem"></th></tr>
                </ng-template>
                <ng-template #body let-c>
                    <tr>
                        <td class="font-medium">{{ c.subjectType }} <span class="text-muted-color">{{ c.subjectId.slice(0, 8) }}…</span></td>
                        <td><code class="text-sm">{{ c.ruleCode }}</code></td>
                        <td>{{ c.jurisdiction }}</td>
                        <td>
                            {{ c.findingCount }}
                            @if (c.hasBlockingFindings) {
                                <i class="pi pi-exclamation-triangle text-red-500 ml-1" title="Has High/Critical findings"></i>
                            }
                        </td>
                        <td><p-tag [value]="c.status" [severity]="severity(c.status)" /></td>
                        <td>{{ c.createdOnUtc | date: 'short' }}</td>
                        <td><p-button label="Manage" icon="pi pi-sliders-h" [text]="true" size="small" (onClick)="manage(c)" /></td>
                    </tr>
                </ng-template>
                <ng-template #emptymessage><tr><td colspan="7" class="text-center p-4 text-muted-color">No compliance checks yet.</td></tr></ng-template>
            </p-table>
        </div>

        <p-dialog header="New compliance check" [(visible)]="newDialog" [modal]="true" [style]="{ width: '460px' }">
            <div class="flex flex-col gap-3">
                <div><label>Subject type</label><p-select [options]="subjectTypes" [(ngModel)]="form.subjectType" styleClass="w-full" /></div>
                <div><label>Subject id</label><input pInputText class="w-full" [(ngModel)]="form.subjectId" /></div>
                <div><label>Rule code</label><input pInputText class="w-full" [(ngModel)]="form.ruleCode" placeholder="HOS-11H" /></div>
                <div><label>Jurisdiction</label><input pInputText class="w-full" [(ngModel)]="form.jurisdiction" placeholder="EU" /></div>
            </div>
            <ng-template #footer>
                <p-button label="Cancel" [text]="true" (onClick)="newDialog = false" />
                <p-button label="Create" icon="pi pi-check" (onClick)="create()" [loading]="saving" />
            </ng-template>
        </p-dialog>

        <p-dialog [header]="detail ? detail.ruleCode + ' — ' + detail.subjectType : 'Check'" [(visible)]="manageDialog" [modal]="true" [style]="{ width: '640px' }">
            @if (detail; as d) {
                <div class="flex items-center gap-2 mb-3">
                    <p-tag [value]="d.status" [severity]="severity(d.status)" />
                    <span class="text-muted-color">{{ d.jurisdiction }} · subject {{ d.subjectId.slice(0, 8) }}…</span>
                </div>
                @if (d.waivedReason) {
                    <p class="text-muted-color text-sm mb-3"><i class="pi pi-info-circle"></i> Waived: {{ d.waivedReason }}</p>
                }

                <span class="font-semibold">Findings ({{ d.findings.length }})</span>
                <p-table [value]="d.findings" styleClass="mb-3 mt-1">
                    <ng-template #header><tr><th>Code</th><th>Severity</th><th>Message</th></tr></ng-template>
                    <ng-template #body let-f>
                        <tr>
                            <td><code class="text-sm">{{ f.code }}</code></td>
                            <td><p-tag [value]="f.severity" [severity]="findingSeverity(f.severity)" /></td>
                            <td>{{ f.message }}</td>
                        </tr>
                    </ng-template>
                    <ng-template #emptymessage><tr><td colspan="3" class="text-muted-color p-2">No findings recorded.</td></tr></ng-template>
                </p-table>

                @if (d.status === 'Pending') {
                    <div class="flex flex-wrap gap-2 mb-4">
                        <p-select [options]="severities" [(ngModel)]="fForm.severity" styleClass="w-32" />
                        <input pInputText placeholder="Message" [(ngModel)]="fForm.message" class="grow" style="min-width: 14rem" />
                        <p-button label="Add finding" icon="pi pi-plus" size="small" (onClick)="addFinding(d)" />
                    </div>
                }

                <hr class="my-3 border-surface" />
                <div class="flex flex-wrap gap-2 items-center">
                    <p-button label="Pass" icon="pi pi-check-circle" size="small" severity="success" [disabled]="d.status !== 'Pending'" (onClick)="pass(d)" />
                    <p-button label="Fail" icon="pi pi-times-circle" size="small" severity="danger" [text]="true" [disabled]="d.status !== 'Pending' || d.findings.length === 0" (onClick)="fail(d)" />
                    @if (d.status === 'Failed') {
                        <input pInputText placeholder="Waive reason" [(ngModel)]="waiveReason" style="width: 14rem" />
                        <p-button label="Waive" icon="pi pi-shield" size="small" [outlined]="true" (onClick)="waive(d)" />
                    }
                </div>
            }
        </p-dialog>
    `
})
export class Compliance implements OnInit {
    private api = inject(ComplianceService);
    private toast = inject(MessageService);

    checks: ComplianceCheckSummary[] = [];
    loading = false;
    saving = false;
    newDialog = false;
    manageDialog = false;
    detail: ComplianceCheckDetail | null = null;
    waiveReason = '';

    subjectTypes = ['Carrier', 'Driver', 'Vehicle', 'Order', 'Load', 'Shipment'];
    severities = ['Low', 'Medium', 'High', 'Critical'];
    form = { subjectId: '22222222-2222-2222-2222-222222222222', subjectType: 'Carrier', ruleCode: '', jurisdiction: 'EU' };
    fForm = { severity: 'Low', message: '' };

    ngOnInit() { this.load(); }

    load() {
        this.loading = true;
        this.api.list(100).subscribe({
            next: (items) => { this.checks = items; this.loading = false; },
            error: () => { this.loading = false; this.err('Could not load compliance checks'); }
        });
    }

    create() {
        this.saving = true;
        this.api.create({ ...this.form }).subscribe({
            next: () => { this.saving = false; this.newDialog = false; this.form.ruleCode = ''; this.ok('Check created'); this.load(); },
            error: (e) => { this.saving = false; this.problem(e); }
        });
    }

    manage(c: ComplianceCheckSummary) {
        this.api.get(c.id).subscribe({ next: (d) => { this.detail = d; this.manageDialog = true; }, error: () => this.err('Could not load check') });
    }

    private refresh(id: string) { this.api.get(id).subscribe({ next: (d) => (this.detail = d) }); this.load(); }

    addFinding(d: ComplianceCheckDetail) {
        this.api.addFinding(d.id, { ...this.fForm }).subscribe({ next: () => { this.fForm.message = ''; this.ok('Finding recorded'); this.refresh(d.id); }, error: (e) => this.problem(e) });
    }
    pass(d: ComplianceCheckDetail) {
        this.api.pass(d.id).subscribe({ next: () => { this.ok('Check passed'); this.refresh(d.id); }, error: (e) => this.problem(e) });
    }
    fail(d: ComplianceCheckDetail) {
        this.api.fail(d.id).subscribe({ next: () => { this.ok('Check failed'); this.refresh(d.id); }, error: (e) => this.problem(e) });
    }
    waive(d: ComplianceCheckDetail) {
        this.api.waive(d.id, this.waiveReason).subscribe({ next: () => { this.waiveReason = ''; this.ok('Check waived'); this.refresh(d.id); }, error: (e) => this.problem(e) });
    }

    severity(status: string): Sev {
        switch (status) {
            case 'Passed': return 'success';
            case 'Failed': return 'danger';
            case 'Waived': return 'info';
            case 'Pending': return 'warn';
            default: return 'secondary';
        }
    }

    findingSeverity(severity: string): Sev {
        switch (severity) {
            case 'Critical':
            case 'High': return 'danger';
            case 'Medium': return 'warn';
            default: return 'secondary';
        }
    }

    private ok(detail: string) { this.toast.add({ severity: 'success', summary: 'OK', detail }); }
    private err(detail: string) { this.toast.add({ severity: 'error', summary: 'Error', detail }); }
    private problem(e: any) { this.toast.add({ severity: 'warn', summary: 'Rejected', detail: e?.error?.description || e?.error?.title || e?.message || 'Request failed' }); }
}
