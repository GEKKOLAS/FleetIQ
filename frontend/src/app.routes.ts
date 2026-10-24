import { Routes } from '@angular/router';
import { AppLayout } from './app/layout/component/app.layout';
import { authGuard } from './app/core/auth.guard';
import { Analytics } from './app/pages/analytics/analytics';
import { Billing } from './app/pages/billing/billing';
import { Carriers } from './app/pages/carriers/carriers';
import { CallbackComponent } from './app/pages/callback/callback';
import { Compliance } from './app/pages/compliance/compliance';
import { Dashboard } from './app/pages/dashboard/dashboard';
import { Documents } from './app/pages/documents/documents';
import { Integration } from './app/pages/integration/integration';
import { Intelligence } from './app/pages/intelligence/intelligence';
import { Landing } from './app/pages/landing/landing';
import { Notfound } from './app/pages/notfound/notfound';
import { Notifications } from './app/pages/notifications/notifications';
import { Orders } from './app/pages/orders/orders';
import { Planning } from './app/pages/planning/planning';
import { Tracking } from './app/pages/tracking/tracking';

export const appRoutes: Routes = [
    { path: '', component: Landing },
    { path: 'callback', component: CallbackComponent },
    { path: 'auth', loadChildren: () => import('./app/pages/auth/auth.routes') },
    {
        path: 'app',
        component: AppLayout,
        canActivate: [authGuard],
        children: [
            { path: '', component: Dashboard },
            { path: 'orders', component: Orders },
            { path: 'carriers', component: Carriers },
            { path: 'planning', component: Planning },
            { path: 'tracking', component: Tracking },
            { path: 'billing', component: Billing },
            { path: 'documents', component: Documents },
            { path: 'notifications', component: Notifications },
            { path: 'analytics', component: Analytics },
            { path: 'compliance', component: Compliance },
            { path: 'integration', component: Integration },
            { path: 'intelligence', component: Intelligence }
        ]
    },
    { path: 'notfound', component: Notfound },
    { path: '**', redirectTo: '/notfound' }
];
