import { CommonModule } from '@angular/common';
import { TABLE_CELLS, partyName } from '../../shared/table-cells';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ToolbarModule } from 'primeng/toolbar';
import { TooltipModule } from 'primeng/tooltip';
import { InvoiceSummary } from '../../core/models';
import { BillingService } from '../../core/billing.service';

const DEFAULT_CUSTOMER = '22222222-2222-2222-2222-222222222222';

@Component({
    selector: 'app-billing',
    standalone: true,
    imports: [...TABLE_CELLS, CommonModule, FormsModule, TableModule, ButtonModule, DialogModule, InputTextModule, InputNumberModule, TagModule, ToastModule, ToolbarModule, TooltipModule],
    providers: [MessageService],
    template: `
        <p-toast />
        <div class="card">
            <p-toolbar styleClass="mb-4">
                <ng-template #start><span class="text-xl font-semibold">Invoices</span></ng-template>
                <ng-template #end>
                    <span class="p-input-icon-left table-search"><i class="pi pi-search"></i><input pInputText type="search" placeholder="Search" aria-label="Search table" (input)="dt.filterGlobal($any($event.target).value, 'contains')" /></span>
                    <p-button label="Refresh" icon="pi pi-refresh" [text]="true" (onClick)="load()" />
                    <p-button label="New Invoice" icon="pi pi-plus" (onClick)="dialog = true" />
                </ng-template>
            </p-toolbar>

            <p-table #dt [globalFilterFields]="['status','currency','customerName']" [value]="invoices" [loading]="loading" [paginator]="true" [rows]="10" dataKey="id" [rowHover]="true">
                <ng-template #header>
                    <tr>
                        <th pSortableColumn="customerName">Customer <p-sortIcon field="customerName" /></th>
                        <th pSortableColumn="status">Status <p-sortIcon field="status" /></th>
                        <th class="text-right" pSortableColumn="total">Total <p-sortIcon field="total" /></th>
                        <th pSortableColumn="createdOnUtc">Created <p-sortIcon field="createdOnUtc" /></th>
                        <th style="width: 14rem"></th>
                    </tr>
                </ng-template>
                <ng-template #body let-i>
                    <tr>
                        <td><app-party [id]="i.customerId" /></td>
                        <td><app-status [value]="i.status" /></td>
                        <td class="text-right font-semibold whitespace-nowrap" style="font-variant-numeric: tabular-nums">{{ i.total | number: '1.2-2' }} <span class="text-muted-color font-normal text-sm">{{ i.currency }}</span></td>
                        <td><app-when [value]="i.createdOnUtc" /></td>
                        <td>
                            <p-button label="Issue" icon="pi pi-send" [text]="true" size="small" [disabled]="i.status !== 'Draft'" (onClick)="issue(i)" />
                            <p-button label="Cancel" icon="pi pi-times" [text]="true" size="small" severity="danger" [disabled]="i.status === 'Paid' || i.status === 'Cancelled'" (onClick)="cancel(i)" />
                        </td>
                    </tr>
                </ng-template>
                <ng-template #emptymessage><tr><td colspan="5" class="text-center p-4 text-muted-color">No invoices yet.</td></tr></ng-template>
            </p-table>
        </div>

        <p-dialog header="New invoice" [(visible)]="dialog" [modal]="true" [style]="{ width: '480px' }">
            <div class="flex flex-col gap-3">
                <label>Customer Id</label>
                <input pInputText [(ngModel)]="form.customerId" />
                <label>Currency</label>
                <input pInputText [(ngModel)]="form.currency" maxlength="3" />
                <span class="font-semibold mt-2">Line</span>
                <input pInputText placeholder="Description" [(ngModel)]="form.lineDescription" />
                <div class="grid grid-cols-2 gap-3">
                    <div><label>Quantity</label><p-inputnumber [(ngModel)]="form.lineQty" [min]="1" /></div>
                    <div><label>Unit price</label><p-inputnumber [(ngModel)]="form.lineUnitPrice" [min]="0" mode="decimal" [minFractionDigits]="2" /></div>
                </div>
            </div>
            <ng-template #footer>
                <p-button label="Cancel" [text]="true" (onClick)="dialog = false" />
                <p-button label="Create" icon="pi pi-check" (onClick)="create()" [loading]="saving" />
            </ng-template>
        </p-dialog>
    `
})
export class Billing implements OnInit {
    private api = inject(BillingService);
    private toast = inject(MessageService);

    invoices: (InvoiceSummary & { customerName: string })[] = [];
    loading = false;
    dialog = false;
    saving = false;

    form = {
        customerId: DEFAULT_CUSTOMER,
        currency: 'EUR',
        lineDescription: 'Freight',
        lineQty: 1,
        lineUnitPrice: 850
    };

    ngOnInit() {
        this.load();
    }

    load() {
        this.loading = true;
        this.api.list({ page: 1, pageSize: 100, sort: 'created_desc' }).subscribe({
            next: (p) => { this.invoices = p.items.map((row) => ({ ...row, customerName: partyName(row.customerId) })); this.loading = false; },
            error: () => { this.loading = false; this.err('Could not load invoices'); }
        });
    }

    create() {
        this.saving = true;
        this.api
            .create({
                customerId: this.form.customerId,
                currency: this.form.currency,
                lines: [{ description: this.form.lineDescription, quantity: this.form.lineQty, unitPrice: this.form.lineUnitPrice }]
            })
            .subscribe({
                next: () => { this.saving = false; this.dialog = false; this.ok('Invoice created'); this.load(); },
                error: () => { this.saving = false; this.err('Create failed'); }
            });
    }

    issue(i: InvoiceSummary) {
        const due = new Date();
        due.setDate(due.getDate() + 30);
        this.api.issue(i.id, due.toISOString()).subscribe({ next: () => { this.ok('Issued'); this.load(); }, error: (e) => this.err(this.reason(e)) });
    }

    cancel(i: InvoiceSummary) {
        this.api.cancel(i.id).subscribe({ next: () => { this.ok('Cancelled'); this.load(); }, error: (e) => this.err(this.reason(e)) });
    }

    severity(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
        switch (status) {
            case 'Issued': return 'info';
            case 'Paid': return 'success';
            case 'Cancelled': return 'danger';
            default: return 'secondary';
        }
    }

    private reason(e: any): string { return e?.error?.detail || e?.error?.title || 'Operation not allowed'; }
    private ok(detail: string) { this.toast.add({ severity: 'success', summary: 'OK', detail, life: 2500 }); }
    private err(detail: string) { this.toast.add({ severity: 'error', summary: 'Error', detail, life: 3500 }); }
}
