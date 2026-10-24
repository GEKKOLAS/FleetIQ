import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { MenuItem } from 'primeng/api';
import { AppMenuitem } from './app.menuitem';

@Component({
    selector: 'app-menu',
    standalone: true,
    imports: [CommonModule, AppMenuitem, RouterModule],
    template: `<ul class="layout-menu">
        <ng-container *ngFor="let item of model; let i = index">
            <li app-menuitem *ngIf="!item.separator" [item]="item" [index]="i" [root]="true"></li>
            <li *ngIf="item.separator" class="menu-separator"></li>
        </ng-container>
    </ul> `
})
export class AppMenu {
    private oidc = inject(OidcSecurityService);
    model: MenuItem[] = [];

    ngOnInit() {
        this.model = [
            {
                label: 'FleetIQ',
                items: [
                    { label: 'Dashboard', icon: 'pi pi-fw pi-home', routerLink: ['/app'] },
                    { label: 'Orders', icon: 'pi pi-fw pi-box', routerLink: ['/app/orders'] },
                    { label: 'Carriers', icon: 'pi pi-fw pi-truck', routerLink: ['/app/carriers'] },
                    { label: 'Planning', icon: 'pi pi-fw pi-sitemap', routerLink: ['/app/planning'] },
                    { label: 'Live Tracking', icon: 'pi pi-fw pi-map-marker', routerLink: ['/app/tracking'] },
                    { label: 'Billing', icon: 'pi pi-fw pi-credit-card', routerLink: ['/app/billing'] },
                    { label: 'Documents', icon: 'pi pi-fw pi-file', routerLink: ['/app/documents'] },
                    { label: 'Notifications', icon: 'pi pi-fw pi-bell', routerLink: ['/app/notifications'] },
                    { label: 'Analytics', icon: 'pi pi-fw pi-chart-bar', routerLink: ['/app/analytics'] },
                    { label: 'Compliance', icon: 'pi pi-fw pi-shield', routerLink: ['/app/compliance'] },
                    { label: 'Integration', icon: 'pi pi-fw pi-arrows-h', routerLink: ['/app/integration'] },
                    { label: 'Intelligence', icon: 'pi pi-fw pi-bolt', routerLink: ['/app/intelligence'] }
                ]
            },
            {
                label: 'Account',
                items: [
                    { label: 'Landing', icon: 'pi pi-fw pi-globe', routerLink: ['/'] },
                    { label: 'Sign out', icon: 'pi pi-fw pi-sign-out', command: () => this.oidc.logoff().subscribe() }
                ]
            }
        ];
    }
}
