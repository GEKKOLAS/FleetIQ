import { DatePipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { TagModule } from 'primeng/tag';

type Severity = 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast';

/** One meaning per status across every table: colour + icon, so a state reads the same on any page. */
const STATUS: Record<string, [Severity, string]> = {
    // done / healthy
    Verified: ['success', 'pi-verified'], Paid: ['success', 'pi-check-circle'], Passed: ['success', 'pi-check-circle'],
    Completed: ['success', 'pi-check-circle'], Active: ['success', 'pi-check-circle'], Delivered: ['success', 'pi-check-circle'],
    Sent: ['success', 'pi-send'], ReadyForPlanning: ['success', 'pi-flag'], Available: ['success', 'pi-check-circle'],
    Finalized: ['success', 'pi-check-circle'],
    // in progress
    Confirmed: ['info', 'pi-check'], Planned: ['info', 'pi-calendar'], Issued: ['info', 'pi-send'], Running: ['info', 'pi-sync'],
    InTransit: ['info', 'pi-truck'], Dispatched: ['info', 'pi-send'], Waived: ['info', 'pi-forward'], InUse: ['info', 'pi-truck'],
    OnDuty: ['info', 'pi-user'],
    // waiting on someone
    Pending: ['warn', 'pi-clock'], Requested: ['warn', 'pi-clock'], Registered: ['warn', 'pi-clock'], Uploaded: ['warn', 'pi-upload'],
    Onboarding: ['warn', 'pi-clock'], Overdue: ['warn', 'pi-exclamation-circle'], Maintenance: ['warn', 'pi-wrench'],
    // stopped
    Cancelled: ['danger', 'pi-times-circle'], Failed: ['danger', 'pi-times-circle'], Rejected: ['danger', 'pi-times-circle'],
    Suspended: ['danger', 'pi-ban'], Disabled: ['danger', 'pi-ban'],
    // not started / retired
    Draft: ['secondary', 'pi-pencil'], Created: ['secondary', 'pi-circle'], Archived: ['secondary', 'pi-inbox'],
    Discontinued: ['secondary', 'pi-ban'], Off: ['secondary', 'pi-circle']
};

/** "ReadyForPlanning" -> "Ready for planning". */
export function humanize(value: string): string {
    const words = value.replace(/([a-z])([A-Z])/g, '$1 $2');
    return words.charAt(0) + words.slice(1).toLowerCase();
}

@Component({
    selector: 'app-status',
    standalone: true,
    imports: [TagModule],
    template: `<p-tag [value]="label" [severity]="look[0]" [icon]="'pi ' + look[1]" styleClass="whitespace-nowrap" />`
})
export class StatusTag {
    @Input({ required: true }) value = '';
    get look(): [Severity, string] { return STATUS[this.value] ?? ['secondary', 'pi-circle']; }
    get label() { return humanize(this.value); }
}

/**
 * Parties the platform knows by name. There is no customer directory service yet, so only the
 * seeded accounts resolve; any other id is shown as a short reference instead of a made-up name.
 */
const PARTIES: Record<string, string> = {
    '22222222-2222-2222-2222-222222222222': 'Acme',
    '33333333-3333-3333-3333-333333333333': 'Nordic'
};

export function partyName(id: string): string {
    return PARTIES[id?.toLowerCase()] ?? `Ref ${id?.slice(0, 8).toUpperCase()}`;
}

@Component({
    selector: 'app-party',
    standalone: true,
    template: `<span class="party" [title]="id">
        <span class="party-avatar" [class.unknown]="!known" aria-hidden="true">@if (known) { {{ name.charAt(0) }} } @else { <i class="pi pi-building"></i> }</span>
        <span class="party-text"><strong>{{ name }}</strong>@if (known) { <small>{{ id.slice(0, 8) }}</small> }</span>
    </span>`,
    styles: [`.party{display:inline-flex;align-items:center;gap:.65rem;min-width:0}
        .party-avatar{width:2rem;height:2rem;flex-shrink:0;border-radius:.6rem;display:grid;place-items:center;font-weight:700;font-size:.85rem;color:var(--primary-color);background:color-mix(in srgb,var(--primary-color) 14%,transparent)}
        .party-avatar.unknown{color:var(--text-color-secondary);background:color-mix(in srgb,var(--text-color-secondary) 14%,transparent);font-size:.8rem}
        .party-text{display:flex;flex-direction:column;line-height:1.25;min-width:0}.party-text strong{font-weight:600}
        .party-text small{color:var(--text-color-secondary);font-size:.72rem;font-variant-numeric:tabular-nums}`]
})
export class PartyCell {
    @Input({ required: true }) id = '';
    get known() { return !!PARTIES[this.id?.toLowerCase()]; }
    get name() { return partyName(this.id); }
}

/** Date on the first line, time underneath, so the column scans by day. */
@Component({
    selector: 'app-when',
    standalone: true,
    imports: [DatePipe],
    template: `@if (value) { <span class="when" [title]="value | date: 'full'"><span>{{ value | date: 'mediumDate' }}</span><small>{{ value | date: 'shortTime' }}</small></span> } @else { <span class="when-empty">—</span> }`,
    styles: [`.when{display:inline-flex;flex-direction:column;line-height:1.25;white-space:nowrap;font-variant-numeric:tabular-nums}
        .when small,.when-empty{color:var(--text-color-secondary);font-size:.72rem}`]
})
export class WhenCell {
    @Input() value?: string | null;
}

export const TABLE_CELLS = [StatusTag, PartyCell, WhenCell] as const;
