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
import { ToolbarModule } from 'primeng/toolbar';
import { TooltipModule } from 'primeng/tooltip';
import { firstValueFrom } from 'rxjs';
import { CarrierSummary, LoadDetail, LoadSummary } from '../../core/models';
import { PlanningService } from '../../core/planning.service';

type Sev = 'success' | 'info' | 'warn' | 'danger' | 'secondary';

// Rough coordinates so route stops land on a sensible spot; falls back to Lisbon/Porto.
const CITY: Record<string, [number, number]> = {
    lisboa: [38.7223, -9.1393], lisbon: [38.7223, -9.1393], porto: [41.1496, -8.6109],
    madrid: [40.4168, -3.7038], sevilla: [37.3891, -5.9845], barcelona: [41.3874, 2.1686], valencia: [39.4699, -0.3763]
};
const coord = (c: string): [number, number] => CITY[c?.toLowerCase().trim()] ?? [38.7223, -9.1393];

@Component({
    selector: 'app-planning',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, DialogModule, InputTextModule, InputNumberModule, SelectModule, TagModule, ToastModule, ToolbarModule, TooltipModule],
    providers: [MessageService],
    template: `
        <p-toast />
        <div class="card">
            <p-toolbar styleClass="mb-4">
                <ng-template #start><span class="text-xl font-semibold">Planning</span></ng-template>
                <ng-template #end>
                    <p-button label="Refresh" icon="pi pi-refresh" [text]="true" (onClick)="load()" />
                    <p-button label="New Load" icon="pi pi-plus" (onClick)="newDialog = true" />
                </ng-template>
            </p-toolbar>

            <p-table [value]="loads" [loading]="loading" [paginator]="true" [rows]="10" dataKey="id" [rowHover]="true">
                <ng-template #header>
                    <tr>
                        <th>Reference</th><th>Route</th><th>Weight</th><th>Orders</th><th>Status</th><th style="width: 16rem"></th>
                    </tr>
                </ng-template>
                <ng-template #body let-l>
                    <tr>
                        <td class="font-medium">{{ l.reference }}</td>
                        <td>{{ l.originCity }} <i class="pi pi-arrow-right text-muted-color mx-1" style="font-size:.7rem"></i> {{ l.destinationCity }}</td>
                        <td>{{ l.totalWeightKg }} kg</td>
                        <td>{{ l.orderCount }}</td>
                        <td><p-tag [value]="l.status" [severity]="severity(l.status)" /></td>
                        <td>
                            <p-button label="Manage" icon="pi pi-sliders-h" [text]="true" size="small" (onClick)="manage(l)" />
                            <p-button *ngIf="l.status === 'Draft'" label="Auto" icon="pi pi-bolt" [text]="true" size="small" severity="success"
                                      pTooltip="Add order, carrier, route, plan & confirm" (onClick)="autoComplete(l)" [loading]="busyId === l.id" />
                        </td>
                    </tr>
                </ng-template>
                <ng-template #emptymessage><tr><td colspan="6" class="text-center p-4 text-muted-color">No loads yet.</td></tr></ng-template>
            </p-table>
        </div>

        <!-- New load -->
        <p-dialog header="New load" [(visible)]="newDialog" [modal]="true" [style]="{ width: '420px' }">
            <div class="flex flex-col gap-3">
                <div><label>Origin city</label><input pInputText class="w-full" [(ngModel)]="form.origin" /></div>
                <div><label>Destination city</label><input pInputText class="w-full" [(ngModel)]="form.destination" /></div>
            </div>
            <ng-template #footer>
                <p-button label="Cancel" [text]="true" (onClick)="newDialog = false" />
                <p-button label="Create" icon="pi pi-check" (onClick)="create()" [loading]="saving" />
            </ng-template>
        </p-dialog>

        <!-- Manage load -->
        <p-dialog [header]="detail ? 'Load ' + detail.reference : 'Load'" [(visible)]="manageDialog" [modal]="true" [style]="{ width: '640px' }">
            <ng-container *ngIf="detail as d">
                <div class="flex items-center gap-2 mb-3">
                    <p-tag [value]="d.status" [severity]="severity(d.status)" />
                    <span class="text-muted-color">{{ d.originCity }} → {{ d.destinationCity }} · {{ d.totalWeightKg }} kg</span>
                </div>

                <div class="grid grid-cols-2 gap-2 mb-3 text-sm">
                    <span>Carrier: <b>{{ d.carrierId ? (d.carrierId.substring(0,8) + '…') : '—' }}</b></span>
                    <span>Route: <b>{{ d.routeId ? (d.routeId.substring(0,8) + '…') : '—' }}</b></span>
                </div>

                <span class="font-semibold">Orders ({{ d.orders.length }})</span>
                <p-table [value]="d.orders" styleClass="mb-3 mt-1" [rowHover]="true">
                    <ng-template #header><tr><th>Description</th><th>Weight</th></tr></ng-template>
                    <ng-template #body let-o><tr><td>{{ o.description }}</td><td>{{ o.weightKg }} kg</td></tr></ng-template>
                    <ng-template #emptymessage><tr><td colspan="2" class="text-muted-color p-2">No orders.</td></tr></ng-template>
                </p-table>

                <div class="flex flex-wrap gap-2" *ngIf="d.status === 'Draft'">
                    <input pInputText placeholder="Order description" [(ngModel)]="orderForm.description" style="flex:1; min-width:12rem" />
                    <p-inputnumber [(ngModel)]="orderForm.weightKg" [min]="0" placeholder="kg" />
                    <p-button label="Add order" icon="pi pi-plus" size="small" (onClick)="addOrder(d)" />
                </div>

                <hr class="my-4 border-surface" />

                <div class="flex flex-wrap items-center gap-2" *ngIf="d.status === 'Draft'">
                    <p-select [options]="verifiedCarriers" optionLabel="legalName" optionValue="id"
                              [(ngModel)]="selectedCarrierId" placeholder="Verified carrier" styleClass="w-56"
                              [disabled]="verifiedCarriers.length === 0" />
                    <p-button label="Assign carrier" icon="pi pi-truck" size="small" [outlined]="true"
                              [disabled]="!selectedCarrierId" (onClick)="assignCarrier(d)" />
                    <small *ngIf="verifiedCarriers.length === 0" class="text-muted-color">No verified carriers — verify one in Carriers first.</small>
                </div>

                <div class="flex flex-wrap gap-2 mt-2">
                    <p-button label="Build route" icon="pi pi-map" size="small" [outlined]="true"
                              [disabled]="d.status !== 'Draft' || !!d.routeId" (onClick)="buildRoute(d)" />
                    <p-button label="Plan" icon="pi pi-calendar" size="small"
                              [disabled]="d.status !== 'Draft' || d.orders.length === 0 || !d.carrierId || !d.routeId" (onClick)="plan(d)" />
                    <p-button label="Confirm" icon="pi pi-check" size="small" severity="success"
                              [disabled]="d.status !== 'Planned'" (onClick)="confirm(d)" />
                    <p-button label="Cancel load" icon="pi pi-times" size="small" severity="danger" [text]="true"
                              [disabled]="d.status === 'Confirmed' || d.status === 'Cancelled'" (onClick)="cancelLoad(d)" />
                </div>

                <p class="text-muted-color text-sm mt-3" *ngIf="d.status === 'Confirmed'">
                    <i class="pi pi-info-circle"></i> Confirmed — Tracking has created &amp; dispatched the shipment. Watch it on Live Tracking.
                </p>
            </ng-container>
        </p-dialog>
    `
})
export class Planning implements OnInit {
    private api = inject(PlanningService);
    private toast = inject(MessageService);

