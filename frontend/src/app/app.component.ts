import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { CartService } from './core/services/inventory.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <!-- Header -->
    <header class="sticky top-0 z-40 bg-canvas/85 backdrop-blur-md border-b border-ink-100">
      <div class="container-page flex items-center justify-between h-16">
        <a routerLink="/" class="flex items-baseline gap-2">
          <span class="font-display text-2xl font-bold tracking-tight">Hoardly</span>
          <span class="eyebrow text-ink-300 hidden sm:block">Hoardings · Creators · India</span>
        </a>

        <nav class="hidden md:flex items-center gap-7 text-sm">
          <a routerLink="/inventory" routerLinkActive="text-ink-900" [routerLinkActiveOptions]="{exact:false}"
             class="text-ink-500 hover:text-ink-900 transition-colors">Inventory</a>
          <a routerLink="/hoardings"
             class="text-ink-500 hover:text-ink-900 transition-colors">Hoardings</a>
          <a routerLink="/creators"
             class="text-ink-500 hover:text-ink-900 transition-colors">Creators</a>
          <a routerLink="/dashboard" routerLinkActive="text-ink-900"
             class="text-ink-500 hover:text-ink-900 transition-colors">Campaigns</a>
        </nav>

        <div class="flex items-center gap-3">
          <a routerLink="/cart" class="relative btn-ghost btn-sm">
            <svg viewBox="0 0 20 20" fill="none" stroke="currentColor" class="w-4 h-4" stroke-width="1.6">
              <path d="M3 3h2l1.5 9.5a1.5 1.5 0 0 0 1.5 1.25h7.5a1.5 1.5 0 0 0 1.5-1.25L18 6H6"/>
              <circle cx="8.5" cy="17" r="1"/>
              <circle cx="14.5" cy="17" r="1"/>
            </svg>
            <span class="hidden sm:inline">Plan</span>
            @if (cart.count() > 0) {
              <span class="absolute -top-1 -right-1 num bg-ink-900 text-canvas text-[10px] w-5 h-5 rounded-full flex items-center justify-center">
                {{ cart.count() }}
              </span>
            }
          </a>

          @if (auth.isAuthenticated()) {
            <a routerLink="/dashboard" class="btn-secondary btn-sm">{{ auth.userInitials() }}</a>
          } @else {
            <a routerLink="/login" class="btn-ghost btn-sm hidden sm:inline-flex">Sign in</a>
            <a routerLink="/register" class="btn-primary btn-sm">Get started</a>
          }
        </div>
      </div>
    </header>

    <main class="min-h-[calc(100vh-4rem-12rem)]">
      <router-outlet/>
    </main>

    <!-- Footer -->
    <footer class="bg-ink-900 text-ink-200 mt-24">
      <div class="container-page py-16 grid md:grid-cols-4 gap-10">
        <div class="md:col-span-2">
          <div class="font-display text-3xl font-bold text-canvas mb-3">Hoardly</div>
          <p class="text-sm text-ink-300 max-w-sm leading-relaxed">
            India's transparent multi-channel advertising marketplace — hoardings, creators, and more in
            one media plan. Real-time availability, instant quotes, single GST invoice.
          </p>
        </div>
        <div>
          <div class="eyebrow text-ink-400 mb-3">Inventory</div>
          <ul class="space-y-2 text-sm">
            <li><a class="hover:text-canvas">Mumbai</a></li>
            <li><a class="hover:text-canvas">Bengaluru</a></li>
            <li><a class="hover:text-canvas">Delhi NCR</a></li>
            <li><a class="hover:text-canvas">Hyderabad</a></li>
          </ul>
        </div>
        <div>
          <div class="eyebrow text-ink-400 mb-3">Company</div>
          <ul class="space-y-2 text-sm">
            <li><a class="hover:text-canvas">About</a></li>
            <li><a class="hover:text-canvas">Case studies</a></li>
            <li><a class="hover:text-canvas">For media owners</a></li>
            <li><a class="hover:text-canvas">Contact</a></li>
          </ul>
        </div>
      </div>
      <div class="border-t border-ink-700">
        <div class="container-page py-5 flex justify-between items-center text-2xs text-ink-400">
          <span>© {{ year }} Hoardly Technologies Pvt Ltd</span>
          <span class="num">v0.1 · MVP</span>
        </div>
      </div>
    </footer>
  `
})
export class AppComponent {
  protected auth = inject(AuthService);
  protected cart = inject(CartService);
  protected readonly year = new Date().getFullYear();
}
