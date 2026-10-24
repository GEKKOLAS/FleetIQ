import { AfterViewInit, Directive, ElementRef, Input, OnDestroy, inject } from '@angular/core';
import gsap from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';

gsap.registerPlugin(ScrollTrigger);

/**
 * Fades the host element in when it scrolls into view.
 * Skipped entirely under prefers-reduced-motion (content stays visible).
 */
@Directive({ selector: '[reveal]', standalone: true })
export class RevealDirective implements AfterViewInit, OnDestroy {
    @Input() revealDelay = 0;
    @Input() revealFrom: 'up' | 'left' | 'right' = 'up';

    private el = inject(ElementRef<HTMLElement>);
    private mm?: gsap.MatchMedia;

    ngAfterViewInit(): void {
        this.mm = gsap.matchMedia();
        this.mm.add('(prefers-reduced-motion: no-preference)', () => {
            const offset = this.revealFrom === 'up' ? { y: 40 } : this.revealFrom === 'left' ? { x: -48 } : { x: 48 };
            gsap.from(this.el.nativeElement, {
                autoAlpha: 0,
                ...offset,
                duration: 0.8,
                delay: this.revealDelay,
                ease: 'power2.out',
                scrollTrigger: { trigger: this.el.nativeElement, start: 'top 88%', once: true }
            });
        });
    }

    ngOnDestroy(): void {
        this.mm?.revert();
    }
}