    loads: LoadSummary[] = [];
    loading = false;
    saving = false;
    busyId: string | null = null;

    newDialog = false;
    manageDialog = false;
    detail: LoadDetail | null = null;

    verifiedCarriers: CarrierSummary[] = [];
    selectedCarrierId: string | null = null;

    form = { origin: 'Lisboa', destination: 'Porto' };
    orderForm = { description: 'Pallet', weightKg: 100 };

    ngOnInit() {
        this.load();
        this.loadCarriers();
    }

    load() {
        this.loading = true;
        this.api.list({ pageSize: 100 }).subscribe({
            next: (p) => { this.loads = p.items; this.loading = false; },
            error: () => { this.loading = false; this.err('Could not load planning data'); }
        });
    }

    loadCarriers() {
        this.api.listVerifiedCarriers().subscribe({
            next: (p) => (this.verifiedCarriers = p.items),
            error: () => (this.verifiedCarriers = [])
        });
    }

    create() {
        this.saving = true;
        this.api.create(this.form.origin, this.form.destination).subscribe({
            next: () => { this.saving = false; this.newDialog = false; this.ok('Load created'); this.load(); },
            error: () => { this.saving = false; this.err('Could not create load'); }
        });
    }

    manage(l: LoadSummary) {
        this.selectedCarrierId = null;
        this.loadCarriers();
        this.api.get(l.id).subscribe({
            next: (d) => { this.detail = d; this.manageDialog = true; },
            error: () => this.err('Could not load detail')
        });
    }

