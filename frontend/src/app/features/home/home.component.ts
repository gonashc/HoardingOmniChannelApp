import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { InventoryService } from '../../core/services/inventory.service';
import { InventoryListItem } from '../../core/models/models';
import { InrPipe } from '../../shared/pipes/inr.pipe';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, InrPipe],
  template: `
    <section class="hero">
      <div class="container">
        <h1 class="display">Plan your launch across <em>every</em> channel.</h1>
        <p class="lede">
          One platform for hoardings, creators, and tomorrow's channels.
          Search inventory, get an instant quote, and book — with a single GST invoice.
        </p>

        <div class="channel-tabs">
          <button [class.active]="channel() === ''" (click)="setChannel('')">All</button>
          <button [class.active]="channel() === 'Hoarding'" (click)="setChannel('Hoarding')">Hoardings</button>
          <button [class.active]="channel() === 'Influencer'" (click)="setChannel('Influencer')">Creators</button>
        </div>

        <form class="search" (submit)="$event.preventDefault(); search()">
          <input [(ngModel)]="q" name="q" placeholder="Search by city, area, creator handle…" />
          <button type="submit">Search</button>
        </form>
      </div>
    </section>

    <section class="trending">
      <div class="container">
        <header>
          <h2>Trending now</h2>
          <a routerLink="/inventory">Browse all →</a>
        </header>

        <div class="grid" *ngIf="!loading(); else skel">
          <a *ngFor="let u of items()" class="card" [routerLink]="['/inventory', u.id]">
            <div class="thumb" [style.background-image]="'url(' + (u.coverImageUrl || '/assets/placeholder.jpg') + ')'">
              <span class="badge">{{ u.channel }}</span>
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
              <p class="price">{{ u.basePricePerUnit | inr }} <small>/ {{ u.unitLabel }}</small></p>
            </ng-container>
          </a>
        </div>

        <ng-template #skel>
          <div class="grid">
            <div class="card skeleton" *ngFor="let _ of [1,2,3,4]"></div>
          </div>
        </ng-template>
      </div>
    </section>
  `,
  styles: [`
    .hero {
      background: linear-gradient(180deg, var(--canvas) 0%, var(--surface) 100%);
      padding: 4rem 0 3rem;
    }
    .container { max-width: 1200px; margin: 0 auto; padding: 0 1.5rem; }
    .display { font-family: var(--font-display); font-size: clamp(2.5rem, 5vw, 4rem); font-weight: 600; line-height: 1.05; letter-spacing: -0.02em; margin: 0 0 1rem; color: var(--ink); }
    .display em { font-style: italic; color: var(--accent); }
    .lede { font-size: 1.125rem; color: var(--ink-muted); max-width: 36rem; line-height: 1.6; margin-bottom: 2rem; }
    .channel-tabs { display: inline-flex; gap: 0; border: 1px solid var(--rule); border-radius: 8px; overflow: hidden; margin-bottom: 1.25rem; background: white; }
    .channel-tabs button { background: transparent; border: 0; padding: 0.625rem 1.125rem; font-size: 0.875rem; color: var(--ink-muted); cursor: pointer; font-weight: 500; }
    .channel-tabs button.active { background: var(--accent); color: white; }
    .search { display: flex; gap: 0.5rem; max-width: 560px; }
    .search input { flex: 1; padding: 0.875rem 1rem; border: 1px solid var(--rule); border-radius: 8px; font-size: 1rem; background: white; }
    .search button { padding: 0.875rem 1.5rem; background: var(--accent); color: white; border: 0; border-radius: 8px; font-weight: 500; cursor: pointer; }
    .trending { padding: 4rem 0; }
    .trending header { display: flex; justify-content: space-between; align-items: baseline; margin-bottom: 1.5rem; }
    .trending h2 { font-family: var(--font-display); font-weight: 600; font-size: 1.75rem; margin: 0; }
    .trending header a { color: var(--accent); text-decoration: none; font-size: 0.875rem; font-weight: 500; }
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 1.25rem; }
    .card { background: white; border: 1px solid var(--rule); border-radius: 12px; overflow: hidden; text-decoration: none; color: inherit; transition: transform 0.15s, box-shadow 0.15s; display: block; }
    .card:hover { transform: translateY(-2px); box-shadow: 0 8px 24px rgba(20,15,40,0.06); }
    .card .thumb { aspect-ratio: 4/3; background-size: cover; background-position: center; background-color: var(--rule); position: relative; }
    .card h3 { font-family: var(--font-display); font-size: 1.0625rem; font-weight: 600; margin: 0.875rem 1rem 0.25rem; line-height: 1.3; }
    .card .meta { font-size: 0.8125rem; color: var(--ink-muted); margin: 0 1rem 0.25rem; font-variant-numeric: tabular-nums; }
    .card .price { font-size: 1rem; font-weight: 600; color: var(--ink); margin: 0.625rem 1rem 1rem; font-variant-numeric: tabular-nums; }
    .card .price small { font-weight: 400; color: var(--ink-muted); font-size: 0.75rem; }
    .badge { position: absolute; top: 0.75rem; left: 0.75rem; background: rgba(255,255,255,0.92); backdrop-filter: blur(6px); padding: 0.25rem 0.625rem; border-radius: 999px; font-size: 0.6875rem; font-weight: 600; color: var(--ink); text-transform: uppercase; letter-spacing: 0.04em; }
    .card.skeleton { aspect-ratio: 4/5; background: var(--rule); }
  `],
})
export class HomeComponent implements OnInit {
  private inv = inject(InventoryService);
  private router = inject(Router);

  items = signal<InventoryListItem[]>([]);
  loading = signal(true);
  channel = signal<string>('');
  q = '';

  ngOnInit() {
    this.loadTrending();
  }

  setChannel(c: string) {
    this.channel.set(c);
    this.loadTrending();
  }

  loadTrending() {
    this.loading.set(true);
    this.inv.trending(this.channel() || undefined, undefined, 8).subscribe({
      next: (items) => { this.items.set(items); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  search() {
    const params: Record<string, string> = {};
    if (this.q) params['query'] = this.q;
    if (this.channel()) params['channel'] = this.channel();
    this.router.navigate(['/inventory'], { queryParams: params });
  }

  formatFollowers(n: number): string {
    if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M followers`;
    if (n >= 1_000) return `${(n / 1_000).toFixed(0)}K followers`;
    return `${n} followers`;
  }
}
