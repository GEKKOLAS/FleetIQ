import { Component, OnInit, effect, inject } from '@angular/core';
import { forkJoin } from 'rxjs';
import { ChartModule } from 'primeng/chart';
import { RouterLink } from '@angular/router';
import { BillingService } from '../../core/billing.service';
import { OrdersService } from '../../core/orders.service';
import { PlanningService } from '../../core/planning.service';
import { TrackingService } from '../../core/tracking.service';
import { LayoutService } from '../../layout/service/layout.service';
import { InvoiceSummary, LoadSummary, OrderSummary, ShipmentSummary } from '../../core/models';

interface Kpi {
    label: string;
    value: number | string;
    icon: string;
    color: string;
    sub?: string;
}

// Categorical palette (validated: CVD ΔE ≥ 39, contrast ≥ 3:1 on both surfaces).
const SERIES = {
    light: { blue: '#2a78d6', aqua: '#199e70', yellow: '#bd7e00' },
    dark: { blue: '#3987e5', aqua: '#199e70', yellow: '#c98500' }
};
// Ordinal blue ramp for pipeline stages (validated with --ordinal).
const ORDINAL = {
    light: ['#86b6ef', '#2a78d6', '#104281'],
    dark: ['#9ec5f4', '#3987e5', '#184f95']
};

/** Draws each dataset's label after its last point, in text ink, nudging overlapping labels apart. */
const endLabelsPlugin = {
    id: 'endLabels',
    afterDatasetsDraw(chart: any) {
        const ink = chart.options.plugins?.endLabels?.color ?? '#898781';
        const targets: { label: string; x: number; y: number }[] = [];
        chart.data.datasets.forEach((ds: any, i: number) => {
            const meta = chart.getDatasetMeta(i);
            if (!meta?.data?.length || meta.hidden) return;
            const last = meta.data[meta.data.length - 1];
            targets.push({ label: ds.label, x: last.x, y: last.y });
        });
        // De-overlap: sort by y, enforce a minimum vertical gap, keep inside the plot area.
        targets.sort((a, b) => a.y - b.y);
        const minGap = 14;
        for (let i = 1; i < targets.length; i++) {
            if (targets[i].y - targets[i - 1].y < minGap) targets[i].y = targets[i - 1].y + minGap;
        }
        let maxY = chart.chartArea.bottom - 2;
        for (let i = targets.length - 1; i >= 0; i--) {
            if (targets[i].y > maxY) targets[i].y = maxY;
            maxY = targets[i].y - minGap;
        }
        const { ctx } = chart;
        ctx.save();
        ctx.fillStyle = ink;
        ctx.font = '12px system-ui, sans-serif';
        ctx.textBaseline = 'middle';
        for (const t of targets) ctx.fillText(t.label, t.x + 8, t.y);
        ctx.restore();
    }
};

/** Total shipments as a hero number in the donut center, in text ink. */
const donutCenterPlugin = {
    id: 'donutCenter',
    afterDraw(chart: any) {
        const opts = chart.options.plugins?.donutCenter;
        if (!opts?.text) return;
        const { ctx } = chart;
        const { left, right, top, bottom } = chart.chartArea;
        const x = (left + right) / 2;
        const y = (top + bottom) / 2;
        ctx.save();
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillStyle = opts.color ?? '#0b0b0b';
        ctx.font = '700 28px system-ui, sans-serif';
        ctx.fillText(opts.text, x, y - 8);
        ctx.fillStyle = opts.subColor ?? '#898781';
        ctx.font = '12px system-ui, sans-serif';
        ctx.fillText(opts.subText ?? '', x, y + 16);
        ctx.restore();
    }
};

