import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent),
  },
  {
    path: 'inventory',
    loadComponent: () => import('./features/inventory/search.component').then(m => m.SearchComponent),
  },
  {
    path: 'inventory/:id',
    loadComponent: () => import('./features/inventory/detail.component').then(m => m.InventoryDetailComponent),
  },
  // Convenience aliases
  {
    path: 'hoardings',
    loadComponent: () => import('./features/inventory/search.component').then(m => m.SearchComponent),
    data: { channel: 'Hoarding' },
  },
  {
    path: 'creators',
    loadComponent: () => import('./features/inventory/search.component').then(m => m.SearchComponent),
    data: { channel: 'Influencer' },
  },
  {
    path: 'cart',
    loadComponent: () => import('./features/booking/cart.component').then(m => m.CartComponent),
  },
  {
    path: 'checkout',
    canActivate: [authGuard],
    loadComponent: () => import('./features/booking/checkout.component').then(m => m.CheckoutComponent),
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register.component').then(m => m.RegisterComponent),
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () => import('./features/campaign/dashboard.component').then(m => m.DashboardComponent),
  },
  { path: '**', redirectTo: '' },
];
