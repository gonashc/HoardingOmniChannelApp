import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section class="container-page py-16 lg:py-20">
      <div class="max-w-lg mx-auto">
        <div class="eyebrow mb-2">Create account</div>
        <h1 class="font-display text-4xl font-bold tracking-tight mb-2">Get started.</h1>
        <p class="text-ink-500 text-sm mb-10">Three minutes. Then you can plan, quote, and book without phone-tag.</p>

        <!-- Role toggle -->
        <div class="flex gap-px bg-ink-100 p-px rounded mb-8 max-w-sm">
          @for (r of roles; track r.value) {
            <button type="button" (click)="role.set(r.value)"
                    [class.bg-surface]="role() === r.value"
                    [class.shadow-edge]="role() === r.value"
                    [class.text-ink-900]="role() === r.value"
                    [class.text-ink-500]="role() !== r.value"
                    class="flex-1 px-4 py-2 text-xs font-medium rounded transition-colors">
              {{ r.label }}
            </button>
          }
        </div>

        <form (ngSubmit)="register()" class="space-y-5">
          <div class="grid sm:grid-cols-2 gap-4">
            <div>
              <label class="field-label">Full name</label>
              <input class="input" [(ngModel)]="fullName" name="fullName" required minlength="2">
            </div>
            <div>
              <label class="field-label">Phone</label>
              <input class="input font-mono" type="tel" [(ngModel)]="phone" name="phone" required
                     placeholder="+91 98765 43210" pattern="[+]?[0-9 ]{10,15}">
            </div>
          </div>
          <div>
            <label class="field-label">Email</label>
            <input class="input" type="email" [(ngModel)]="email" name="email" required>
          </div>
          <div>
            <label class="field-label">Password</label>
            <input class="input" type="password" [(ngModel)]="password" name="password" required minlength="8"
                   placeholder="At least 8 characters with uppercase, lowercase and a number">
          </div>

          @if (role() !== 'Advertiser') {
            <div class="grid sm:grid-cols-2 gap-4">
              <div>
                <label class="field-label">Company name</label>
                <input class="input" [(ngModel)]="companyName" name="companyName">
              </div>
              <div>
                <label class="field-label">GSTIN <span class="text-ink-400">(optional)</span></label>
                <input class="input font-mono" [(ngModel)]="gstin" name="gstin"
                       placeholder="22AAAAA0000A1Z5">
              </div>
            </div>
          }

          @if (error()) {
            <div class="bg-signal-negative-50 border border-signal-negative-500/20 text-signal-negative-500 text-xs px-3 py-2 rounded">
              {{ error() }}
            </div>
          }

          <button type="submit" [disabled]="loading()" class="btn-primary w-full py-3.5">
            {{ loading() ? 'Creating account…' : 'Create account' }}
          </button>

          <p class="text-2xs text-ink-400 text-center">
            By creating an account you agree to Hoardly's terms of use and privacy policy.
          </p>
        </form>

        <div class="text-center text-sm text-ink-500 mt-8">
          Already have an account? <a routerLink="/login" class="text-ink-900 underline underline-offset-4">Sign in</a>
        </div>
      </div>
    </section>
  `
})
export class RegisterComponent {
  private auth = inject(AuthService);
  private router = inject(Router);

  protected fullName = '';
  protected phone = '';
  protected email = '';
  protected password = '';
  protected companyName = '';
  protected gstin = '';
  protected readonly role = signal<'Advertiser' | 'MediaOwner' | 'MediaPlanner'>('Advertiser');
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly roles = [
    { value: 'Advertiser' as const,   label: 'Advertiser' },
    { value: 'MediaPlanner' as const, label: 'Agency / planner' },
    { value: 'MediaOwner' as const,   label: 'Media owner' }
  ];

  register(): void {
    this.error.set(null);
    this.loading.set(true);
    this.auth.register({
      email: this.email,
      phone: this.phone,
      password: this.password,
      fullName: this.fullName,
      role: this.role(),
      companyName: this.companyName || undefined,
      gstin: this.gstin || undefined
    }).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: err => {
        this.loading.set(false);
        this.error.set(err?.error?.error ?? 'Registration failed.');
      }
    });
  }
}
