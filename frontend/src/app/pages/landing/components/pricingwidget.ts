import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DividerModule } from 'primeng/divider';
import { RippleModule } from 'primeng/ripple';
import { RevealDirective } from './reveal.directive';

interface Plan {
    name: string;
    price: string;
    period: string;
    cta: string;
    highlight: boolean;
    features: string[];
}

@Component({
    selector: 'pricing-widget',
    standalone: true,
    imports: [DividerModule, ButtonModule, RippleModule, RouterModule, RevealDirective],
    template: `
        <div id="pricing" class="py-6 px-6 lg:px-20 my-2 md:my-12">
            <div class="text-center mb-6" reveal>
                <div class="text-surface-900 dark:text-surface-0 font-semibold mb-2 text-4xl">Pricing that scales with your fleet</div>
                <span class="text-muted-color text-2xl">Start free, upgrade when the trucks do.</span>
            </div>

            <div class="grid grid-cols-12 gap-6 justify-between mt-16 md:mt-10">
                @for (plan of plans; track plan.name; let i = $index) {
                    <div class="col-span-12 lg:col-span-4 p-0 md:p-4" reveal [revealDelay]="i * 0.12">
                        <div
                            class="p-6 h-full flex flex-col rounded-2xl border-2 transition-all duration-300 hover:-translate-y-1.5 hover:shadow-xl relative"
                            [class]="plan.highlight ? 'border-primary bg-surface-0 dark:bg-surface-900 shadow-lg' : 'border-surface-200 dark:border-surface-700 bg-surface-0 dark:bg-surface-900 hover:border-primary'"
                        >
                            @if (plan.highlight) {
                                <span class="absolute -top-4 left-1/2 -translate-x-1/2 text-sm font-semibold uppercase tracking-wide px-4 py-1.5 rounded-full text-white" style="background: var(--p-primary-500)">Most popular</span>
                            }
                            <div class="text-surface-900 dark:text-surface-0 text-center mt-4 mb-2 text-2xl font-semibold">{{ plan.name }}</div>
                            <div class="my-6 flex flex-col items-center gap-4">
                                <div class="flex items-baseline">
                                    <span class="text-5xl font-bold mr-2 text-surface-900 dark:text-surface-0">{{ plan.price }}</span>
                                    <span class="text-surface-600 dark:text-surface-200">{{ plan.period }}</span>
                                </div>
                                <button pButton pRipple [label]="plan.cta" [rounded]="true" [outlined]="!plan.highlight" routerLink="/app" class="font-medium"></button>
                            </div>
                            <p-divider class="w-full"></p-divider>
                            <ul class="my-6 list-none p-0 flex text-surface-900 dark:text-surface-0 flex-col gap-1 px-2">
                                @for (feature of plan.features; track feature) {
                                    <li class="py-2 flex items-start">
                                        <i class="pi pi-fw pi-check-circle text-xl text-primary mr-3 mt-0.5"></i>
                                        <span class="text-lg leading-normal">{{ feature }}</span>
                                    </li>
                                }
                            </ul>
                        </div>
                    </div>
                }
            </div>
        </div>
    `
})
export class PricingWidget {
    plans: Plan[] = [
        {
            name: 'Starter',
            price: '$0',
            period: 'per month',
            cta: 'Start free',
            highlight: false,
            features: ['Up to 50 orders / month', '1 dispatcher seat', 'Order & invoice management', 'Community support']
        },
        {
            name: 'Growth',
            price: '$49',
            period: 'per month',
            cta: 'Get started',
            highlight: true,
            features: ['Unlimited orders & loads', 'Live GPS tracking map', 'Load planning with auto-assign', 'Multi-currency invoicing', 'Priority support']
        },
        {
            name: 'Enterprise',
            price: 'Custom',
            period: 'talk to us',
            cta: 'Contact sales',
            highlight: false,
            features: ['Everything in Growth', 'SSO / OIDC integration', 'Dedicated environment', 'AI predictions preview', 'SLA & guided onboarding']
        }
    ];
}
