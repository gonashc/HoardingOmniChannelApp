import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { InventoryService } from '../../core/services/inventory.service';
import { CartService } from '../../core/services/inventory.service';
import { InventoryDetail, QuoteResponse } from '../../core/models/models';
import { InrPipe } from '../../shared/pipes/inr.pipe';

@Component({
  selector: 'app-inventory-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, InrPipe],
  template: `
    <div class="container" *ngIf="detail() as d">
      <nav class="crumbs">
        <a (click)="back()">← Back</a>
        <span class="badge">{{ d.channel }}</span>
      </nav>

      <div class="layout">
        <main>
          <header class="hd">
            <h1>{{ d.name }}</h1>
            <p class="code">{{ d.code }}</p>
            <p class="desc">{{ d.description }}</p>
          </header>

          <!-- Cover image -->
          <div class="cover" *ngIf="coverUrl(d) as url" [style.background-image]="'url(' + url + ')'"></div>

          <!-- HOARDING-specific panels -->
          <ng-container *ngIf="d.hoarding as h">
            <section class="panel">
              <h2>Specifications</h2>
              <dl class="specs">
                <div><dt>Type</dt><dd>{{ h.type }}</dd></div>
                <div><dt>Illumination</dt><dd>{{ h.illumination }}</dd></div>
                <div><dt>Size</dt><dd>{{ h.widthFt }} × {{ h.heightFt }} ft</dd></div>
                <div *ngIf="h.facingDirection"><dt>Facing</dt><dd>{{ h.facingDirection }}</dd></div>
                <div *ngIf="h.visibilityScore"><dt>Visibility</dt><dd>{{ h.visibilityScore }} / 10</dd></div>
                <div *ngIf="d.estimatedReachDaily"><dt>Daily traffic</dt><dd>{{ d.estimatedReachDaily | number }}</dd></div>
              </dl>
            </section>

            <section class="panel">
              <h2>Location</h2>
              <p><strong>{{ h.location.address }}</strong></p>
              <p class="muted">{{ h.location.areaName }}, {{ h.location.cityName }}, {{ h.location.stateName }} {{ h.location.pincode }}</p>
              <p class="muted" *ngIf="h.location.landmark">Near: {{ h.location.landmark }}</p>
              <div class="map-placeholder">
                <p>Map view ({{ h.location.latitude }}, {{ h.location.longitude }})</p>
                <a *ngIf="h.streetViewUrl" [href]="h.streetViewUrl" target="_blank">View Street View →</a>
              </div>
            </section>
          </ng-container>

          <!-- INFLUENCER-specific panels -->
          <ng-container *ngIf="d.influencer as i">
            <section class="panel">
              <h2>Channel stats</h2>
              <dl class="specs">
                <div><dt>Platform</dt><dd>{{ i.platform }}</dd></div>
                <div><dt>Handle</dt>
                  <dd>
                    <a *ngIf="i.profileUrl; else justHandle" [href]="i.profileUrl" target="_blank">{{ '@' + i.handle }}</a>
                    <ng-template #justHandle>{{ '@' + i.handle }}</ng-template>
                    <span class="verify-pill" *ngIf="i.isPlatformVerified">✓ Verified</span>
                  </dd>
                </div>
                <div><dt>Followers</dt><dd>{{ i.followerCount | number }}</dd></div>
                <div *ngIf="i.avgViewsPerPost"><dt>Avg views</dt><dd>{{ i.avgViewsPerPost | number }}</dd></div>
                <div *ngIf="i.avgLikesPerPost"><dt>Avg likes</dt><dd>{{ i.avgLikesPerPost | number }}</dd></div>
                <div *ngIf="i.engagementRate"><dt>Engagement rate</dt><dd>{{ i.engagementRate }}%</dd></div>
                <div><dt>Tier</dt><dd>{{ i.tier }}</dd></div>
                <div *ngIf="i.metricsVerifiedAt"><dt>Last verified</dt><dd>{{ i.metricsVerifiedAt | date:'mediumDate' }}</dd></div>
              </dl>
            </section>

            <section class="panel">
              <h2>Audience demographics</h2>
              <div class="demo-grid">
                <div *ngIf="i.audienceAgeSplitJson">
                  <h4>Age</h4>
                  <ul class="bars">
                    <li *ngFor="let row of parseJson(i.audienceAgeSplitJson)">
                      <span class="lbl">{{ row.key }}</span>
                      <span class="bar"><span class="fill" [style.width.%]="row.value * 100"></span></span>
                      <span class="pct">{{ (row.value * 100).toFixed(0) }}%</span>
                    </li>
                  </ul>
                </div>
                <div *ngIf="i.audienceGenderSplitJson">
                  <h4>Gender</h4>
                  <ul class="bars">
                    <li *ngFor="let row of parseJson(i.audienceGenderSplitJson)">
                      <span class="lbl">{{ row.key }}</span>
                      <span class="bar"><span class="fill" [style.width.%]="row.value * 100"></span></span>
                      <span class="pct">{{ (row.value * 100).toFixed(0) }}%</span>
                    </li>
                  </ul>
                </div>
                <div *ngIf="i.audienceGeoSplitJson">
                  <h4>Geography</h4>
                  <ul class="bars">
                    <li *ngFor="let row of parseJson(i.audienceGeoSplitJson)">
                      <span class="lbl">{{ row.key }}</span>
                      <span class="bar"><span class="fill" [style.width.%]="row.value * 100"></span></span>
                      <span class="pct">{{ (row.value * 100).toFixed(0) }}%</span>
                    </li>
                  </ul>
                </div>
              </div>
            </section>

            <section class="panel">
              <h2>Categories &amp; languages</h2>
              <div class="tag-row">
                <span class="tag" *ngFor="let c of i.contentCategories">{{ c }}</span>
              </div>
              <div class="tag-row" style="margin-top: 0.75rem">
                <span class="tag tag-light" *ngFor="let l of i.languages">{{ l | uppercase }}</span>
              </div>
              <p class="muted" style="margin-top: 1rem" *ngIf="i.excludesCategories?.length">
                Won't promote: {{ i.excludesCategories.join(', ') }}
              </p>
              <p class="muted" *ngIf="i.requiresPaidPartnershipTag">
                Requires paid-partnership disclosure on all branded content.
              </p>
              <p class="muted">Typical turnaround: {{ i.typicalTurnaroundDays }} days</p>
            </section>

            <section class="panel">
              <h2>Rate card</h2>
              <table class="rates">
                <thead><tr><th>Deliverable</th><th>Rate</th></tr></thead>
                <tbody>
                  <tr *ngFor="let r of parseJson(i.deliverablePricingJson || '{}')">
                    <td>{{ r.key }}</td>
                    <td>{{ r.value | inr }}</td>
                  </tr>
                </tbody>
              </table>
            </section>
          </ng-container>
        </main>

        <!-- Booking sidebar -->
        <aside class="book-card">
          <div class="price-line" *ngIf="d.channel === 'Hoarding'">
            <strong>{{ d.pricing.monthlyRate | inr }}</strong> <small>/ month</small>
          </div>
          <div class="price-line" *ngIf="d.channel === 'Influencer'">
            <strong>{{ d.pricing.pricePerUnit | inr }}</strong> <small>/ {{ d.pricing.unitLabel }}</small>
          </div>

          <div class="form-row">
            <label>Start date</label>
            <input type="date" [(ngModel)]="startDate" />
          </div>
          <div class="form-row">
            <label>End date</label>
            <input type="date" [(ngModel)]="endDate" />
          </div>

          <!-- Influencer deliverable picker -->
          <ng-container *ngIf="d.influencer as i">
            <div class="form-row">
              <label>Deliverable</label>
              <select [(ngModel)]="deliverable">
                <option *ngFor="let v of i.availableDeliverables" [value]="v.toLowerCase()">{{ v }}</option>
              </select>
            </div>
            <div class="form-row">
              <label>Quantity</label>
              <input type="number" min="1" [(ngModel)]="quantity" />
            </div>
          </ng-container>

          <button class="btn-primary" (click)="getQuote()" [disabled]="!canQuote()">Get instant quote</button>

          <div class="quote-box" *ngIf="quote() as q">
            <div class="qrow"><span>Base</span><strong>{{ q.baseAmount | inr }}</strong></div>
            <div class="qrow" *ngIf="q.setupCost > 0"><span>Setup</span><strong>{{ q.setupCost | inr }}</strong></div>
            <div class="qrow"><span>GST ({{ d.pricing.gstPercentage }}%)</span><strong>{{ q.gstAmount | inr }}</strong></div>
            <div class="qrow total"><span>Total</span><strong>{{ q.totalAmount | inr }}</strong></div>
            <button class="btn-primary" (click)="addToCart()">Add to cart</button>
          </div>
        </aside>
      </div>
    </div>
  `,
  styles: [`
    .container { max-width: 1200px; margin: 0 auto; padding: 2rem 1.5rem; }
    .crumbs { display: flex; align-items: center; gap: 1rem; margin-bottom: 1rem; }
    .crumbs a { color: var(--accent); cursor: pointer; text-decoration: none; font-size: 0.875rem; }
    .crumbs .badge { background: var(--ink); color: white; padding: 0.25rem 0.625rem; border-radius: 999px; font-size: 0.6875rem; text-transform: uppercase; letter-spacing: 0.04em; font-weight: 600; }
    .layout { display: grid; grid-template-columns: 1fr 360px; gap: 2rem; }
    @media (max-width: 900px) { .layout { grid-template-columns: 1fr; } }
    .hd h1 { font-family: var(--font-display); font-size: 2rem; font-weight: 600; line-height: 1.15; margin: 0 0 0.25rem; }
    .hd .code { color: var(--ink-muted); font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.06em; margin: 0 0 0.75rem; }
    .hd .desc { color: var(--ink-muted); margin: 0 0 1.5rem; line-height: 1.6; }
    .cover { aspect-ratio: 16/9; background-size: cover; background-position: center; border-radius: 12px; background-color: var(--rule); margin-bottom: 1.5rem; }
    .panel { background: white; border: 1px solid var(--rule); border-radius: 12px; padding: 1.5rem; margin-bottom: 1rem; }
    .panel h2 { font-family: var(--font-display); font-size: 1.125rem; font-weight: 600; margin: 0 0 1rem; }
    .specs { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 0.875rem 1.5rem; margin: 0; }
    .specs > div { display: flex; flex-direction: column; }
    .specs dt { font-size: 0.6875rem; color: var(--ink-muted); text-transform: uppercase; letter-spacing: 0.06em; font-weight: 600; }
    .specs dd { font-size: 0.9375rem; color: var(--ink); margin: 0.25rem 0 0; font-variant-numeric: tabular-nums; }
    .verify-pill { background: var(--accent); color: white; font-size: 0.625rem; padding: 0.125rem 0.5rem; border-radius: 999px; margin-left: 0.5rem; vertical-align: middle; }
    .muted { color: var(--ink-muted); margin: 0.25rem 0; font-size: 0.875rem; }
    .map-placeholder { background: var(--canvas); border: 1px dashed var(--rule); border-radius: 8px; padding: 2rem; text-align: center; margin-top: 1rem; }
    .demo-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 1.5rem; }
    .demo-grid h4 { font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.06em; color: var(--ink-muted); margin: 0 0 0.625rem; font-weight: 600; }
    .bars { list-style: none; padding: 0; margin: 0; }
    .bars li { display: grid; grid-template-columns: 56px 1fr 40px; gap: 0.5rem; align-items: center; margin-bottom: 0.5rem; font-size: 0.8125rem; }
    .lbl { color: var(--ink-muted); }
    .bar { background: var(--canvas); height: 6px; border-radius: 3px; overflow: hidden; }
    .fill { display: block; height: 100%; background: var(--accent); border-radius: 3px; }
    .pct { font-variant-numeric: tabular-nums; text-align: right; color: var(--ink); }
    .tag-row { display: flex; flex-wrap: wrap; gap: 0.375rem; }
    .tag { background: var(--accent); color: white; padding: 0.25rem 0.75rem; border-radius: 999px; font-size: 0.75rem; font-weight: 500; }
    .tag-light { background: var(--canvas); color: var(--ink); border: 1px solid var(--rule); }
    .rates { width: 100%; border-collapse: collapse; }
    .rates th, .rates td { padding: 0.625rem; text-align: left; border-bottom: 1px solid var(--rule); font-size: 0.875rem; }
    .rates th { font-weight: 600; color: var(--ink-muted); text-transform: uppercase; font-size: 0.6875rem; letter-spacing: 0.06em; }
    .rates td:last-child { font-variant-numeric: tabular-nums; text-align: right; font-weight: 600; }
    .book-card { background: white; border: 1px solid var(--rule); border-radius: 12px; padding: 1.5rem; height: fit-content; position: sticky; top: 1rem; }
    .price-line { font-family: var(--font-display); margin-bottom: 1rem; padding-bottom: 1rem; border-bottom: 1px solid var(--rule); }
    .price-line strong { font-size: 1.5rem; font-weight: 600; }
    .price-line small { color: var(--ink-muted); font-size: 0.875rem; margin-left: 0.25rem; }
    .form-row { margin-bottom: 0.75rem; }
    .form-row label { display: block; font-size: 0.6875rem; text-transform: uppercase; letter-spacing: 0.06em; color: var(--ink-muted); margin-bottom: 0.3rem; font-weight: 600; }
    .form-row input, .form-row select { width: 100%; padding: 0.55rem 0.75rem; border: 1px solid var(--rule); border-radius: 6px; font-size: 0.875rem; box-sizing: border-box; background: white; }
    .btn-primary { width: 100%; padding: 0.75rem; background: var(--accent); color: white; border: 0; border-radius: 6px; font-weight: 500; cursor: pointer; margin-top: 0.5rem; }
    .btn-primary:disabled { opacity: 0.5; cursor: not-allowed; }
    .quote-box { background: var(--canvas); border-radius: 8px; padding: 1rem; margin-top: 1rem; }
    .qrow { display: flex; justify-content: space-between; padding: 0.375rem 0; font-size: 0.875rem; font-variant-numeric: tabular-nums; }
    .qrow.total { border-top: 1px solid var(--rule); padding-top: 0.625rem; margin-top: 0.375rem; font-weight: 600; }
  `],
})
export class InventoryDetailComponent implements OnInit {
  private inv = inject(InventoryService);
  private cart = inject(CartService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  detail = signal<InventoryDetail | null>(null);
  quote = signal<QuoteResponse | null>(null);
  startDate = '';
  endDate = '';
  deliverable = 'reel';
  quantity = 1;

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    this.inv.detail(id).subscribe(d => {
      this.detail.set(d);
      const today = new Date();
      const next = new Date(); next.setDate(today.getDate() + 30);
      this.startDate = today.toISOString().slice(0, 10);
      this.endDate = next.toISOString().slice(0, 10);
      if (d.channel === 'Influencer' && d.influencer?.availableDeliverables?.length) {
        this.deliverable = d.influencer.availableDeliverables[0].toLowerCase();
      }
    });
  }

