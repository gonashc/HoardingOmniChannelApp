import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section class="container-page py-16 lg:py-24">
      <div class="max-w-md mx-auto">
        <div class="eyebrow mb-2">Sign in</div>
        <h1 class="font-display text-4xl font-bold tracking-tight mb-2">Welcome back.</h1>
        <p class="text-ink-500 text-sm mb-10">Pick up where you left off — your cart and campaigns are waiting.</p>

        <form (ngSubmit)="login()" class="space-y-5">
          <div>
            <label class="field-label">Email or phone</label>
            <input class="input" [(ngModel)]="emailOrPhone" name="emailOrPhone" required autofocus
                   placeholder="you@company.com or +91 98765 43210">
          </div>
          <div>
            <label class="field-label flex justify-between">
              <span>Password</span>
              <a class="text-2xs uppercase tracking-widest text-ink-500 hover:text-ink-900 normal-case tracking-normal cursor-pointer">Forgot?</a>
            </label>
            <input class="input" type="password" [(ngModel)]="password" name="password" required minlength="8">
          </div>

          @if (error()) {
            <div class="bg-signal-negative-50 border border-signal-negative-500/20 text-signal-negative-500 text-xs px-3 py-2 rounded">
              {{ error() }}
            </div>
          }

          <button type="submit" [disabled]="loading()" class="btn-primary w-full py-3.5">
            {{ loading() ? 'Signing in…' : 'Sign in' }}
          </button>
        </form>

        <div class="text-center text-sm text-ink-500 mt-8">
          New to Hoardly? <a routerLink="/register" class="text-ink-900 underline underline-offset-4">Create an account</a>
        </div>
      </div>
    </section>
  `
})
export class LoginComponent {
  private auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  protected emailOrPhone = '';
  protected password = '';
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  login(): void {
    this.error.set(null);
    this.loading.set(true);
    this.auth.login(this.emailOrPhone, this.password).subscribe({
      next: () => {
        this.loading.set(false);
        const returnUrl = this.route.snapshot.queryParams['returnUrl'] ?? '/dashboard';
        this.router.navigateByUrl(returnUrl);
      },
      error: err => {
        this.loading.set(false);
        this.error.set(err?.error?.error ?? 'Invalid credentials.');
      }
    });
  }
}