    private refreshDetail(id: string) {
        this.api.get(id).subscribe({ next: (d) => (this.detail = d) });
        this.load();
    }

    addOrder(d: LoadDetail) {
        this.api.addOrder(d.id, { orderId: crypto.randomUUID(), description: this.orderForm.description, weightKg: this.orderForm.weightKg })
            .subscribe({ next: () => { this.ok('Order added'); this.refreshDetail(d.id); }, error: (e) => this.problem(e) });
    }

    assignCarrier(d: LoadDetail) {
        if (!this.selectedCarrierId) return;
        this.api.assignCarrier(d.id, this.selectedCarrierId)
            .subscribe({ next: () => { this.ok('Carrier assigned'); this.refreshDetail(d.id); }, error: (e) => this.problem(e) });
    }

    async buildRoute(d: LoadDetail) {
        try {
            const [pLat, pLng] = coord(d.originCity);
            const [dLat, dLng] = coord(d.destinationCity);
            const r = await firstValueFrom(this.api.createRoute(d.id));
            await firstValueFrom(this.api.addStop(r.id, { kind: 'Pickup', city: d.originCity, address: 'Pickup point', latitude: pLat, longitude: pLng }));
            await firstValueFrom(this.api.addStop(r.id, { kind: 'Delivery', city: d.destinationCity, address: 'Delivery point', latitude: dLat, longitude: dLng }));
            await firstValueFrom(this.api.finalizeRoute(r.id));
            await firstValueFrom(this.api.attachRoute(d.id, r.id));
            this.ok('Route built & attached');
            this.refreshDetail(d.id);
        } catch (e) { this.problem(e); }
    }

    plan(d: LoadDetail) {
        this.api.plan(d.id).subscribe({ next: () => { this.ok('Load planned'); this.refreshDetail(d.id); }, error: (e) => this.problem(e) });
    }

    confirm(d: LoadDetail) {
        this.api.confirm(d.id).subscribe({ next: () => { this.ok('Load confirmed — shipment created in Tracking'); this.refreshDetail(d.id); }, error: (e) => this.problem(e) });
    }

    cancelLoad(d: LoadDetail) {
        this.api.cancel(d.id).subscribe({ next: () => { this.ok('Load cancelled'); this.refreshDetail(d.id); }, error: (e) => this.problem(e) });
    }

    // One-click: run the full pipeline for a Draft load.
    async autoComplete(l: LoadSummary) {
        if (this.verifiedCarriers.length === 0) {
            this.toast.add({ severity: 'warn', summary: 'No carriers', detail: 'Verify a carrier in Carriers first.' });
            return;
        }
        const carrierId = this.verifiedCarriers[0].id;
        this.busyId = l.id;
        try {
            const [pLat, pLng] = coord(l.originCity);
            const [dLat, dLng] = coord(l.destinationCity);
            await firstValueFrom(this.api.addOrder(l.id, { orderId: crypto.randomUUID(), description: 'Auto pallet', weightKg: 200 }));
            await firstValueFrom(this.api.assignCarrier(l.id, carrierId));
            const r = await firstValueFrom(this.api.createRoute(l.id));
            await firstValueFrom(this.api.addStop(r.id, { kind: 'Pickup', city: l.originCity, address: 'Pickup point', latitude: pLat, longitude: pLng }));
            await firstValueFrom(this.api.addStop(r.id, { kind: 'Delivery', city: l.destinationCity, address: 'Delivery point', latitude: dLat, longitude: dLng }));
            await firstValueFrom(this.api.finalizeRoute(r.id));
            await firstValueFrom(this.api.attachRoute(l.id, r.id));
            await firstValueFrom(this.api.plan(l.id));
            await firstValueFrom(this.api.confirm(l.id));
            this.ok(`${l.reference} confirmed — shipment dispatched to Live Tracking`);
            this.load();
        } catch (e) { this.problem(e); }
        finally { this.busyId = null; }
    }

    severity(status: string): Sev {
        switch (status) {
            case 'Confirmed': return 'success';
            case 'Planned': return 'warn';
            case 'Cancelled': return 'danger';
            default: return 'secondary';
        }
    }

    private ok(detail: string) { this.toast.add({ severity: 'success', summary: 'OK', detail }); }
    private err(detail: string) { this.toast.add({ severity: 'error', summary: 'Error', detail }); }
    private problem(e: any) {
        const detail = e?.error?.title || e?.error?.detail || e?.message || 'Request failed';
        this.toast.add({ severity: 'warn', summary: 'Rejected', detail });
    }
}
