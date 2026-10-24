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
import { CarrierDetail, CarrierSummary } from '../../core/models';
import { CarriersService } from '../../core/carriers.service';

type Sev = 'success' | 'info' | 'warn' | 'danger' | 'secondary';

@Component({
    selector: 'app-carriers',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, DialogModule, InputTextModule, InputNumberModule, SelectModule, TagModule, ToastModule, ToolbarModule],
    providers: [MessageService],
    template: `
        <p-toast />
        <div class="card">
            <p-toolbar styleClass="mb-4">
                <ng-template #start><span class="text-xl font-semibold">Carriers</span></ng-template>
                <ng-template #end>
                    <p-button label="Refresh" icon="pi pi-refresh" [text]="true" (onClick)="load()" />
                    <p-button label="New Carrier" icon="pi pi-plus" (onClick)="newDialog = true" />
                </ng-template>
            </p-toolbar>

            <p-table [value]="carriers" [loading]="loading" [paginator]="true" [rows]="10" dataKey="id" [rowHover]="true">
                <ng-template #header>
                    <tr><th>Legal name</th><th>Country</th><th>Capacity</th><th>Fleet</th><th>Drivers</th><th>Status</th><th style="width: 8rem"></th></tr>
                </ng-template>
                <ng-template #body let-c>
                    <tr>
                        <td class="font-medium">{{ c.legalName }}</td>
                        <td>{{ c.country }}</td>
                        <td>{{ c.totalCapacityKg | number }} kg</td>
                        <td>{{ c.fleetSize }}</td>
                        <td>{{ c.driverCount }}</td>
                        <td><p-tag [value]="c.status" [severity]="severity(c.status)" /></td>
                        <td><p-button label="Manage" icon="pi pi-sliders-h" [text]="true" size="small" (onClick)="manage(c)" /></td>
                    </tr>
                </ng-template>
                <ng-template #emptymessage><tr><td colspan="7" class="text-center p-4 text-muted-color">No carriers yet.</td></tr></ng-template>
            </p-table>
        </div>

        <p-dialog header="New carrier" [(visible)]="newDialog" [modal]="true" [style]="{ width: '440px' }">
            <div class="flex flex-col gap-3">
                <div><label>Legal name</label><input pInputText class="w-full" [(ngModel)]="form.legalName" /></div>
                <div><label>Registration number</label><input pInputText class="w-full" [(ngModel)]="form.registrationNumber" /></div>
                <div><label>Country</label><input pInputText class="w-full" [(ngModel)]="form.country" /></div>
            </div>
            <ng-template #footer>
                <p-button label="Cancel" [text]="true" (onClick)="newDialog = false" />
                <p-button label="Create" icon="pi pi-check" (onClick)="create()" [loading]="saving" />
            </ng-template>
        </p-dialog>

        <p-dialog [header]="detail ? detail.legalName : 'Carrier'" [(visible)]="manageDialog" [modal]="true" [style]="{ width: '680px' }">
            <ng-container *ngIf="detail as d">
                <div class="flex items-center gap-2 mb-3">
                    <p-tag [value]="d.status" [severity]="severity(d.status)" />
                    <span class="text-muted-color">{{ d.country }} · reg {{ d.registrationNumber }} · {{ d.totalCapacityKg | number }} kg total</span>
                </div>

                <span class="font-semibold">Fleet ({{ d.vehicles.length }})</span>
                <p-table [value]="d.vehicles" styleClass="mb-2 mt-1">
                    <ng-template #header><tr><th>Plate</th><th>Type</th><th>Capacity</th><th>Status</th></tr></ng-template>
                    <ng-template #body let-v><tr><td>{{ v.plate }}</td><td>{{ v.type }}</td><td>{{ v.capacityKg | number }} kg</td><td>{{ v.status }}</td></tr></ng-template>
                    <ng-template #emptymessage><tr><td colspan="4" class="text-muted-color p-2">No vehicles.</td></tr></ng-template>
                </p-table>
                <div class="flex flex-wrap gap-2 mb-4" *ngIf="d.status !== 'Suspended'">
                    <input pInputText placeholder="Plate" [(ngModel)]="vForm.plate" style="width:9rem" />
                    <p-select [options]="vehicleTypes" [(ngModel)]="vForm.type" styleClass="w-40" />
                    <p-inputnumber [(ngModel)]="vForm.capacityKg" [min]="1" placeholder="kg" />
                    <p-button label="Add vehicle" icon="pi pi-plus" size="small" (onClick)="addVehicle(d)" />
                </div>

                <span class="font-semibold">Drivers ({{ d.drivers.length }})</span>
                <p-table [value]="d.drivers" styleClass="mb-2 mt-1">
                    <ng-template #header><tr><th>Name</th><th>Licence</th><th>Status</th></tr></ng-template>
                    <ng-template #body let-dr><tr><td>{{ dr.fullName }}</td><td>{{ dr.licenceNumber }}</td><td>{{ dr.status }}</td></tr></ng-template>
                    <ng-template #emptymessage><tr><td colspan="3" class="text-muted-color p-2">No drivers.</td></tr></ng-template>
                </p-table>
                <div class="flex flex-wrap gap-2 mb-4" *ngIf="d.status !== 'Suspended'">
                    <input pInputText placeholder="Full name" [(ngModel)]="dForm.fullName" style="width:12rem" />
                    <input pInputText placeholder="Licence" [(ngModel)]="dForm.licenceNumber" style="width:9rem" />
                    <p-button label="Add driver" icon="pi pi-plus" size="small" (onClick)="addDriver(d)" />
                </div>

                <hr class="my-3 border-surface" />
                <div class="flex flex-wrap gap-2">
                    <p-button label="Verify" icon="pi pi-verified" size="small" severity="success"
                              [disabled]="d.status !== 'Onboarding'" (onClick)="verify(d)"
                              pTooltip="Needs at least 1 vehicle and 1 driver" />
                    <p-button label="Suspend" icon="pi pi-ban" size="small" severity="danger" [text]="true"
                              [disabled]="d.status !== 'Verified'" (onClick)="suspend(d)" />
                    <p-button label="Reactivate" icon="pi pi-replay" size="small" [outlined]="true"
                              [disabled]="d.status !== 'Suspended'" (onClick)="reactivate(d)" />
                </div>
                <p class="text-muted-color text-sm mt-3" *ngIf="d.status === 'Verified'">
                    <i class="pi pi-info-circle"></i> Verified — now assignable to loads in Planning.
                </p>
            </ng-container>
        </p-dialog>
    `
})
export class Carriers implements OnInit {
    private api = inject(CarriersService);
    private toast = inject(MessageService);

