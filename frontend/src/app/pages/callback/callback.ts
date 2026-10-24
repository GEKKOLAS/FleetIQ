import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';

@Component({
    selector: 'app-callback',
    standalone: true,
    template: `
        <div class="flex flex-col items-center justify-center min-h-screen gap-4">
            <i class="pi pi-spin pi-spinner" style="font-size: 2.5rem"></i>
            <span class="text-muted-color">Signing you in…</span>
        </div>
    `
})
export class CallbackComponent implements OnInit {
    private oidc = inject(OidcSecurityService);
    private router = inject(Router);

    ngOnInit(): void {
        this.oidc.isAuthenticated$.subscribe(({ isAuthenticated }) => {
            if (isAuthenticated) this.router.navigateByUrl('/app');
        });
    }
}
