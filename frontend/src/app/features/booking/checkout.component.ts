import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { BookingService, CartService } from '../../core/services/inventory.service';
import { InrPipe } from '../../shared/pipes/inr.pipe';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, InrPipe],
  template: `
    <div class="container">
      <h1>Checkout</h1>
      <p class="lede" *ngIf="cart.cart().length > 0">
        Review your media plan, then confirm to create bookings. You'll be invoiced once
        each owner approves the brief.
      </p>

      <div class="layout" *ngIf="cart.cart().length > 0; else empty">
        <main>
          <div class="line" *ngFor="let item of cart.cart()">
            <div class="line-head">
              <span class="badge">{{ item.inventoryUnit.channel }}</span>
              <h3>{{ item.inventoryUnit.name }}</h3>
            </div>
            <p class="meta" *ngIf="item.inventoryUnit.hoarding as h">
              {{ h.areaName }}, {{ h.cityName }} ·
              {{ h.widthFt }}×{{ h.heightFt }} ft
            </p>
            <p class="meta" *ngIf="item.inventoryUnit.influencer as inf">
              {{ '@' + inf.handle }} · {{ inf.platform }} ·
              {{ formatFollowers(inf.followerCount) }}
            </p>
            <p class="meta">{{ item.startDate }} → {{ item.endDate }}</p>
            <p class="meta deliverable" *ngIf="item.deliverableSpec as spec">
              Deliverable: {{ spec.quantity }} × {{ spec.deliverable }}
            </p>
            <p class="amt">{{ item.quotedPrice | inr }}</p>
          </div>
        </main>

        <aside class="pay">
          <h3>Total</h3>
          <p class="big">{{ cart.total() | inr }}</p>
          <p class="muted">Includes 18% GST</p>
          <button class="btn-primary" (click)="confirm()" [disabled]="processing()">
            {{ processing() ? 'Creating bookings...' : 'Confirm &amp; book' }}
          </button>
          <p class="muted small">Single GST invoice will be issued covering all line items across channels.</p>
          <p class="error" *ngIf="error()">{{ error() }}</p>
        </aside>
      </div>

      <ng-template #empty>
        <p>Cart is empty.</p>
      </ng-template>
    </div>
  `,
  styles: [`
    .container { max-width: 1100px; margin: 0 auto; padding: 2rem 1.5rem; }
    h1 { font-family: var(--font-display); font-size: 2rem; font-weight: 600; margin: 0 0 0.5rem; }
    .lede { color: var(--ink-muted); margin: 0 0 1.5rem; max-width: 38rem; line-height: 1.6; }
    .layout { display: grid; grid-template-columns: 1fr 320px; gap: 2rem; }
    @media (max-width: 800px) { .layout { grid-template-columns: 1fr; } }
    .line { background: white; border: 1px solid var(--rule); border-radius: 12px; padding: 1.25rem; margin-bottom: 1rem; }
    .line-head { display: flex; align-items: center; gap: 0.75rem; margin-bottom: 0.5rem; }
    .badge { background: var(--ink); color: white; padding: 0.25rem 0.625rem; border-radius: 999px; font-size: 0.625rem; text-transform: uppercase; letter-spacing: 0.04em; font-weight: 600; }
    .line h3 { font-family: var(--font-display); font-size: 1rem; font-weight: 600; margin: 0; }
    .meta { font-size: 0.8125rem; color: var(--ink-muted); margin: 0.25rem 0; font-variant-numeric: tabular-nums; }
    .meta.deliverable { color: var(--ink); }
    .amt { font-weight: 600; font-variant-numeric: tabular-nums; margin: 0.5rem 0 0; }
    .pay { background: white; border: 1px solid var(--rule); border-radius: 12px; padding: 1.5rem; height: fit-content; position: sticky; top: 1rem; }
    .pay h3 { font-family: var(--font-display); margin: 0 0 0.5rem; font-size: 1.125rem; font-weight: 600; }
    .pay .big { font-family: var(--font-display); font-size: 2rem; font-weight: 600; margin: 0; font-variant-numeric: tabular-nums; }
    .muted { color: var(--ink-muted); font-size: 0.8125rem; margin: 0.25rem 0 0.875rem; }
    .muted.small { font-size: 0.75rem; margin-top: 1rem; }
    .btn-primary { width: 100%; padding: 0.75rem; background: var(--accent); color: white; border: 0; border-radius: 6px; font-weight: 500; cursor: pointer; }
    .btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }
    .error { color: #c0392b; font-size: 0.8125rem; margin-top: 0.625rem; }
  `],
})
export class CheckoutComponent {
  cart = inject(CartService);
  private booking = inject(BookingService);
  private router = inject(Router);

  processing = signal(false);
  error = signal<string | null>(null);

  async confirm() {
    this.processing.set(true);
    this.error.set(null);
    try {
      for (const item of this.cart.cart()) {
        const spec = item.deliverableSpec ? JSON.stringify(item.deliverableSpec) : undefined;
        await this.booking.create({
          inventoryUnitId: item.inventoryUnit.id,
          startDate: item.startDate,
          endDate: item.endDate,
          deliverableSpecJson: spec,
          includeSetupCost: true,
        }).toPromise();
      }
      this.cart.clear();
      this.router.navigate(['/dashboard'], { queryParams: { booked: 1 } });
    } catch (e: any) {
      this.error.set(e?.error?.error ?? e?.message ?? 'Could not create bookings.');
    } finally {
      this.processing.set(false);
    }
  }

  formatFollowers(n: number): string {
    if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
    if (n >= 1_000) return `${(n / 1_000).toFixed(0)}K`;
    return `${n}`;
  }
}
