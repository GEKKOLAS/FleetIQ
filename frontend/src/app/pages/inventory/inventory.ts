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
import { ToastModule } from 'primeng/toast';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { ToolbarModule } from 'primeng/toolbar';
import { TooltipModule } from 'primeng/tooltip';
import { InventoryService } from '../../core/inventory.service';
import { InventoryItemDetail, InventoryItemSummary, StockOperation } from '../../core/models';

type MoveKind = StockOperation | 'adjustments';

interface MoveOption {
    label: string;
    value: MoveKind;
    hint: string;
}

const MOVES: MoveOption[] = [
    { label: 'Receive', value: 'receipts', hint: 'Goods in (e.g. a purchase order). Adds to stock on hand.' },
    { label: 'Reserve', value: 'reservations', hint: 'Allocate available stock to an order so it cannot be promised twice.' },
    { label: 'Release reservation', value: 'reservations/release', hint: 'Return reserved stock to the available pool (e.g. order cancelled).' },
    { label: 'Ship', value: 'shipments', hint: 'Reserved goods leave the warehouse. Consumes the reservation and the stock.' },
    { label: 'Cycle count', value: 'adjustments', hint: 'Set on-hand to the physically counted quantity. A reason is required.' }
];

@Component({
    selector: 'app-inventory',
    standalone: true,
    imports: [
        CommonModule, FormsModule, TableModule, ButtonModule, DialogModule, InputTextModule, InputNumberModule,
        SelectModule, TagModule, ToastModule, ToggleSwitchModule, ToolbarModule, TooltipModule
    ],
    providers: [MessageService],
    template: `
        <p-toast />
        <div class="grid grid-cols-12 gap-4 mb-4">
            <div class="col-span-12 md:col-span-4">
                <div class="card mb-0"><span class="text-muted-color">SKUs tracked</span><div class="text-3xl font-semibold mt-2">{{ activeCount }}</div></div>
            </div>
            <div class="col-span-12 md:col-span-4">
                <div class="card mb-0"><span class="text-muted-color">Below reorder point</span>
                    <div class="text-3xl font-semibold mt-2" [class.text-orange-500]="lowCount > 0">{{ lowCount }}</div></div>
            </div>
            <div class="col-span-12 md:col-span-4">
                <div class="card mb-0"><span class="text-muted-color">Warehouses</span><div class="text-3xl font-semibold mt-2">{{ warehouses.length }}</div></div>
            </div>
        </div>

        <div class="card">
            <p-toolbar styleClass="mb-4">
                <ng-template #start>
                    <span class="text-xl font-semibold mr-4">Inventory</span>
                    <p-select [options]="warehouseOptions" [(ngModel)]="warehouse" (onChange)="load()" placeholder="All warehouses" styleClass="mr-4" />
                    <label class="flex items-center gap-2 text-sm"><p-toggleswitch [(ngModel)]="lowOnly" (onChange)="load()" /> Low stock only</label>
                </ng-template>
                <ng-template #end>
                    <p-button label="Refresh" icon="pi pi-refresh" [text]="true" (onClick)="load()" />
                    <p-button label="New Item" icon="pi pi-plus" (onClick)="newDialog = true" />
                </ng-template>
            </p-toolbar>

            <p-table [value]="items" [loading]="loading" [paginator]="true" [rows]="10" dataKey="id" [rowHover]="true">
                <ng-template #header>
                    <tr>
                        <th pSortableColumn="sku">SKU <p-sortIcon field="sku" /></th><th>Item</th><th>Warehouse</th>
                        <th class="text-right" pSortableColumn="quantityOnHand">On hand <p-sortIcon field="quantityOnHand" /></th>
                        <th class="text-right">Reserved</th>
                        <th class="text-right" pSortableColumn="quantityAvailable">Available <p-sortIcon field="quantityAvailable" /></th>
                        <th class="text-right">Reorder at</th><th>Status</th><th style="width: 12rem"></th>
                    </tr>
                </ng-template>
                <ng-template #body let-i>
                    <tr>
                        <td class="font-medium whitespace-nowrap">{{ i.sku }}</td>
                        <td>{{ i.name }}</td>
                        <td class="text-muted-color">{{ i.warehouseCode }}</td>
                        <td class="text-right">{{ i.quantityOnHand | number: '1.0-3' }} <span class="text-muted-color text-sm">{{ i.unitOfMeasure }}</span></td>
                        <td class="text-right">{{ i.quantityReserved | number: '1.0-3' }}</td>
                        <td class="text-right font-semibold" [class.text-orange-500]="i.isBelowReorderPoint">{{ i.quantityAvailable | number: '1.0-3' }}</td>
                        <td class="text-right text-muted-color">{{ i.reorderPoint | number: '1.0-3' }}</td>
                        <td>
                            @if (i.status === 'Discontinued') { <p-tag value="Discontinued" severity="secondary" /> }
                            @else if (i.isBelowReorderPoint) { <p-tag value="Low stock" severity="warn" /> }
                            @else { <p-tag value="In stock" severity="success" /> }
                        </td>
                        <td class="text-right">
                            <p-button icon="pi pi-arrow-right-arrow-left" [text]="true" size="small" pTooltip="Move stock"
                                      [disabled]="i.status === 'Discontinued' && i.quantityOnHand === 0" (onClick)="openMove(i)" />
                            <p-button icon="pi pi-history" [text]="true" size="small" pTooltip="Movement history" (onClick)="openHistory(i)" />
                            <p-button icon="pi pi-ban" [text]="true" size="small" severity="danger" pTooltip="Discontinue"
                                      [disabled]="i.status === 'Discontinued' || i.quantityReserved > 0" (onClick)="discontinue(i)" />
                        </td>
                    </tr>
                </ng-template>
                <ng-template #emptymessage><tr><td colspan="9" class="text-center p-4 text-muted-color">No inventory items match.</td></tr></ng-template>
            </p-table>
        </div>

        <p-dialog header="New inventory item" [(visible)]="newDialog" [modal]="true" [style]="{ width: '460px' }">
            <div class="flex flex-col gap-3">
                <div><label>SKU</label><input pInputText class="w-full" [(ngModel)]="form.sku" placeholder="PAL-EUR-1200" /></div>
                <div><label>Name</label><input pInputText class="w-full" [(ngModel)]="form.name" placeholder="EUR pallet 1200x800" /></div>
                <div class="flex gap-3">
                    <div class="flex-1"><label>Warehouse</label><input pInputText class="w-full" [(ngModel)]="form.warehouseCode" placeholder="LIS-01" /></div>
                    <div class="flex-1"><label>Unit</label><input pInputText class="w-full" [(ngModel)]="form.unitOfMeasure" placeholder="unit" /></div>
                </div>
                <div><label>Reorder point</label><p-inputnumber [(ngModel)]="form.reorderPoint" [min]="0" [maxFractionDigits]="3" styleClass="w-full" /></div>
            </div>
            <ng-template #footer>
                <p-button label="Cancel" [text]="true" (onClick)="newDialog = false" />
                <p-button label="Create" icon="pi pi-check" (onClick)="create()" [loading]="saving" />
            </ng-template>
        </p-dialog>

        <p-dialog [header]="'Move stock · ' + (selected?.sku ?? '')" [(visible)]="moveDialog" [modal]="true" [style]="{ width: '460px' }">
            @if (selected) {
                <div class="flex flex-col gap-3">
                    <div class="text-sm text-muted-color">
                        On hand <b>{{ selected.quantityOnHand | number: '1.0-3' }}</b> · Reserved <b>{{ selected.quantityReserved | number: '1.0-3' }}</b>
                        · Available <b>{{ selected.quantityAvailable | number: '1.0-3' }}</b> {{ selected.unitOfMeasure }}
                    </div>
                    <div><label>Movement</label><p-select [options]="moves" optionLabel="label" optionValue="value" [(ngModel)]="move.kind" styleClass="w-full" /></div>
                    <small class="text-muted-color">{{ moveHint }}</small>
                    <div><label>{{ move.kind === 'adjustments' ? 'Counted quantity' : 'Quantity' }}</label>
                        <p-inputnumber [(ngModel)]="move.quantity" [min]="0" [maxFractionDigits]="3" styleClass="w-full" /></div>
                    <div><label>{{ move.kind === 'adjustments' ? 'Reason' : 'Reference (PO / order number)' }}</label>
                        <input pInputText class="w-full" [(ngModel)]="move.reference" [placeholder]="move.kind === 'adjustments' ? 'Cycle count' : 'ORD-2026-01042'" /></div>
                </div>
            }
            <ng-template #footer>
                <p-button label="Cancel" [text]="true" (onClick)="moveDialog = false" />
                <p-button label="Apply" icon="pi pi-check" (onClick)="applyMove()" [loading]="saving" />
            </ng-template>
        </p-dialog>

        <p-dialog [header]="'Movement history · ' + (history?.sku ?? '')" [(visible)]="historyDialog" [modal]="true" [style]="{ width: '760px' }">
            <p-table [value]="history?.movements ?? []" [rows]="8" [paginator]="(history?.movements?.length ?? 0) > 8" size="small">
                <ng-template #header>
                    <tr><th>When</th><th>Type</th><th class="text-right">Qty</th><th>Reference</th><th class="text-right">On hand</th><th class="text-right">Reserved</th></tr>
                </ng-template>
                <ng-template #body let-m>
                    <tr>
                        <td>{{ m.occurredOnUtc | date: 'short' }}</td>
                        <td><p-tag [value]="m.type" [severity]="severityFor(m.type)" /></td>
                        <td class="text-right">{{ m.quantity > 0 && m.type === 'Adjustment' ? '+' : '' }}{{ m.quantity | number: '1.0-3' }}</td>
                        <td class="text-muted-color">{{ m.reference }}</td>
                        <td class="text-right">{{ m.onHandAfter | number: '1.0-3' }}</td>
                        <td class="text-right">{{ m.reservedAfter | number: '1.0-3' }}</td>
                    </tr>
                </ng-template>
                <ng-template #emptymessage><tr><td colspan="6" class="text-center p-4 text-muted-color">No movements yet.</td></tr></ng-template>
            </p-table>
        </p-dialog>
    `
})
export class Inventory implements OnInit {
    private api = inject(InventoryService);
    private toast = inject(MessageService);

