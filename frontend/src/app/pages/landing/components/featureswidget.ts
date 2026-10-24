import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RevealDirective } from './reveal.directive';

interface Feature {
    icon: string;
    title: string;
    description: string;
    badge?: string;
}

@Component({
    selector: 'features-widget',
    standalone: true,
    imports: [CommonModule, RevealDirective],
    template: `
        <div id="features" class="py-6 px-6 lg:px-20 mt-8 mx-0 lg:mx-20">
            <div class="grid grid-cols-12 gap-6 justify-center">
                <div class="col-span-12 text-center mt-20 mb-6" reveal>
                    <div class="text-surface-900 dark:text-surface-0 font-semibold mb-2 text-4xl">Everything your freight operation needs</div>
                    <span class="text-muted-color text-2xl">From order intake to settlement — one platform, twelve services, zero spreadsheets.</span>
                </div>

                @for (feature of features; track feature.title; let i = $index) {
                    <div class="col-span-12 md:col-span-6 lg:col-span-4" reveal [revealDelay]="(i % 3) * 0.12">
                        <div
                            class="h-full p-6 rounded-2xl border border-surface-200 dark:border-surface-700 bg-surface-0 dark:bg-surface-900 transition-all duration-300 hover:-translate-y-1.5 hover:shadow-xl hover:border-primary cursor-default"
                        >
                            <div class="flex items-center justify-center w-14 h-14 rounded-xl mb-4 bg-primary-100 dark:bg-primary-400/20">
                                <i class="pi pi-fw {{ feature.icon }} text-2xl! text-primary-600 dark:text-primary-300"></i>
                            </div>
                            <div class="flex items-center gap-2 mb-2">
                                <span class="text-surface-900 dark:text-surface-0 text-xl font-semibold">{{ feature.title }}</span>
                                @if (feature.badge) {
                                    <span class="text-xs font-semibold uppercase tracking-wide px-2 py-1 rounded-full bg-primary-100 dark:bg-primary-400/20 text-primary-700 dark:text-primary-300">{{ feature.badge }}</span>
                                }
                            </div>
                            <span class="text-surface-600 dark:text-surface-200 leading-normal">{{ feature.description }}</span>
                        </div>
                    </div>
                }

                <!-- Stats band: slanted edges via clip-path, primary gradient works over both themes -->
                <div class="col-span-12 mt-20 mb-10" reveal>
                    <div
                        class="py-16 md:py-20 px-6 md:px-16 text-white"
                        style="clip-path: polygon(0 2.5rem, 100% 0, 100% calc(100% - 2.5rem), 0 100%); background: linear-gradient(135deg, var(--p-primary-600), var(--p-primary-400))"
                    >
                        <div class="grid grid-cols-12 gap-8 text-center">
                            @for (stat of stats; track stat.label) {
                                <div class="col-span-6 lg:col-span-3">
                                    <div class="text-4xl md:text-5xl font-bold mb-2">{{ stat.value }}</div>
                                    <div class="text-white/85 text-lg">{{ stat.label }}</div>
                                </div>
                            }
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `
})
export class FeaturesWidget {
    features: Feature[] = [
        { icon: 'pi-box', title: 'Order management', description: 'Capture customer orders, confirm them in one click and follow their full lifecycle from intake to delivery.' },
        { icon: 'pi-sitemap', title: 'Load planning', description: 'Group orders into loads, set routes and weights, and auto-assign the best available carrier.' },
        { icon: 'pi-map-marker', title: 'Live tracking', description: 'Watch every shipment move on a live map with real-time GPS positions pushed over SignalR.' },
        { icon: 'pi-truck', title: 'Carrier network', description: 'Onboard carriers, track fleet size, capacity and drivers, and keep your network compliant.' },
        { icon: 'pi-wallet', title: 'Billing & invoicing', description: 'Draft, issue and settle invoices in multiple currencies, tied to the orders they came from.' },
        { icon: 'pi-bolt', title: 'AI intelligence', description: 'ETA predictions and exception detection that warn you before delays cost you money.', badge: 'Preview' }
    ];

    stats = [
        { value: '12', label: 'Services behind one gateway' },
        { value: 'Live', label: 'GPS tracking via SignalR' },
        { value: 'OIDC', label: 'Auth Code + PKCE security' },
        { value: 'Event', label: 'Driven — transactional outbox' }
    ];
}
