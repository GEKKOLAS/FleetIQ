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
import { DocumentSummary } from '../../core/models';
import { DocumentsService } from '../../core/documents.service';

const DEFAULT_OWNER = '22222222-2222-2222-2222-222222222222'; // seeded customer

@Component({
    selector: 'app-documents',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, DialogModule, InputTextModule, SelectModule, TagModule, ToastModule, ToolbarModule],
    providers: [MessageService],
    template: `
        <p-toast />
        <div class="card">
            <p-toolbar styleClass="mb-4">
                <ng-template #start><span class="text-xl font-semibold">Documents</span></ng-template>
                <ng-template #end>
                    <p-button label="Refresh" icon="pi pi-refresh" [text]="true" (onClick)="load()" />
                    <p-button label="New Document" icon="pi pi-plus" (onClick)="newDialog = true" />
                </ng-template>
            </p-toolbar>

            <p-table [value]="documents" [loading]="loading" [paginator]="true" [rows]="10" dataKey="id" [rowHover]="true">
                <ng-template #header>
                    <tr><th>Category</th><th>Storage ref</th><th>Owner</th><th>Status</th><th>Created</th><th style="width: 8rem"></th></tr>
                </ng-template>
                <ng-template #body let-d>
                    <tr>
                        <td class="font-medium">{{ d.category }}</td>
                        <td class="text-muted-color">{{ d.storageRef }}</td>
                        <td class="text-muted-color">{{ d.ownerId.slice(0, 8) }}…</td>
                        <td><p-tag [value]="d.status" [severity]="d.status === 'Verified' ? 'success' : 'warn'" /></td>
                        <td>{{ d.createdOnUtc | date: 'short' }}</td>
                        <td>
                            <p-button label="Verify" icon="pi pi-verified" [text]="true" size="small" severity="success"
                                      [disabled]="d.status !== 'Uploaded'" (onClick)="verify(d)" />
                        </td>
                    </tr>
                </ng-template>
                <ng-template #emptymessage><tr><td colspan="6" class="text-center p-4 text-muted-color">No documents yet.</td></tr></ng-template>
            </p-table>
        </div>

        <p-dialog header="New document" [(visible)]="newDialog" [modal]="true" [style]="{ width: '440px' }">
            <div class="flex flex-col gap-3">
                <div><label>Category</label><p-select [options]="categories" [(ngModel)]="form.category" styleClass="w-full" /></div>
                <div><label>Storage ref</label><input pInputText class="w-full" [(ngModel)]="form.storageRef" placeholder="s3://fleetiq-docs/…" /></div>
                <div><label>Owner id</label><input pInputText class="w-full" [(ngModel)]="form.ownerId" /></div>
            </div>
            <ng-template #footer>
                <p-button label="Cancel" [text]="true" (onClick)="newDialog = false" />
                <p-button label="Create" icon="pi pi-check" (onClick)="create()" [loading]="saving" />
            </ng-template>
        </p-dialog>
    `
})
export class Documents implements OnInit {
    private api = inject(DocumentsService);
    private toast = inject(MessageService);

    documents: DocumentSummary[] = [];
    loading = false;
    saving = false;
    newDialog = false;

    categories = ['CMR', 'Proof of delivery', 'Invoice', 'Insurance', 'Customs', 'Other'];
    form = { ownerId: DEFAULT_OWNER, category: 'CMR', storageRef: '' };

    ngOnInit() { this.load(); }

    load() {
        this.loading = true;
        this.api.list(100).subscribe({
            next: (items) => { this.documents = items; this.loading = false; },
            error: () => { this.loading = false; this.err('Could not load documents'); }
        });
    }

    create() {
        this.saving = true;
        this.api.create({ ...this.form }).subscribe({
            next: () => { this.saving = false; this.newDialog = false; this.form.storageRef = ''; this.ok('Document created'); this.load(); },
            error: (e) => { this.saving = false; this.problem(e); }
        });
    }

    verify(d: DocumentSummary) {
        this.api.verify(d.id).subscribe({ next: () => { this.ok('Document verified'); this.load(); }, error: (e) => this.problem(e) });
    }

    private ok(detail: string) { this.toast.add({ severity: 'success', summary: 'OK', detail }); }
    private err(detail: string) { this.toast.add({ severity: 'error', summary: 'Error', detail }); }
    private problem(e: any) { this.toast.add({ severity: 'warn', summary: 'Rejected', detail: e?.error?.title || e?.error?.detail || e?.message || 'Request failed' }); }
}
