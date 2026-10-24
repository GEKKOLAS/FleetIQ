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
import { NotificationSummary } from '../../core/models';
import { NotificationsService } from '../../core/notifications.service';

@Component({
    selector: 'app-notifications',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, DialogModule, InputTextModule, SelectModule, TagModule, ToastModule, ToolbarModule],
    providers: [MessageService],
    template: `
        <p-toast />
        <div class="card">
            <p-toolbar styleClass="mb-4">
                <ng-template #start>
                    <div>
                        <span class="text-xl font-semibold">Notifications</span>
                        <p class="text-muted-color text-sm m-0 mt-1">Issued invoices create one automatically via the event bus.</p>
                    </div>
                </ng-template>
                <ng-template #end>
                    <p-button label="Refresh" icon="pi pi-refresh" [text]="true" (onClick)="load()" />
                    <p-button label="New Notification" icon="pi pi-plus" (onClick)="newDialog = true" />
                </ng-template>
            </p-toolbar>

            <p-table [value]="notifications" [loading]="loading" [paginator]="true" [rows]="10" dataKey="id" [rowHover]="true">
                <ng-template #header>
                    <tr><th>Recipient</th><th>Channel</th><th>Template</th><th>Status</th><th>Created</th><th style="width: 9rem"></th></tr>
                </ng-template>
                <ng-template #body let-n>
                    <tr>
                        <td class="font-medium">{{ n.recipient }}</td>
                        <td><i [class]="channelIcon(n.channel)" class="mr-2 text-muted-color"></i>{{ n.channel }}</td>
                        <td class="text-muted-color">{{ n.templateId }}</td>
                        <td><p-tag [value]="n.status" [severity]="n.status === 'Sent' ? 'success' : 'warn'" /></td>
                        <td>{{ n.createdOnUtc | date: 'short' }}</td>
                        <td>
                            <p-button label="Mark sent" icon="pi pi-send" [text]="true" size="small"
                                      [disabled]="n.status !== 'Pending'" (onClick)="markSent(n)" />
                        </td>
                    </tr>
                </ng-template>
                <ng-template #emptymessage><tr><td colspan="6" class="text-center p-4 text-muted-color">No notifications yet — issue an invoice in Billing and refresh.</td></tr></ng-template>
            </p-table>
        </div>

        <p-dialog header="New notification" [(visible)]="newDialog" [modal]="true" [style]="{ width: '440px' }">
            <div class="flex flex-col gap-3">
                <div><label>Recipient</label><input pInputText class="w-full" [(ngModel)]="form.recipient" placeholder="ops@customer.io" /></div>
                <div><label>Channel</label><p-select [options]="channels" [(ngModel)]="form.channel" styleClass="w-full" /></div>
                <div><label>Template</label><input pInputText class="w-full" [(ngModel)]="form.templateId" placeholder="shipment-delayed" /></div>
            </div>
            <ng-template #footer>
                <p-button label="Cancel" [text]="true" (onClick)="newDialog = false" />
                <p-button label="Create" icon="pi pi-check" (onClick)="create()" [loading]="saving" />
            </ng-template>
        </p-dialog>
    `
})
export class Notifications implements OnInit {
    private api = inject(NotificationsService);
    private toast = inject(MessageService);

    notifications: NotificationSummary[] = [];
    loading = false;
    saving = false;
    newDialog = false;

    channels = ['Email', 'Sms', 'Push'];
    form = { recipient: '', channel: 'Email', templateId: '' };

    ngOnInit() { this.load(); }

    load() {
        this.loading = true;
        this.api.list(100).subscribe({
            next: (items) => { this.notifications = items; this.loading = false; },
            error: () => { this.loading = false; this.err('Could not load notifications'); }
        });
    }

    create() {
        this.saving = true;
        this.api.create({ ...this.form }).subscribe({
            next: () => { this.saving = false; this.newDialog = false; this.form = { recipient: '', channel: 'Email', templateId: '' }; this.ok('Notification created'); this.load(); },
            error: (e) => { this.saving = false; this.problem(e); }
        });
    }

    markSent(n: NotificationSummary) {
        this.api.markSent(n.id).subscribe({ next: () => { this.ok('Marked as sent'); this.load(); }, error: (e) => this.problem(e) });
    }

    channelIcon(channel: string): string {
        switch (channel) {
            case 'Sms': return 'pi pi-mobile';
            case 'Push': return 'pi pi-bell';
            default: return 'pi pi-envelope';
        }
    }

    private ok(detail: string) { this.toast.add({ severity: 'success', summary: 'OK', detail }); }
    private err(detail: string) { this.toast.add({ severity: 'error', summary: 'Error', detail }); }
    private problem(e: any) { this.toast.add({ severity: 'warn', summary: 'Rejected', detail: e?.error?.title || e?.error?.detail || e?.message || 'Request failed' }); }
}