  back() { this.router.navigate(['/inventory']); }

  canQuote(): boolean {
    return !!this.startDate && !!this.endDate && this.startDate < this.endDate;
  }

  getQuote() {
    const d = this.detail();
    if (!d) return;
    let spec: string | undefined;
    if (d.channel === 'Influencer') {
      spec = JSON.stringify({ deliverable: this.deliverable, quantity: this.quantity });
    }
    this.inv.quote(d.id, this.startDate, this.endDate, spec).subscribe(q => this.quote.set(q));
  }

  addToCart() {
    const d = this.detail();
    const q = this.quote();
    if (!d || !q) return;

    // Build a minimal list-item view for the cart
    const listView: any = {
      id: d.id, code: d.code, channel: d.channel, name: d.name,
      basePriceMonthly: d.pricing.monthlyRate,
      basePricePerUnit: d.pricing.pricePerUnit,
      unitLabel: d.pricing.unitLabel,
      currency: d.pricing.currency,
      coverImageUrl: d.coverImageUrl,
      ratingCount: d.ratingCount,
      isAvailable: true,
      hoarding: d.hoarding ? {
        type: d.hoarding.type,
        illumination: d.hoarding.illumination,
        cityName: d.hoarding.location.cityName,
        areaName: d.hoarding.location.areaName,
        latitude: d.hoarding.location.latitude,
        longitude: d.hoarding.location.longitude,
        widthFt: d.hoarding.widthFt,
        heightFt: d.hoarding.heightFt,
      } : undefined,
      influencer: d.influencer ? {
        platform: d.influencer.platform,
        handle: d.influencer.handle,
        isPlatformVerified: d.influencer.isPlatformVerified,
        followerCount: d.influencer.followerCount,
        engagementRate: d.influencer.engagementRate,
        tier: d.influencer.tier,
        categories: d.influencer.contentCategories,
        languages: d.influencer.languages,
      } : undefined,
    };

    this.cart.add({
      inventoryUnit: listView,
      startDate: this.startDate,
      endDate: this.endDate,
      deliverableSpec: d.channel === 'Influencer'
        ? { deliverable: this.deliverable, quantity: this.quantity }
        : undefined,
      quotedPrice: q.totalAmount,
    });
    this.router.navigate(['/cart']);
  }

  parseJson(s: string | undefined | null): { key: string; value: number }[] {
    if (!s) return [];
    try {
      const obj = JSON.parse(s);
      return Object.entries(obj).map(([key, value]) => ({ key, value: Number(value) }));
    } catch { return []; }
  }

  coverUrl(d: InventoryDetail): string | null {
    if (d.coverImageUrl) return d.coverImageUrl;
    const first = d.images && d.images.length > 0 ? d.images[0] : null;
    return first?.url ?? null;
  }
}
