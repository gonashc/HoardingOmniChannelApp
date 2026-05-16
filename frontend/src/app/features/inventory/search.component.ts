import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { InventoryService } from '../../core/services/inventory.service';
import { InventoryListItem, InventorySearchCriteria, PagedResult } from '../../core/models/models';
import { InrPipe } from '../../shared/pipes/inr.pipe';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, InrPipe],
  template: `
    <div class="container">
      <div class="channel-tabs">
        <button [class.active]="channel === ''" (click)="setChannel('')">All</button>
        <button [class.active]="channel === 'Hoarding'" (click)="setChannel('Hoarding')">Hoardings</button>
        <button [class.active]="channel === 'Influencer'" (click)="setChannel('Influencer')">Creators</button>
      </div>

      <div class="layout">
        <aside class="filters">
          <h3>Refine</h3>

          <div class="group">
            <label>Search</label>
            <input [(ngModel)]="filters.query" placeholder="city, area, handle…" (keyup.enter)="apply()" />
          </div>

          <div class="group">
            <label>Price (₹)</label>
            <div class="row">
              <input type="number" [(ngModel)]="filters.minPrice" placeholder="Min" />
              <input type="number" [(ngModel)]="filters.maxPrice" placeholder="Max" />
            </div>
          </div>

          <!-- HOARDING-specific filters -->
          <ng-container *ngIf="channel === 'Hoarding' || channel === ''">
            <div class="group">
              <label>Hoarding type</label>
              <select [(ngModel)]="hoardingTypeSingle">
                <option value="">Any</option>
                <option value="billboard">Billboard</option>
                <option value="unipole">Unipole</option>
                <option value="gantry">Gantry</option>
                <option value="dooh">Digital (DOOH)</option>
                <option value="bus_shelter">Bus Shelter</option>
                <option value="metro_pillar">Metro Pillar</option>
              </select>
            </div>
            <div class="group">
              <label>Min daily traffic</label>
              <input type="number" [(ngModel)]="filters.minTraffic" placeholder="50000" />
            </div>
            <div class="group">
              <label>Illumination</label>
              <select [(ngModel)]="filters.illuminationType">
                <option value="">Any</option>
                <option value="frontlit">Frontlit</option>
                <option value="backlit">Backlit</option>
                <option value="digital">Digital</option>
                <option value="non_lit">Non-lit</option>
              </select>
            </div>
          </ng-container>

          <!-- INFLUENCER-specific filters -->
          <ng-container *ngIf="channel === 'Influencer' || channel === ''">
            <div class="group">
              <label>Platform</label>
              <select [(ngModel)]="platformSingle">
                <option value="">Any</option>
                <option value="instagram">Instagram</option>
                <option value="youtube">YouTube</option>
                <option value="twitter_x">X (Twitter)</option>
                <option value="linkedin">LinkedIn</option>
                <option value="tiktok">TikTok</option>
              </select>
            </div>
            <div class="group">
              <label>Tier</label>
              <select [(ngModel)]="tierSingle">
                <option value="">Any</option>
                <option value="nano">Nano (&lt;10K)</option>
                <option value="micro">Micro (10K–100K)</option>
                <option value="mid">Mid (100K–500K)</option>
                <option value="macro">Macro (500K–1M)</option>
                <option value="mega">Mega (1M–10M)</option>
                <option value="celebrity">Celebrity (10M+)</option>
              </select>
            </div>
            <div class="group">
              <label>Followers</label>
              <div class="row">
                <input type="number" [(ngModel)]="filters.minFollowers" placeholder="Min" />
                <input type="number" [(ngModel)]="filters.maxFollowers" placeholder="Max" />
              </div>
            </div>
            <div class="group">
              <label>Min engagement %</label>
              <input type="number" step="0.1" [(ngModel)]="filters.minEngagementRate" placeholder="3.5" />
            </div>
            <div class="group">
              <label>Category</label>
              <select [(ngModel)]="categorySingle">
                <option value="">Any</option>
                <option value="food">Food</option>
                <option value="travel">Travel</option>
                <option value="technology">Technology</option>
                <option value="beauty">Beauty</option>
                <option value="fashion">Fashion</option>
                <option value="lifestyle">Lifestyle</option>
                <option value="fitness">Fitness</option>
                <option value="finance">Finance</option>
              </select>
            </div>
            <div class="group">
              <label class="check">
                <input type="checkbox" [(ngModel)]="filters.platformVerifiedOnly" />
                Verified accounts only
              </label>
            </div>
          </ng-container>

          <div class="group">
            <label>Sort by</label>
            <select [(ngModel)]="filters.sortBy">
              <option value="popularity">Most popular</option>
              <option value="price_asc">Price (low to high)</option>
              <option value="price_desc">Price (high to low)</option>
              <option value="reach">Highest reach</option>
              <option value="newest">Newest</option>
            </select>
          </div>

          <button class="apply-btn" (click)="apply()">Apply filters</button>
        </aside>

        <section class="results">
          <header>
            <p class="count" *ngIf="result()">{{ result()?.total }} results</p>
          </header>

          <div class="grid" *ngIf="!loading(); else skel">
            <a *ngFor="let u of result()?.items" class="card" [routerLink]="['/inventory', u.id]">
              <div class="thumb" [style.background-image]="'url(' + (u.coverImageUrl || '/assets/placeholder.jpg') + ')'">
                <span class="badge">{{ u.channel }}</span>
                <span class="badge verify" *ngIf="u.influencer?.isPlatformVerified">✓</span>
              </div>
              <h3>{{ u.name }}</h3>

              <ng-container *ngIf="u.hoarding as h">
                <p class="meta">{{ h.areaName }}, {{ h.cityName }}</p>
                <p class="meta">{{ h.widthFt }}×{{ h.heightFt }} ft · {{ h.illumination }}</p>
                <p class="price">{{ u.basePriceMonthly | inr }} <small>/ month</small></p>
              </ng-container>

              <ng-container *ngIf="u.influencer as inf">
                <p class="meta">{{ '@' + inf.handle }} · {{ inf.platform }}</p>
                <p class="meta">{{ formatFollowers(inf.followerCount) }} · {{ inf.engagementRate }}% ER</p>
                <p class="meta tags" *ngIf="inf.categories as cats">
                  <span class="tag" *ngFor="let c of cats.slice(0,3)">{{ c }}</span>
                </p>
                <p class="price">{{ u.basePricePerUnit | inr }} <small>/ {{ u.unitLabel }}</small></p>
              </ng-container>
            </a>
          </div>

          <ng-template #skel>
            <div class="grid"><div class="card skeleton" *ngFor="let _ of [1,2,3,4,5,6]"></div></div>
          </ng-template>
        </section>
      </div>
    </div>
  `,
  styles: [`
    .container { max-width: 1280px; margin: 0 auto; padding: 2rem 1.5rem; }
    .channel-tabs { display: inline-flex; border: 1px solid var(--rule); border-radius: 8px; overflow: hidden; margin-bottom: 1.5rem; background: white; }
    .channel-tabs button { background: transparent; border: 0; padding: 0.625rem 1.125rem; font-size: 0.875rem; color: var(--ink-muted); cursor: pointer; font-weight: 500; }
    .channel-tabs button.active { background: var(--accent); color: white; }
    .layout { display: grid; grid-template-columns: 280px 1fr; gap: 2rem; }
    @media (max-width: 768px) { .layout { grid-template-columns: 1fr; } }
    .filters { background: white; border: 1px solid var(--rule); border-radius: 12px; padding: 1.5rem; height: fit-content; position: sticky; top: 1rem; }
    .filters h3 { font-family: var(--font-display); font-size: 1.125rem; font-weight: 600; margin: 0 0 1.25rem; }
    .group { margin-bottom: 1.125rem; }
    .group label { display: block; font-size: 0.75rem; font-weight: 600; color: var(--ink-muted); text-transform: uppercase; letter-spacing: 0.04em; margin-bottom: 0.4rem; }
    .group label.check { text-transform: none; letter-spacing: 0; font-weight: 400; color: var(--ink); display: flex; gap: 0.5rem; align-items: center; }
    .group input, .group select { width: 100%; padding: 0.55rem 0.75rem; border: 1px solid var(--rule); border-radius: 6px; font-size: 0.875rem; box-sizing: border-box; background: white; }
    .row { display: flex; gap: 0.5rem; }
    .row input { flex: 1; }
    .apply-btn { width: 100%; padding: 0.75rem; background: var(--accent); color: white; border: 0; border-radius: 6px; font-weight: 500; cursor: pointer; margin-top: 0.5rem; }
    .results header { margin-bottom: 1rem; }
    .count { color: var(--ink-muted); font-size: 0.875rem; font-variant-numeric: tabular-nums; margin: 0; }
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 1.25rem; }
    .card { background: white; border: 1px solid var(--rule); border-radius: 12px; overflow: hidden; text-decoration: none; color: inherit; transition: transform 0.15s, box-shadow 0.15s; display: block; }
    .card:hover { transform: translateY(-2px); box-shadow: 0 8px 24px rgba(20,15,40,0.06); }
    .card .thumb { aspect-ratio: 4/3; background-size: cover; background-position: center; background-color: var(--rule); position: relative; }
    .card h3 { font-family: var(--font-display); font-size: 1.0625rem; font-weight: 600; margin: 0.875rem 1rem 0.25rem; line-height: 1.3; }
    .card .meta { font-size: 0.8125rem; color: var(--ink-muted); margin: 0 1rem 0.25rem; font-variant-numeric: tabular-nums; }
    .card .price { font-size: 1rem; font-weight: 600; color: var(--ink); margin: 0.625rem 1rem 1rem; font-variant-numeric: tabular-nums; }
    .card .price small { font-weight: 400; color: var(--ink-muted); font-size: 0.75rem; }
    .badge { position: absolute; top: 0.75rem; left: 0.75rem; background: rgba(255,255,255,0.92); backdrop-filter: blur(6px); padding: 0.25rem 0.625rem; border-radius: 999px; font-size: 0.6875rem; font-weight: 600; color: var(--ink); text-transform: uppercase; letter-spacing: 0.04em; }
    .badge.verify { left: auto; right: 0.75rem; background: var(--accent); color: white; }
    .tags { display: flex; gap: 0.25rem; flex-wrap: wrap; }
    .tag { background: var(--canvas); border: 1px solid var(--rule); border-radius: 999px; padding: 0.125rem 0.5rem; font-size: 0.6875rem; }
    .card.skeleton { aspect-ratio: 4/5; background: var(--rule); }
  `],
})
export class SearchComponent implements OnInit {
  private inv = inject(InventoryService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  channel = '';
  hoardingTypeSingle = '';
  platformSingle = '';
  tierSingle = '';
  categorySingle = '';
  filters: InventorySearchCriteria = { sortBy: 'popularity', page: 1, pageSize: 24 };

  result = signal<PagedResult<InventoryListItem> | null>(null);
  loading = signal(true);

  ngOnInit() {
    this.route.queryParamMap.subscribe(p => {
      this.channel = p.get('channel') ?? '';
      this.filters.query = p.get('query') ?? undefined;
      this.filters.cityId = p.get('cityId') ? Number(p.get('cityId')) : undefined;
      this.apply();
    });
  }

  setChannel(c: string) {
    this.channel = c;
    this.apply();
  }

  apply() {
    this.loading.set(true);
    const c: InventorySearchCriteria = { ...this.filters };
    if (this.channel) c.channel = this.channel;
    if (this.hoardingTypeSingle) c.hoardingTypes = [this.hoardingTypeSingle];
    if (this.platformSingle) c.platforms = [this.platformSingle];
    if (this.tierSingle) c.tiers = [this.tierSingle];
    if (this.categorySingle) c.categories = [this.categorySingle];

    this.inv.search(c).subscribe({
      next: r => { this.result.set(r); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  formatFollowers(n: number): string {
    if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
    if (n >= 1_000) return `${(n / 1_000).toFixed(0)}K`;
    return `${n}`;
  }
}
