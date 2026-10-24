import { Component, OnInit, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';

@Component({
    selector: 'app-root',
    standalone: true,
    imports: [RouterModule],
    template: `<router-outlet></router-outlet>`
})
export class AppComponent implements OnInit {
    private oidc = inject(OidcSecurityService);

    ngOnInit(): void {
        // Restores an existing session and processes the /callback after login.
        this.oidc.checkAuth().subscribe();
    }
}
