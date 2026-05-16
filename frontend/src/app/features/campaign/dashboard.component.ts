import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { InrPipe } from '../../shared/pipes/inr.pipe';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, InrPipe],
  template: `
    <section class="container-page py-12">
      <!-- Header -->
      <div class="flex items-end justify-between mb-10 gap-4">
        <div>
          <div class="eyebrow mb-2">Welcome back</div>
          <h1 class="font-display text-4xl font-bold tracking-tight">{{ auth.user()?.fullName }}</h1>
          <p class="text-sm text-ink-500 mt-1">Track all your campaigns, bookings and creative approvals in one place.</p>
        </div>
        <div class="flex gap-3">
          <a routerLink="/inventory" class="btn-secondary">Browse inventory</a>
          <button (click)="auth.logout()" class="btn-ghost btn-sm">Sign out</button>
        </div>
      </div>

      <!-- KPI strip -->
      <div class="grid sm:grid-cols-2 lg:grid-cols-4 gap-px bg-ink-100 border border-ink-100 rounded overflow-hidden mb-12">
        @for (k of kpis; track k.label) {
          <div class="bg-surface p-6">
            <div class="eyebrow mb-2">{{ k.label }}</div>
            <div class="font-display text-3xl font-bold num">{{ k.value }}</div>
            @if (k.delta) {
              <div class="text-2xs mt-1 num"
                   [class.text-signal-positive-500]="k.delta!.startsWith('+')"
                   [class.text-signal-negative-500]="k.delta!.startsWith('-')">
                {{ k.delta }}
              </div>
            }
          </div>
        }
      </div>

      <!-- Two columns: campaigns list + activity -->
      <div class="grid lg:grid-cols-3 gap-8">
        <div class="lg:col-span-2">
          <div class="flex items-center justify-between mb-4">
            <h2 class="font-display text-xl font-semibold">Active campaigns</h2>
            <button class="btn-secondary btn-sm">+ New campaign</button>
          </div>
          <div class="card overflow-hidden">
            <table class="w-full text-sm">
              <thead class="bg-ink-50">
                <tr class="text-left">
                  <th class="px-5 py-3 eyebrow text-ink-400">Campaign</th>
                  <th class="px-5 py-3 eyebrow text-ink-400">Status</th>
                  <th class="px-5 py-3 eyebrow text-ink-400 text-right">Spend</th>
                  <th class="px-5 py-3 eyebrow text-ink-400">Period</th>
                </tr>
              </thead>
              <tbody>
                @for (c of mockCampaigns; track c.id) {
                  <tr class="border-t border-ink-100 hover:bg-ink-50/50 cursor-pointer transition-colors">
                    <td class="px-5 py-4">
                      <div class="font-display text-base font-semibold">{{ c.name }}</div>
                      <div class="text-2xs uppercase tracking-widest text-ink-400 num">{{ c.code }}</div>
                    </td>
                    <td class="px-5 py-4">
                      <span class="chip"
                            [class.chip-accent]="c.status === 'Active'"
                            [class.bg-signal-warning-50]="c.status === 'Scheduled'"
                            [class.bg-ink-100]="c.status === 'Completed'">
                        {{ c.status }}
                      </span>
                    </td>
                    <td class="px-5 py-4 text-right num">{{ c.spend | inr }}</td>
                    <td class="px-5 py-4 text-2xs num text-ink-500">{{ c.period }}</td>
                  </tr>
                }
              </tbody>
            </table>
            @if (mockCampaigns.length === 0) {
              <div class="p-12 text-center">
                <p class="text-ink-500 text-sm">No campaigns yet.
                  <a routerLink="/inventory" class="underline">Browse inventory →</a>
                </p>
              </div>
            }
          </div>
        </div>

        <!-- Activity column -->
        <div>
          <h2 class="font-display text-xl font-semibold mb-4">Recent activity</h2>
          <div class="card p-5">
            <ol class="space-y-4">
              @for (a of activity; track a.id) {
                <li class="flex gap-3 text-sm">
                  <div class="w-8 h-8 rounded-full bg-ink-100 flex items-center justify-center shrink-0 text-2xs font-mono">
                    {{ a.icon }}
                  </div>
                  <div class="min-w-0">
                    <div class="text-ink-900">{{ a.text }}</div>
                    <div class="text-2xs text-ink-400 mt-0.5 num">{{ a.time }}</div>
                  </div>
                </li>
              }
            </ol>
          </div>

          <!-- Suggestions card -->
          <div class="card p-5 mt-4 bg-accent-50 border-accent-100">
            <div class="eyebrow text-accent-700 mb-2">Suggested next step</div>
            <p class="text-sm text-ink-700 mb-3">
              Add geo-tagged proof of posting permissions to your team for tighter campaign tracking.
            </p>
            <button class="text-xs underline underline-offset-4 text-accent-700">Configure proofs →</button>
          </div>
        </div>
      </div>
    </section>
  `
})
export class DashboardComponent {
  protected auth = inject(AuthService);

  // Stub data — wire to bookings/campaigns endpoints in Phase 2
  protected readonly kpis: { label: string; value: string; delta?: string }[] = [
    { label: 'Active campaigns', value: '0' },
    { label: 'Total spend (30d)', value: '₹0' },
    { label: 'Hoardings live',   value: '0' },
    { label: 'Creator deliverables live', value: '0' },
  ];

  protected readonly mockCampaigns: { id: string; code: string; name: string; status: string; spend: number; period: string }[] = [];

  protected readonly activity = [
    { id: 1, icon: '👋', text: 'Account created. Welcome to Hoardly.', time: 'Just now' }
  ];
}