    carriers: CarrierSummary[] = [];
    loading = false;
    saving = false;
    newDialog = false;
    manageDialog = false;
    detail: CarrierDetail | null = null;

    vehicleTypes = ['Van', 'Truck', 'Trailer', 'Refrigerated'];
    form = { legalName: '', registrationNumber: '', country: 'ES' };
    vForm = { plate: '', type: 'Truck', capacityKg: 24000 };
    dForm = { fullName: '', licenceNumber: '' };

    ngOnInit() { this.load(); }

    load() {
        this.loading = true;
        this.api.list({ pageSize: 100 }).subscribe({
            next: (p) => { this.carriers = p.items; this.loading = false; },
            error: () => { this.loading = false; this.err('Could not load carriers'); }
        });
    }

    create() {
        this.saving = true;
        this.api.create(this.form).subscribe({
            next: () => { this.saving = false; this.newDialog = false; this.ok('Carrier created'); this.load(); },
            error: (e) => { this.saving = false; this.problem(e); }
        });
    }

    manage(c: CarrierSummary) {
        this.api.get(c.id).subscribe({ next: (d) => { this.detail = d; this.manageDialog = true; }, error: () => this.err('Could not load carrier') });
    }

    private refresh(id: string) { this.api.get(id).subscribe({ next: (d) => (this.detail = d) }); this.load(); }

    addVehicle(d: CarrierDetail) {
        this.api.addVehicle(d.id, { ...this.vForm }).subscribe({ next: () => { this.ok('Vehicle added'); this.refresh(d.id); }, error: (e) => this.problem(e) });
    }
    addDriver(d: CarrierDetail) {
        this.api.addDriver(d.id, { ...this.dForm }).subscribe({ next: () => { this.ok('Driver added'); this.dForm = { fullName: '', licenceNumber: '' }; this.refresh(d.id); }, error: (e) => this.problem(e) });
    }
    verify(d: CarrierDetail) {
        this.api.verify(d.id).subscribe({ next: () => { this.ok('Carrier verified — assignable in Planning'); this.refresh(d.id); }, error: (e) => this.problem(e) });
    }
    suspend(d: CarrierDetail) {
        this.api.suspend(d.id, 'Manual suspension').subscribe({ next: () => { this.ok('Carrier suspended'); this.refresh(d.id); }, error: (e) => this.problem(e) });
    }
    reactivate(d: CarrierDetail) {
        this.api.reactivate(d.id).subscribe({ next: () => { this.ok('Carrier reactivated'); this.refresh(d.id); }, error: (e) => this.problem(e) });
    }

    severity(status: string): Sev {
        switch (status) {
            case 'Verified': return 'success';
            case 'Onboarding': return 'warn';
            case 'Suspended': return 'danger';
            default: return 'secondary';
        }
    }

    private ok(detail: string) { this.toast.add({ severity: 'success', summary: 'OK', detail }); }
    private err(detail: string) { this.toast.add({ severity: 'error', summary: 'Error', detail }); }
    private problem(e: any) { this.toast.add({ severity: 'warn', summary: 'Rejected', detail: e?.error?.title || e?.error?.detail || e?.message || 'Request failed' }); }
}
