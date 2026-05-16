import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResult, RegisterPayload, User } from '../models/models';

const STORAGE_KEY = 'hoardly.auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  // Backend returns a flat AuthResult: { userId, email, fullName, role, token }.
  private readonly authState = signal<AuthResult | null>(this.loadFromStorage());

  /** Derived: the signed-in user, or null. */
  readonly user = computed<User | null>(() => {
    const s = this.authState();
    if (!s) return null;
    return {
      id: s.userId,
      email: s.email,
      fullName: s.fullName,
      role: s.role,
    };
  });

  readonly isAuthenticated = computed(() => this.authState() !== null);
  readonly token = computed(() => this.authState()?.token ?? null);

  readonly userInitials = computed(() => {
    const u = this.user();
    if (!u) return '';
    return u.fullName.split(' ').map(s => s[0]).filter(Boolean).slice(0, 2).join('').toUpperCase();
  });

  login(emailOrPhone: string, password: string): Observable<AuthResult> {
    return this.http
      .post<AuthResult>(`${environment.apiBaseUrl}/auth/login`, { emailOrPhone, password })
      .pipe(tap(r => this.persist(r)));
  }

  register(payload: RegisterPayload): Observable<AuthResult> {
    return this.http
      .post<AuthResult>(`${environment.apiBaseUrl}/auth/register`, payload)
      .pipe(tap(r => this.persist(r)));
  }

  logout(): void {
    if (typeof localStorage !== 'undefined') localStorage.removeItem(STORAGE_KEY);
    this.authState.set(null);
    this.router.navigate(['/']);
  }

  private persist(r: AuthResult): void {
    if (typeof localStorage !== 'undefined') localStorage.setItem(STORAGE_KEY, JSON.stringify(r));
    this.authState.set(r);
  }

  private loadFromStorage(): AuthResult | null {
    try {
      const raw = typeof localStorage !== 'undefined' ? localStorage.getItem(STORAGE_KEY) : null;
      return raw ? (JSON.parse(raw) as AuthResult) : null;
    } catch {
      return null;
    }
  }
}