@Component({
    selector: 'app-dashboard',
    standalone: true,
    imports: [ChartModule, RouterLink],
    styleUrl: './dashboard.scss',
    template: `
        <header class="dashboard-intro"><div><span>FLEETIQ / OPERATIONS OVERVIEW</span><h1>Tu operación, conectada.</h1><p>Planifica con claridad. Mueve tu flota con confianza.</p></div><a routerLink="/app/planning"><i aria-hidden="true" class="pi pi-sitemap"></i> Planificar operación <i aria-hidden="true" class="pi pi-arrow-up-right"></i></a></header>
        <section class="network-banner"><div class="banner-copy"><span class="banner-eyebrow"><i aria-hidden="true" class="pi pi-globe"></i> CONTROL TOWER</span><h2>Una nueva perspectiva<br/>de tu red logística.</h2><p>Explora tus envíos, centros de distribución y rutas<br class="desktop-break"/> en una vista 3D interactiva.</p><a routerLink="/app/tracking">Explorar live tracking <i aria-hidden="true" class="pi pi-arrow-right"></i></a></div><img src="media/logistics-network.svg" alt="Ilustración isométrica de una red de almacenes y transporte" width="500" height="280" /></section>
        <div class="grid grid-cols-12 gap-6">
            @for (k of kpis; track k.label) {
                <div class="col-span-12 xl:col-span-3 md:col-span-6">
                    <div class="card mb-0">
                        <div class="flex justify-between mb-3">
                            <div>
                                <span class="block text-muted-color font-medium mb-2">{{ k.label }}</span>
                                <div class="text-surface-900 dark:text-surface-0 font-medium text-2xl" style="font-variant-numeric: tabular-nums">{{ k.value }}</div>
                            </div>
                            <div class="flex items-center justify-center rounded-border" [style]="'width:2.5rem;height:2.5rem;background:' + k.color + '22'">
                                <i aria-hidden="true" [class]="'pi ' + k.icon" [style]="'color:' + k.color"></i>
                            </div>
                        </div>
                        @if (k.sub) {
                            <span class="text-muted-color text-sm">{{ k.sub }}</span>
                        }
                    </div>
                </div>
            }

            <div class="col-span-12 xl:col-span-8">
                <div class="card h-full">
                    <div class="font-semibold text-xl mb-1">Activity</div>
                    <div class="text-muted-color text-sm mb-4">Orders, loads and shipments created per day</div>
                    @if (hasActivity) {
                        <div role="img" aria-label="Line chart of orders, loads and shipments created per day">
                            <p-chart type="line" [data]="activityData" [options]="activityOptions" [plugins]="linePlugins" height="300px" />
                        </div>
                    } @else {
                        <div class="text-muted-color py-16 text-center">No activity yet — create your first order to see it here.</div>
                    }
                </div>
            </div>

            <div class="col-span-12 xl:col-span-4">
                <div class="card h-full">
                    <div class="font-semibold text-xl mb-1">Shipment pipeline</div>
                    <div class="text-muted-color text-sm mb-4">Created → In transit → Delivered</div>
                    @if (hasShipments) {
                        <div role="img" aria-label="Donut chart of shipments by pipeline stage">
                            <p-chart type="doughnut" [data]="pipelineData" [options]="pipelineOptions" [plugins]="donutPlugins" height="300px" />
                        </div>
                    } @else {
                        <div class="text-muted-color py-16 text-center">No shipments yet.</div>
                    }
                </div>
            </div>

            <div class="col-span-12 xl:col-span-6">
                <div class="card h-full">
                    <div class="font-semibold text-xl mb-1">Invoiced amount</div>
                    <div class="text-muted-color text-sm mb-4">By invoice status, per currency</div>
                    @if (hasInvoices) {
                        <div role="img" aria-label="Bar chart of invoiced amount by status, one series per currency">
                            <p-chart type="bar" [data]="invoiceData" [options]="invoiceOptions" height="280px" />
                        </div>
                    } @else {
                        <div class="text-muted-color py-16 text-center">No invoices yet.</div>
                    }
                </div>
            </div>

            <div class="col-span-12 xl:col-span-6">
                <div class="card h-full">
                    <div class="font-semibold text-xl mb-1">Loads by status</div>
                    <div class="text-muted-color text-sm mb-4">Where planning work sits right now</div>
                    @if (hasLoads) {
                        <div role="img" aria-label="Horizontal bar chart of loads per status">
                            <p-chart type="bar" [data]="loadsData" [options]="loadsOptions" height="280px" />
                        </div>
                    } @else {
                        <div class="text-muted-color py-16 text-center">No loads yet.</div>
                    }
                </div>
            </div>
        </div>
    `
})
export class Dashboard implements OnInit {
    private orders = inject(OrdersService);
    private billing = inject(BillingService);
    private planning = inject(PlanningService);
    private tracking = inject(TrackingService);
    private layout = inject(LayoutService);

    kpis: Kpi[] = [
        { label: 'Orders', value: '…', icon: 'pi-box', color: '#2a78d6' },
        { label: 'Active loads', value: '…', icon: 'pi-sitemap', color: '#199e70' },
        { label: 'In transit', value: '…', icon: 'pi-map-marker', color: '#bd7e00' },
        { label: 'Issued amount', value: '…', icon: 'pi-wallet', color: '#4a3aa7' }
    ];

    linePlugins = [endLabelsPlugin];
    donutPlugins = [donutCenterPlugin];

    activityData: any;
    activityOptions: any;
    pipelineData: any;
    pipelineOptions: any;
    invoiceData: any;
    invoiceOptions: any;
    loadsData: any;
    loadsOptions: any;

    hasActivity = false;
    hasShipments = false;
    hasInvoices = false;
    hasLoads = false;

