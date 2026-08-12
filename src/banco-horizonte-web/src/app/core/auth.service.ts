import { HttpClient, HttpHeaders } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { CurrentUser } from './models';

interface SupabaseSession { access_token: string; refresh_token: string; expires_in: number; expires_at?: number; user: { id: string; email: string }; }

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly sessionKey = 'bh_session';
  private readonly demoUser: CurrentUser = { id: '00000000-0000-0000-0000-000000000001', name: 'María Andrade', email: 'supervisor@bancohorizonte.demo', roles: ['Supervisor'] };
  readonly user = signal<CurrentUser | null>(environment.demoMode ? this.demoUser : null);
  readonly isAuthenticated = computed(() => this.user() !== null || !!this.accessToken);
  readonly isDemo = environment.demoMode;

  constructor() {
    if (!environment.demoMode && this.accessToken) {
      void this.refreshSession().then(() => this.loadProfile()).catch(() => this.logout());
    }
  }

  get accessToken(): string | null {
    const raw = localStorage.getItem(this.sessionKey);
    if (!raw) return null;
    try { return (JSON.parse(raw) as SupabaseSession).access_token; } catch { return null; }
  }

  async login(email: string, password: string): Promise<void> {
    if (environment.demoMode) { this.user.set(this.demoUser); await this.navigateHome(); return; }
    this.ensureConfigured();
    const session = await firstValueFrom(this.http.post<SupabaseSession>(
      `${environment.supabaseUrl}/auth/v1/token?grant_type=password`, { email, password }, { headers: this.supabaseHeaders() }));
    this.saveSession(session);
    await this.loadProfile();
    await this.navigateHome();
  }

  async signUp(firstNames: string, lastNames: string, email: string, password: string): Promise<void> {
    this.ensureConfigured();
    await firstValueFrom(this.http.post(`${environment.supabaseUrl}/auth/v1/signup`,
      { email, password, data: { first_names: firstNames, last_names: lastNames } }, { headers: this.supabaseHeaders() }));
  }

  async loadProfile(): Promise<void> {
    const profile = await firstValueFrom(this.http.get<CurrentUser>(`${environment.apiUrl}/auth/me`));
    this.user.set(profile);
  }

  async refreshSession(): Promise<void> {
    const raw = localStorage.getItem(this.sessionKey); if (!raw) return;
    const current = JSON.parse(raw) as SupabaseSession;
    if ((current.expires_at ?? 0) > Math.floor(Date.now() / 1000) + 60) return;
    const session = await firstValueFrom(this.http.post<SupabaseSession>(
      `${environment.supabaseUrl}/auth/v1/token?grant_type=refresh_token`,
      { refresh_token: current.refresh_token }, { headers: this.supabaseHeaders() }));
    this.saveSession(session);
  }

  logout(): void { localStorage.removeItem(this.sessionKey); this.user.set(null); void this.router.navigate(['/login']); }
  hasAnyRole(...roles: string[]): boolean { return this.user()?.roles.some(role => roles.includes(role)) ?? false; }
  async navigateHome(): Promise<void> {
    await this.router.navigate([this.hasAnyRole('Supervisor', 'Administrador') ? '/dashboard' : '/reclamos']);
  }

  private supabaseHeaders(): HttpHeaders { return new HttpHeaders({ apikey: environment.supabasePublishableKey, 'Content-Type': 'application/json' }); }
  private ensureConfigured(): void {
    if (!environment.supabaseUrl || !environment.supabasePublishableKey) throw new Error('Configura Supabase URL y publishable key en environment.ts.');
  }
  private saveSession(session: SupabaseSession): void {
    session.expires_at = Math.floor(Date.now() / 1000) + session.expires_in;
    localStorage.setItem(this.sessionKey, JSON.stringify(session));
  }
}
