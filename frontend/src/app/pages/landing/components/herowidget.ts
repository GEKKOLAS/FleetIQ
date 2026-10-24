import { AfterViewInit, Component, ElementRef, ViewChild, inject } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import gsap from 'gsap';

@Component({
    selector: 'hero-widget',
    imports: [ButtonModule, RippleModule, RouterModule],
    template: `
        <section
            id="hero"
            class="relative flex items-center overflow-hidden bg-surface-950"
            style="min-height: calc(100dvh - 6rem); clip-path: polygon(0 0, 100% 0, 100% 100%, 0 calc(100% - 3.5rem))"
        >
            <!-- Decorative background video: muted + playsinline so mobile browsers allow autoplay -->
            <video #bgVideo class="absolute inset-0 w-full h-full object-cover" src="/media/hero-bg.mp4" autoplay muted loop playsinline preload="metadata" tabindex="-1" aria-hidden="true"></video>
            <!-- Scrim: keeps white copy at readable contrast over any video frame -->
            <div class="absolute inset-0 bg-gradient-to-r from-black/80 via-black/55 to-black/20" aria-hidden="true"></div>

            <div class="relative z-10 px-6 lg:px-20 py-20">
                <div class="mx-6 md:mx-20 max-w-3xl">
                    <span data-animate class="inline-block uppercase tracking-[0.2em] text-sm font-semibold text-primary-300 mb-4">AI-assisted transport management</span>
                    <h1 data-animate class="text-5xl md:text-6xl font-bold text-white! leading-tight">
                        <span class="font-light block text-white/80">FleetIQ</span>
                        Transport Management, simplified
                    </h1>
                    <p data-animate class="font-normal text-xl md:text-2xl leading-normal mt-6 text-white/90">
                        Plan loads, dispatch carriers, track shipments live and settle invoices — your entire freight operation on one platform.
                    </p>
                    <div data-animate class="flex flex-wrap items-center gap-4 mt-10">
                        <button pButton pRipple [rounded]="true" type="button" label="Enter app" routerLink="/app" class="text-xl! px-6!"></button>
                        <button
                            pButton
                            pRipple
                            [rounded]="true"
                            [outlined]="true"
                            type="button"
                            label="Explore features"
                            (click)="router.navigate(['/'], { fragment: 'features' })"
                            class="text-xl! px-6! text-white! border-white/60! hover:bg-white/10!"
                        ></button>
                    </div>
                </div>
            </div>
        </section>
    `
})
export class HeroWidget implements AfterViewInit {
    @ViewChild('bgVideo') bgVideo?: ElementRef<HTMLVideoElement>;

    private host = inject(ElementRef<HTMLElement>);

    constructor(public router: Router) {}

    ngAfterViewInit(): void {
        // Respect prefers-reduced-motion: hold the first frame, skip the entrance animation.
        if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
            const video = this.bgVideo?.nativeElement;
            if (video) {
                video.pause();
                video.currentTime = 0;
            }
            return;
        }
        gsap.from(this.host.nativeElement.querySelectorAll('[data-animate]'), {
            autoAlpha: 0,
            y: 28,
            duration: 0.9,
            ease: 'power2.out',
            stagger: 0.12,
            delay: 0.15
        });
    }
}