    private raw?: { orders: OrderSummary[]; invoices: InvoiceSummary[]; loads: LoadSummary[]; shipments: ShipmentSummary[] };

    constructor() {
        // Rebuild chart colors when the theme toggles; tokens flip with the .app-dark class.
        effect(() => {
            this.layout.isDarkTheme();
            if (this.raw) setTimeout(() => this.buildCharts(), 0);
        });
    }

    ngOnInit() {
        forkJoin({
            orders: this.orders.list({ page: 1, pageSize: 200 }),
            invoices: this.billing.list({ page: 1, pageSize: 200 }),
            loads: this.planning.list({ page: 1, pageSize: 200 }),
            shipments: this.tracking.list({ page: 1, pageSize: 200 })
        }).subscribe({
            next: ({ orders, invoices, loads, shipments }) => {
                this.raw = { orders: orders.items, invoices: invoices.items, loads: loads.items, shipments: shipments.items };
                this.buildKpis(orders.totalCount, invoices.items, loads.items, shipments.items);
                this.buildCharts();
            },
            error: () => {
                this.kpis = this.kpis.map((k) => ({ ...k, value: 'n/a', sub: 'API not available' }));
            }
        });
    }

    private buildKpis(orderCount: number, invoices: InvoiceSummary[], loads: LoadSummary[], shipments: ShipmentSummary[]) {
        const activeLoads = loads.filter((l) => l.status !== 'Cancelled');
        const confirmed = loads.filter((l) => l.status === 'Confirmed').length;
        const inTransit = shipments.filter((s) => s.status === 'InTransit').length;
        const issuedByCurrency = new Map<string, number>();
        for (const i of invoices) {
            if (i.status === 'Issued' || i.status === 'Paid') {
                issuedByCurrency.set(i.currency, (issuedByCurrency.get(i.currency) ?? 0) + i.total);
            }
        }
        const parts = [...issuedByCurrency.entries()].map(([cur, sum]) => this.money(sum, cur));
        this.kpis = [
            { label: 'Orders', value: orderCount, icon: 'pi-box', color: '#2a78d6' },
            { label: 'Active loads', value: activeLoads.length, icon: 'pi-sitemap', color: '#199e70', sub: `${confirmed} confirmed` },
            { label: 'In transit', value: inTransit, icon: 'pi-map-marker', color: '#bd7e00', sub: `of ${shipments.length} shipments` },
            { label: 'Issued amount', value: parts[0] ?? this.money(0, 'EUR'), icon: 'pi-wallet', color: '#4a3aa7', sub: parts.slice(1).join(' · ') || undefined }
        ];
    }

    private buildCharts() {
        if (!this.raw) return;
        const dark = !!this.layout.isDarkTheme();
        const s = dark ? SERIES.dark : SERIES.light;
        const ordinal = dark ? ORDINAL.dark : ORDINAL.light;
        const style = getComputedStyle(document.documentElement);
        const ink = style.getPropertyValue('--p-text-color').trim() || (dark ? '#ffffff' : '#0b0b0b');
        const muted = style.getPropertyValue('--p-text-muted-color').trim() || '#898781';
        const grid = style.getPropertyValue('--p-content-border-color').trim() || (dark ? '#2c2c2a' : '#e1e0d9');
        const surface = getComputedStyle(document.querySelector('.card') ?? document.body).backgroundColor || (dark ? '#18181b' : '#ffffff');

        const { orders, invoices, loads, shipments } = this.raw;

        // --- Activity line: created per day, one series per entity ---
        const dates = [...orders, ...loads, ...shipments].map((x) => x.createdOnUtc);
        this.hasActivity = dates.length > 0;
        if (this.hasActivity) {
            const days = this.dayRange(dates);
            const labels = days.map((d) => d.toLocaleDateString('en', { month: 'short', day: 'numeric' }));
            const counts = (items: { createdOnUtc: string }[]) => days.map((d) => items.filter((i) => this.sameDay(new Date(i.createdOnUtc), d)).length);
            const line = (label: string, data: number[], color: string) => ({
                label,
                data,
                borderColor: color,
                backgroundColor: color,
                borderWidth: 2,
                pointRadius: 2.5,
                pointHoverRadius: 5,
                pointHitRadius: 12,
                tension: 0.3
            });
            this.activityData = {
                labels,
                datasets: [line('Orders', counts(orders), s.blue), line('Loads', counts(loads), s.aqua), line('Shipments', counts(shipments), s.yellow)]
            };
            this.activityOptions = {
                maintainAspectRatio: false,
                layout: { padding: { right: 88 } },
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: { position: 'top', align: 'end', labels: { color: ink, usePointStyle: true, boxWidth: 8, boxHeight: 8 } },
                    tooltip: { mode: 'index', intersect: false },
                    endLabels: { color: muted }
                },
                scales: {
                    x: { grid: { display: false }, ticks: { color: muted, maxRotation: 0, autoSkip: true, maxTicksLimit: 8 }, border: { color: grid } },
                    y: { beginAtZero: true, grid: { color: grid }, ticks: { color: muted, precision: 0 }, border: { display: false } }
                }
            };
        }

