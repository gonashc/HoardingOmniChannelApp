import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CartService } from '../../core/services/inventory.service';
import { InrPipe } from '../../shared/pipes/inr.pipe';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterLink, InrPipe],
  template: `
    <div class="container">
      <h1>Your media plan</h1>

      <div class="empty" *ngIf="cart.cart().length === 0">
        <p>Your cart is empty.</p>
        <a routerLink="/inventory" class="btn-primary">Browse inventory</a>
      </div>

      <div class="layout" *ngIf="cart.cart().length > 0">
        <main>
          <article class="line" *ngFor="let item of cart.cart()">
            <div class="thumb"
              [style.background-image]="'url(' + (item.inventoryUnit.coverImageUrl || '/assets/placeholder.jpg') + ')'">
              <span class="badge">{{ item.inventoryUnit.channel }}</span>
            </div>

            <div class="content">
              <h3>{{ item.inventoryUnit.name }}</h3>

              <ng-container *ngIf="item.inventoryUnit.hoarding as h">
                <p class="meta">{{ h.areaName }}, {{ h.cityName }}</p>
                <p class="meta">{{ h.widthFt }}×{{ h.heightFt }} ft · {{ h.illumination }}</p>
              </ng-container>

              <ng-container *ngIf="item.inventoryUnit.influencer as inf">
                <p class="meta">{{ '@' + inf.handle }} · {{ inf.platform }}</p>
                <p class="meta">{{ formatFollowers(inf.followerCount) }} · {{ inf.tier }}</p>
                <p class="meta deliverable" *ngIf="item.deliverableSpec as spec">
                  Deliverable: <strong>{{ spec.quantity }} × {{ spec.deliverable }}</strong>
                </p>
              </ng-container>

              <p class="dates">{{ item.startDate }} → {{ item.endDate }}</p>
            </div>

            <div class="actions">
              <p class="price">{{ item.quotedPrice | inr }}</p>
              <button (click)="cart.remove(item.inventoryUnit.id)">Remove</button>
            </div>
          </article>
        </main>

        <aside class="summary">
          <h3>Summary</h3>
          <div class="srow"><span>Items</span><strong>{{ cart.count() }}</strong></div>
          <div class="srow total"><span>Total (incl. GST)</span><strong>{{ cart.total() | inr }}</strong></div>
          <button class="btn-primary" routerLink="/checkout">Proceed to checkout</button>
          <button class="btn-text" (click)="cart.clear()">Clear cart</button>
        </aside>
      </div>
    </div>
  `,
  styles: [`
    .container { max-width: 1200px; margin: 0 auto; padding: 2rem 1.5rem; }
    h1 { font-family: var(--font-display); font-size: 2rem; font-weight: 600; margin: 0 0 1.5rem; }
    .empty { text-align: center; padding: 4rem 0; color: var(--ink-muted); }
    .empty .btn-primary { display: inline-block; margin-top: 1rem; padding: 0.75rem 1.5rem; background: var(--accent); color: white; text-decoration: none; border-radius: 6px; }
    .layout { display: grid; grid-template-columns: 1fr 320px; gap: 2rem; }
    @media (max-width: 800px) { .layout { grid-template-columns: 1fr; } }
    .line { background: white; border: 1px solid var(--rule); border-radius: 12px; padding: 1rem; display: grid; grid-template-columns: 120px 1fr auto; gap: 1.25rem; margin-bottom: 1rem; align-items: center; }
    .thumb { aspect-ratio: 4/3; background-size: cover; background-position: center; background-color: var(--rule); border-radius: 8px; position: relative; }
    .badge { position: absolute; top: 0.5rem; left: 0.5rem; background: rgba(255,255,255,0.92); padding: 0.125rem 0.5rem; border-radius: 999px; font-size: 0.625rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.04em; }
    .content h3 { font-family: var(--font-display); font-size: 1rem; font-weight: 600; margin: 0 0 0.25rem; }
    .meta { font-size: 0.8125rem; color: var(--ink-muted); margin: 0 0 0.25rem; }
    .meta.deliverable { color: var(--ink); margin-top: 0.375rem; }
    .dates { font-size: 0.75rem; color: var(--ink-muted); margin: 0.5rem 0 0; font-variant-numeric: tabular-nums; }
    .actions { text-align: right; }
    .actions .price { font-weight: 600; font-variant-numeric: tabular-nums; margin: 0 0 0.5rem; }
    .actions button { background: transparent; border: 0; color: var(--ink-muted); font-size: 0.8125rem; cursor: pointer; padding: 0.25rem 0.5rem; }
    .actions button:hover { color: var(--ink); text-decoration: underline; }
    .summary { background: white; border: 1px solid var(--rule); border-radius: 12px; padding: 1.5rem; height: fit-content; position: sticky; top: 1rem; }
    .summary h3 { font-family: var(--font-display); font-size: 1.125rem; font-weight: 600; margin: 0 0 1rem; }
    .srow { display: flex; justify-content: space-between; padding: 0.5rem 0; font-size: 0.875rem; font-variant-numeric: tabular-nums; }
    .srow.total { border-top: 1px solid var(--rule); padding-top: 0.875rem; margin-top: 0.375rem; font-size: 1rem; font-weight: 600; }
    .btn-primary { width: 100%; padding: 0.75rem; background: var(--accent); color: white; border: 0; border-radius: 6px; font-weight: 500; cursor: pointer; margin-top: 1rem; text-align: center; text-decoration: none; display: block; }
    .btn-text { width: 100%; padding: 0.5rem; background: transparent; color: var(--ink-muted); border: 0; cursor: pointer; margin-top: 0.5rem; font-size: 0.8125rem; }
  `],
})
export class CartComponent {
  cart = inject(CartService);

  formatFollowers(n: number): string {
    if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M followers`;
    if (n >= 1_000) return `${(n / 1_000).toFixed(0)}K followers`;
    return `${n} followers`;
  }
}
