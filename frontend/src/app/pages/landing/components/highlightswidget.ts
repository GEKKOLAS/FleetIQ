import { Component } from '@angular/core';
import { RevealDirective } from './reveal.directive';

@Component({
    selector: 'highlights-widget',
    standalone: true,
    imports: [RevealDirective],
    template: `
        <!-- Full-bleed slanted band; surface tones adapt to light/dark -->
        <section id="highlights" class="bg-surface-100 dark:bg-surface-800 py-24 my-12" style="clip-path: polygon(0 3rem, 100% 0, 100% calc(100% - 3rem), 0 100%)">
            <div class="px-6 lg:px-20 mx-0 lg:mx-20">
                <div class="text-center" reveal>
                    <div class="text-surface-900 dark:text-surface-0 font-semibold mb-2 text-4xl">See your operation, live</div>
                    <span class="text-muted-color text-2xl">Real screens from FleetIQ — not mockups.</span>
                </div>

                <div class="grid grid-cols-12 gap-10 mt-20 pb-2 items-center">
                    <div class="col-span-12 lg:col-span-7 order-1 lg:order-0" reveal revealFrom="left">
                        <img src="/media/app-tracking.png" alt="FleetIQ live tracking map with in-transit shipments" class="w-full rounded-xl shadow-2xl border border-surface-200 dark:border-surface-700" loading="lazy" />
                    </div>

                    <div class="col-span-12 lg:col-span-5 flex flex-col lg:items-start text-center lg:text-left gap-4" reveal revealFrom="right">
                        <div class="flex items-center justify-center bg-primary-100 dark:bg-primary-400/20 self-center lg:self-start w-16 h-16 rounded-xl">
                            <i class="pi pi-fw pi-map text-3xl! text-primary-600 dark:text-primary-300"></i>
                        </div>
                        <div class="leading-tight text-surface-900 dark:text-surface-0 text-3xl font-semibold">Live tracking on a real map</div>
                        <span class="text-surface-700 dark:text-surface-100 text-xl leading-normal">
                            Every shipment reports its GPS position in real time. Watch trucks move, spot the ones stuck in "Created", and drill into any shipment without leaving the map.
                        </span>
                    </div>
                </div>

                <div class="grid grid-cols-12 gap-10 mt-24 pt-2 items-center">
                    <div class="col-span-12 lg:col-span-5 flex flex-col text-center lg:text-left lg:items-start gap-4" reveal revealFrom="left">
                        <div class="flex items-center justify-center bg-primary-100 dark:bg-primary-400/20 self-center lg:self-start w-16 h-16 rounded-xl">
                            <i class="pi pi-fw pi-gauge text-3xl! text-primary-600 dark:text-primary-300"></i>
                        </div>
                        <div class="leading-tight text-surface-900 dark:text-surface-0 text-3xl font-semibold">One dashboard for the whole flow</div>
                        <span class="text-surface-700 dark:text-surface-100 text-xl leading-normal">
                            Orders, confirmed loads, issued invoices and amounts due — pulled live from the gateway so your numbers always match the operation on the ground.
                        </span>
                    </div>

                    <div class="col-span-12 lg:col-span-7 order-1 lg:order-2" reveal revealFrom="right">
                        <img src="/media/app-dashboard.png" alt="FleetIQ dashboard with orders, invoices and issued amount KPIs" class="w-full rounded-xl shadow-2xl border border-surface-200 dark:border-surface-700" loading="lazy" />
                    </div>
                </div>
            </div>
        </section>
    `
})
export class HighlightsWidget {}
