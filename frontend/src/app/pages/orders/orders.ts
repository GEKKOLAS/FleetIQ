import { CommonModule } from '@angular/common';
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
import { OrderSummary } from '../../core/models';
import { OrdersService } from '../../core/orders.service';

const DEFAULT_CUSTOMER = '22222222-2222-2222-2222-222222222222';

@Component({
    selector: 'app-orders',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, DialogModule, InputTextModule, InputNumberModule, TagModule, ToastModule, ToolbarModule, TooltipModule],
    providers: [MessageService],
    template: `
        <p-toast />
        <div class="card">
            <p-toolbar styleClass="mb-4">
                <ng-template #start><span class="text-xl font-semibold">Orders</span></ng-template>
                <ng-template #end>
                    <p-button label="Refresh" icon="pi pi-refresh" [text]="true" (onClick)="load()" />
                    <p-button label="New Order" icon="pi pi-plus" (onClick)="openNew()" />
                </ng-template>
            </p-toolbar>

            <p-table [value]="orders" [loading]="loading" [paginator]="true" [rows]="10" dataKey="id" [rowHover]="true">
                <ng-template #header>
                    <tr>
                        <th>Order #</th>
                        <th>Customer</th>
                        <th>Status</th>
                        <th>Created</th>
                        <th style="width: 14rem"></th>
                    </tr>
                </ng-template>
                <ng-template #body let-o>
                    <tr>
                        <td>{{ o.orderNumber }}</td>
                        <td class="text-muted-color">{{ o.customerId.substring(0, 8) }}…</td>
                        <td><p-tag [value]="o.status" [severity]="severity(o.status)" /></td>
                        <td>{{ o.createdOnUtc | date: 'short' }}</td>
                        <td>
                            <p-button icon="pi pi-check" [rounded]="true" [text]="true" severity="success" pTooltip="Confirm" (onClick)="confirm(o)" />
                            <p-button icon="pi pi-times" [rounded]="true" [text]="true" severity="danger" pTooltip="Cancel" (onClick)="cancel(o)" />
                        </td>
                    </tr>
                </ng-template>
                <ng-template #emptymessage><tr><td colspan="5" class="text-center p-4 text-muted-color">No orders yet.</td></tr></ng-template>
            </p-table>
        </div>

        <p-dialog header="New order" [(visible)]="dialog" [modal]="true" [style]="{ width: '480px' }">
            <div class="flex flex-col gap-3">
                <label>Customer Id</label>
                <input pInputText [(ngModel)]="form.customerId" />
                <div class="grid grid-cols-2 gap-3">
                    <div><label>Pickup city</label><input pInputText [(ngModel)]="form.pickupCity" /></div>
                    <div><label>Delivery city</label><input pInputText [(ngModel)]="form.deliveryCity" /></div>
                </div>
                <span class="font-semibold mt-2">Item</span>
                <input pInputText placeholder="Description" [(ngModel)]="form.itemDescription" />
                <div class="grid grid-cols-2 gap-3">
                    <div><label>Quantity</label><p-inputnumber [(ngModel)]="form.itemQuantity" [min]="1" /></div>
                    <div><label>Weight (kg)</label><p-inputnumber [(ngModel)]="form.itemWeight" [min]="0" /></div>
                </div>
            </div>
            <ng-template #footer>
                <p-button label="Cancel" [text]="true" (onClick)="dialog = false" />
                <p-button label="Create" icon="pi pi-check" (onClick)="create()" [loading]="saving" />
            </ng-template>
        </p-dialog>
    `
})
export class Orders implements OnInit {
    private api = inject(OrdersService);
    private toast = inject(MessageService);

    orders: OrderSummary[] = [];
    loading = false;
    dialog = false;
    saving = false;

    form = {
        customerId: DEFAULT_CUSTOMER,
        pickupCity: 'Lisbon',
        deliveryCity: 'Porto',
        itemDescription: 'Pallet',
        itemQuantity: 1,
        itemWeight: 100
    };

    ngOnInit() {
        this.load();
    }

    load() {
        this.loading = true;
        this.api.list({ page: 1, pageSize: 100 }).subscribe({
            next: (p) => {
                this.orders = p.items;
                this.loading = false;
            },
            error: () => {
                this.loading = false;
                this.err('Could not load orders');
            }
        });
    }

    openNew() {
        this.dialog = true;
    }

    create() {
        this.saving = true;
        this.api
            .create({
                customerId: this.form.customerId,
                pickup: { street: '1 Main St', city: this.form.pickupCity, postalCode: '0000-000', country: 'PT' },
                delivery: { street: '5 Park Ave', city: this.form.deliveryCity, postalCode: '0000-000', country: 'PT' },
                items: [{ description: this.form.itemDescription, quantity: this.form.itemQuantity, weightKg: this.form.itemWeight }]
            })
            .subscribe({
                next: () => {
                    this.saving = false;
                    this.dialog = false;
                    this.ok('Order created');
                    this.load();
                },
                error: () => {
                    this.saving = false;
                    this.err('Create failed');
                }
            });
    }

    confirm(o: OrderSummary) {
        this.api.confirm(o.id).subscribe({ next: () => { this.ok('Confirmed'); this.load(); }, error: (e) => this.err(this.reason(e)) });
    }

    cancel(o: OrderSummary) {
        this.api.cancel(o.id).subscribe({ next: () => { this.ok('Cancelled'); this.load(); }, error: (e) => this.err(this.reason(e)) });
    }

    severity(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
        switch (status) {
            case 'Confirmed': return 'info';
            case 'ReadyForPlanning': return 'success';
            case 'Cancelled': return 'danger';
            default: return 'secondary';
        }
    }

    private reason(e: any): string {
        return e?.error?.detail || e?.error?.title || 'Operation not allowed';
    }
    private ok(detail: string) { this.toast.add({ severity: 'success', summary: 'OK', detail, life: 2500 }); }
    private err(detail: string) { this.toast.add({ severity: 'error', summary: 'Error', detail, life: 3500 }); }
}