        // --- Shipment pipeline donut: ordinal single-hue ramp ---
        const stages: [string, string][] = [
            ['Created', 'Created'],
            ['In transit', 'InTransit'],
            ['Delivered', 'Delivered']
        ];
        const stageCounts = stages.map(([, key]) => shipments.filter((sh) => sh.status === key).length);
        this.hasShipments = shipments.length > 0;
        this.pipelineData = {
            labels: stages.map(([label]) => label),
            datasets: [{ data: stageCounts, backgroundColor: ordinal, borderColor: surface, borderWidth: 2, hoverOffset: 6 }]
        };
        this.pipelineOptions = {
            maintainAspectRatio: false,
            cutout: '68%',
            plugins: {
                legend: { position: 'bottom', labels: { color: ink, usePointStyle: true, boxWidth: 8, boxHeight: 8 } },
                donutCenter: { text: `${shipments.length}`, subText: 'shipments', color: ink, subColor: muted }
            }
        };

        // --- Invoiced amount by status, one series per currency ---
        const statuses = ['Draft', 'Issued', 'Paid'];
        const currencies = [...new Set(invoices.map((i) => i.currency))].sort();
        const currencyColors = [s.blue, s.aqua, s.yellow];
        this.hasInvoices = invoices.length > 0;
        this.invoiceData = {
            labels: statuses,
            datasets: currencies.map((cur, i) => ({
                label: cur,
                data: statuses.map((st) => invoices.filter((inv) => inv.status === st && inv.currency === cur).reduce((sum, inv) => sum + inv.total, 0)),
                backgroundColor: currencyColors[i % currencyColors.length],
                borderRadius: 4,
                maxBarThickness: 36
            }))
        };
        this.invoiceOptions = {
            maintainAspectRatio: false,
            plugins: {
                legend: { position: 'top', align: 'end', labels: { color: ink, usePointStyle: true, boxWidth: 8, boxHeight: 8 } },
                tooltip: { callbacks: { label: (ctx: any) => `${ctx.dataset.label}: ${this.money(ctx.parsed.y, ctx.dataset.label)}` } }
            },
            scales: {
                x: { grid: { display: false }, ticks: { color: muted }, border: { color: grid } },
                y: { beginAtZero: true, grid: { color: grid }, ticks: { color: muted, callback: (v: number) => this.compact(v) }, border: { display: false } }
            }
        };

        // --- Loads by status: single series, single hue ---
        const loadStatuses = ['Draft', 'Planned', 'Confirmed', 'Cancelled'];
        this.hasLoads = loads.length > 0;
        this.loadsData = {
            labels: loadStatuses,
            datasets: [
                {
                    label: 'Loads',
                    data: loadStatuses.map((st) => loads.filter((l) => l.status === st).length),
                    backgroundColor: s.blue,
                    borderRadius: 4,
                    maxBarThickness: 28
                }
            ]
        };
        this.loadsOptions = {
            indexAxis: 'y',
            maintainAspectRatio: false,
            plugins: { legend: { display: false } },
            scales: {
                x: { beginAtZero: true, grid: { color: grid }, ticks: { color: muted, precision: 0 }, border: { display: false } },
                y: { grid: { display: false }, ticks: { color: ink }, border: { color: grid } }
            }
        };
    }

    private money(value: number, currency: string): string {
        try {
            return new Intl.NumberFormat('en', { style: 'currency', currency }).format(value);
        } catch {
            return `${value.toFixed(2)} ${currency}`;
        }
    }

    private compact(value: number): string {
        return new Intl.NumberFormat('en', { notation: 'compact' }).format(value);
    }

    private sameDay(a: Date, b: Date): boolean {
        return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
    }

    /** Daily buckets from the earliest record to today (capped to the last 60 days). */
    private dayRange(isoDates: string[]): Date[] {
        const min = new Date(Math.min(...isoDates.map((d) => new Date(d).getTime())));
        const today = new Date();
        const start = new Date(today);
        start.setDate(start.getDate() - 59);
        const from = min > start ? min : start;
        const days: Date[] = [];
        for (const d = new Date(from.getFullYear(), from.getMonth(), from.getDate()); d <= today; d.setDate(d.getDate() + 1)) {
            days.push(new Date(d));
        }
        return days;
    }
}
