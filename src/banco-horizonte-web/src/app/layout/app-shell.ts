import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <a class="skip-link" href="#main-content">Saltar al contenido</a>
    <div class="shell" [class.nav-open]="navOpen()">
      <aside class="sidebar" aria-label="Navegación principal">
        <a class="brand" [routerLink]="homeLink" aria-label="Banco Horizonte, inicio">
          <span class="brand-mark" aria-hidden="true">BH</span>
          <span><strong>Banco Horizonte</strong><small>Centro de reclamos</small></span>
        </a>
        <div class="ops-label"><span></span> OPERACIONES / 01</div>
        <nav>
          @if (auth.hasAnyRole('Supervisor', 'Administrador')) {
            <a routerLink="/dashboard" routerLinkActive="active"><i aria-hidden="true">⌁</i> Tablero</a>
          }
          <a routerLink="/reclamos" routerLinkActive="active" [routerLinkActiveOptions]="{exact:true}"><i aria-hidden="true">▤</i> Reclamos</a>
          @if (auth.hasAnyRole('Operador', 'Supervisor', 'Administrador')) {
            <a routerLink="/reclamos/nuevo" routerLinkActive="active"><i aria-hidden="true">＋</i> Registrar caso</a>
          }
          @if (auth.hasAnyRole('Supervisor', 'Administrador')) {
            <a routerLink="/configuracion" routerLinkActive="active"><i aria-hidden="true">◇</i> Configuración</a>
          }
        </nav>
        <div class="sidebar-foot">
          <div class="system-state"><span></span><div><strong>Sistema operativo</strong><small>API + monitoreo SLA</small></div></div>
          <div class="user-card">
            <div class="avatar">{{ initials }}</div>
            <div><strong>{{ auth.user()?.name }}</strong><small>{{ auth.user()?.roles?.[0] }}</small></div>
            <button type="button" (click)="auth.logout()" aria-label="Cerrar sesión" title="Cerrar sesión">↗</button>
          </div>
        </div>
      </aside>
      <div class="workspace">
        <header class="topbar">
          <button class="menu-button" type="button" (click)="navOpen.set(!navOpen())" aria-label="Abrir menú">☰</button>
          <p><span class="live-dot"></span> Monitoreo en tiempo real</p>
          <div class="top-actions"><span>{{ today }}</span><span class="demo-chip">{{ auth.isDemo ? 'MODO DEMO' : 'CONECTADO' }}</span></div>
        </header>
        <main id="main-content"><router-outlet /></main>
      </div>
      @if (navOpen()) { <button class="scrim" aria-label="Cerrar menú" (click)="navOpen.set(false)"></button> }
    </div>
  `,
  styleUrl: './app-shell.scss',
})
export class AppShell {
  readonly auth = inject(AuthService);
  readonly navOpen = signal(false);
  readonly today = new Intl.DateTimeFormat('es-EC', { day: '2-digit', month: 'short', year: 'numeric' }).format(new Date()).toUpperCase();
  get homeLink(): string { return this.auth.hasAnyRole('Supervisor', 'Administrador') ? '/dashboard' : '/reclamos'; }
  get initials(): string { return (this.auth.user()?.name ?? 'BH').split(' ').slice(0, 2).map(x => x[0]).join(''); }
}