    items: InventoryItemSummary[] = [];
    warehouses: string[] = [];
    warehouse: string | null = null;
    lowOnly = false;
    loading = false;
    saving = false;

    newDialog = false;
    form = { sku: '', name: '', warehouseCode: '', unitOfMeasure: 'unit', reorderPoint: 0 };

    moves = MOVES;
    moveDialog = false;
    selected: InventoryItemSummary | null = null;
    move: { kind: MoveKind; quantity: number | null; reference: string } = { kind: 'receipts', quantity: null, reference: '' };

    historyDialog = false;
    history: InventoryItemDetail | null = null;

    get warehouseOptions() { return [{ label: 'All warehouses', value: null }, ...this.warehouses.map((w) => ({ label: w, value: w }))]; }
    get activeCount() { return this.items.filter((i) => i.status !== 'Discontinued').length; }
    get lowCount() { return this.items.filter((i) => i.isBelowReorderPoint).length; }
    get moveHint() { return MOVES.find((m) => m.value === this.move.kind)?.hint ?? ''; }

    ngOnInit() { this.load(); }

    load() {
        this.loading = true;
        this.api.list({ warehouse: this.warehouse ?? undefined, lowStock: this.lowOnly }).subscribe({
            next: (items) => {
                this.items = items;
                // Only learn warehouses from an unfiltered load, so the dropdown keeps every option.
                if (!this.warehouse && !this.lowOnly) this.warehouses = [...new Set(items.map((i) => i.warehouseCode))].sort();
                this.loading = false;
            },
            error: () => { this.loading = false; this.err('Could not load inventory'); }
        });
    }

