import { HttpClient, HttpHeaders } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { CurrentUser } from './models';

interface SupabaseSession { access_token: string; refresh_token: string; expires_in: number; expires_at?: number; user: { id: string; email: string }; }

export type AuthFailureStage = 'sign-in' | 'profile';

export class AuthFlowError extends Error {
  constructor(readonly stage: AuthFailureStage, readonly source: unknown) {
    super(stage === 'sign-in' ? 'No se pudo validar la cuenta.' : 'No se pudo cargar el perfil.');
  }
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly sessionKey = 'bh_session';
  private readonly demoUser: CurrentUser = { id: '00000000-0000-0000-0000-000000000001', name: 'María Andrade', email: 'supervisor@bancohorizonte.demo', roles: ['Supervisor'] };
  readonly user = signal<CurrentUser | null>(environment.demoMode ? this.demoUser : null);
  readonly isAuthenticated = computed(() => this.user() !== null || !!this.accessToken);
  readonly isDemo = environment.demoMode;
  private readonly initialization: Promise<void>;
  private refreshInFlight: Promise<void> | null = null;

  constructor() {
    this.initialization = !environment.demoMode && this.accessToken
      ? this.refreshSession().then(() => this.loadProfile()).catch(() => this.logout())
      : Promise.resolve();
  }

  get accessToken(): string | null {
    const raw = localStorage.getItem(this.sessionKey);
    if (!raw) return null;
    try { return (JSON.parse(raw) as SupabaseSession).access_token; } catch { return null; }
  }

  async login(email: string, password: string): Promise<void> {
    if (environment.demoMode) { this.user.set(this.demoUser); await this.navigateHome(); return; }
    this.ensureConfigured();
    let session: SupabaseSession;
    try {
      session = await firstValueFrom(this.http.post<SupabaseSession>(
        `${environment.supabaseUrl}/auth/v1/token?grant_type=password`,
        { email: email.trim().toLowerCase(), password },
        { headers: this.supabaseHeaders() }));
    } catch (error) {
      throw new AuthFlowError('sign-in', error);
    }
    this.saveSession(session);
    try {
      await this.loadProfile();
    } catch (error) {
      this.clearSession();
      throw new AuthFlowError('profile', error);
    }
    await this.navigateHome();
  }

  async signUp(firstNames: string, lastNames: string, email: string, password: string): Promise<void> {
    this.ensureConfigured();
    const normalizedEmail = email.trim().toLowerCase();
    const status = await firstValueFrom(this.http.post<{ registered: boolean }>(
      `${environment.apiUrl}/auth/registration-status`, { email: normalizedEmail }));
    if (status.registered) throw new Error('Este correo ya está registrado. Inicia sesión con tu cuenta existente.');
    await firstValueFrom(this.http.post(`${environment.supabaseUrl}/auth/v1/signup`,
      { email: normalizedEmail, password, data: { first_names: firstNames, last_names: lastNames } }, { headers: this.supabaseHeaders() }));
  }

  async loadProfile(): Promise<void> {
    const profile = await firstValueFrom(this.http.get<CurrentUser>(`${environment.apiUrl}/auth/me`));
    this.user.set(profile);
  }

  whenReady(): Promise<void> { return this.initialization; }

  refreshSession(force = false): Promise<void> {
    const raw = localStorage.getItem(this.sessionKey); if (!raw) return Promise.resolve();
    const current = JSON.parse(raw) as SupabaseSession;
    if (!force && (current.expires_at ?? 0) > Math.floor(Date.now() / 1000) + 60) return Promise.resolve();
    if (this.refreshInFlight) return this.refreshInFlight;
    this.refreshInFlight = firstValueFrom(this.http.post<SupabaseSession>(
        `${environment.supabaseUrl}/auth/v1/token?grant_type=refresh_token`,
        { refresh_token: current.refresh_token }, { headers: this.supabaseHeaders() }))
      .then(session => this.saveSession(session))
      .finally(() => { this.refreshInFlight = null; });
    return this.refreshInFlight;
  }

  logout(): void { this.clearSession(); void this.router.navigate(['/login']); }
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
  private clearSession(): void {
    localStorage.removeItem(this.sessionKey);
    this.user.set(null);
  }
}