    create() {
        this.saving = true;
        this.api.create({ ...this.form }).subscribe({
            next: () => {
                this.saving = false;
                this.newDialog = false;
                this.form = { sku: '', name: '', warehouseCode: this.form.warehouseCode, unitOfMeasure: 'unit', reorderPoint: 0 };
                this.ok('Item created');
                this.load();
            },
            error: (e) => { this.saving = false; this.problem(e); }
        });
    }

    openMove(i: InventoryItemSummary) {
        this.selected = i;
        this.move = { kind: i.status === 'Discontinued' ? 'adjustments' : 'receipts', quantity: null, reference: '' };
        this.moveDialog = true;
    }

    applyMove() {
        if (!this.selected || this.move.quantity === null) { this.err('Enter a quantity'); return; }
        const { kind, quantity, reference } = this.move;
        const call = kind === 'adjustments'
            ? this.api.adjust(this.selected.id, quantity, reference)
            : this.api.move(this.selected.id, kind, quantity, reference);
        this.saving = true;
        call.subscribe({
            next: () => { this.saving = false; this.moveDialog = false; this.ok('Stock updated'); this.load(); },
            error: (e) => { this.saving = false; this.problem(e); }
        });
    }

    openHistory(i: InventoryItemSummary) {
        this.api.get(i.id).subscribe({
            next: (d) => { this.history = d; this.historyDialog = true; },
            error: (e) => this.problem(e)
        });
    }

    discontinue(i: InventoryItemSummary) {
        this.api.discontinue(i.id).subscribe({ next: () => { this.ok(`${i.sku} discontinued`); this.load(); }, error: (e) => this.problem(e) });
    }

    severityFor(type: string): 'success' | 'info' | 'warn' | 'secondary' | 'contrast' {
        switch (type) {
            case 'Receipt': return 'success';
            case 'Reservation': return 'info';
            case 'Shipment': return 'contrast';
            case 'Adjustment': return 'warn';
            default: return 'secondary';
        }
    }

    private ok(detail: string) { this.toast.add({ severity: 'success', summary: 'OK', detail }); }
    private err(detail: string) { this.toast.add({ severity: 'error', summary: 'Error', detail }); }
    // The API returns { code, description } for domain rule violations and a ProblemDetails for validation errors.
    private problem(e: any) {
        const detail = e?.error?.description || e?.error?.title || e?.error?.detail || e?.message || 'Request failed';
        this.toast.add({ severity: 'warn', summary: 'Rejected', detail });
    }
}
