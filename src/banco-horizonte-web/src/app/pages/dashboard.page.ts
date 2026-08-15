import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DataService } from '../core/data.service';
import { DashboardSummary } from '../core/models';
import { AuthService } from '../core/auth.service';
import { apiErrorMessage } from '../core/api-error';

@Component({
  imports: [DatePipe, RouterLink],
  template: `
    <header class="page-head"><div><p class="section-code">TABLERO / CONTROL OPERATIVO</p><h1>Buenos días, {{ firstName }}.</h1><p>Esta es la situación de reclamos al {{ currentTime }}.</p></div><a class="action-button" routerLink="/reclamos/nuevo">＋ REGISTRAR RECLAMO</a></header>
    @if (error()) {
      <section class="feedback-panel" role="alert"><div><strong>No pudimos calcular los indicadores</strong><p>{{ error() }}</p></div><button type="button" (click)="load()">REINTENTAR</button></section>
    }
    @if (summary(); as data) {
      <section class="metric-grid" aria-label="Indicadores principales">
        <article><span>CASOS ABIERTOS</span><strong>{{ data.open }}</strong><small>{{data.resolved}} resueltos de {{data.total}} registrados</small></article>
        <article class="danger"><span>PRIORIDAD CRÍTICA</span><strong>{{ data.critical }}</strong><small><b>●</b> Requieren atención inmediata</small></article>
        <article class="warning"><span>PRÓXIMOS A VENCER</span><strong>{{ data.nearDeadline }}</strong><small><b>◷</b> Dentro del umbral SLA</small></article>
        <article><span>CUMPLIMIENTO SLA</span><strong>{{ data.slaCompliance }}<i>%</i></strong><small><b class="positive">↗ 2.4%</b> rendimiento del equipo</small></article>
      </section>

      <section class="operations-grid">
        <article class="panel attention-panel">
          <header><div><p class="section-code">ALERTA / SLA</p><h2>Atención inmediata</h2></div><a routerLink="/reclamos">VER BANDEJA →</a></header>
          <div class="case-list">
            @for (item of data.immediateAttention; track item.id) {
              <a [routerLink]="['/reclamos', item.id]" class="case-row">
                <span class="priority-marker" [class.critical]="item.priority === 'Crítica'" [class.high]="item.priority === 'Alta'"></span>
                <div><strong>{{ item.code }}</strong><small>{{ item.customer }} · {{ item.category }}</small></div>
                <span class="status-pill">{{ item.status }}</span>
                <div class="owner"><small>RESPONSABLE</small><strong>{{ item.assignee || 'Sin asignar' }}</strong></div>
                <div class="sla" [class.overdue]="item.slaState === 'Vencido'"><small>{{ item.slaState === 'Vencido' ? 'VENCIDO' : 'VENCE' }}</small><strong>{{ item.slaDeadline | date:'HH:mm' }}</strong></div>
              </a>
            }
          </div>
        </article>

        <article class="panel performance-panel">
          <header><div><p class="section-code">EQUIPO / CARGA</p><h2>Casos por analista</h2></div><span>ACTIVOS</span></header>
          <div class="analyst-list">
            @for (analyst of data.analystLoads; track analyst.analyst; let i = $index) {
              <div><span class="rank">0{{ i + 1 }}</span><strong>{{ analyst.analyst }}</strong><div class="bar"><i [style.width.%]="analyst.activeCases * 9"></i></div><b>{{ analyst.activeCases }}</b></div>
            }
          </div>
          <div class="sla-gauge"><div><span>SLA DEL MES</span><strong>{{ data.slaCompliance }}%</strong></div><div class="gauge"><i [style.width.%]="data.slaCompliance"></i></div><small>Meta operativa: 90%</small></div>
        </article>
      </section>

      <section class="lower-grid">
        <article class="panel category-panel"><header><div><p class="section-code">DISTRIBUCIÓN</p><h2>Reclamos por categoría</h2></div></header>
          <div class="category-bars">@for (slice of data.byCategory; track slice.label; let i = $index) { <div><span>{{ slice.label }}</span><div><i [style.width.%]="slice.value * 18"></i></div><b>{{ slice.value }}</b></div> }</div>
          <header><div><p class="section-code">PRIORIDAD</p><h2>Reclamos por nivel</h2></div></header>
          <div class="category-bars">@for (slice of data.byPriority; track slice.label) { <div><span>{{ slice.label }}</span><div><i [style.width.%]="slice.value * 18"></i></div><b>{{ slice.value }}</b></div> }</div>
        </article>
        <article class="panel pulse-panel"><p class="section-code">ESTADO DEL SISTEMA</p><div class="pulse-ring"><span>{{ data.overdue }}</span></div><h2>Casos fuera de SLA</h2><p>Prioriza estos casos para recuperar el nivel de cumplimiento.</p><a routerLink="/reclamos" [queryParams]="{sla:'overdue'}">REVISAR VENCIDOS →</a></article>
      </section>
    } @else if (loading()) { <div class="loading-state">Calculando indicadores operativos…</div> }
  `,
  styleUrls: ['./dashboard.page.scss', './dashboard.production.scss'],
})
export class DashboardPage {
  private readonly data = inject(DataService);
  private readonly auth = inject(AuthService);
  readonly summary = signal<DashboardSummary | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly currentTime = new Intl.DateTimeFormat('es-EC', { hour: '2-digit', minute: '2-digit' }).format(new Date());
  get firstName(): string { return this.auth.user()?.name.split(' ')[0] ?? 'equipo'; }
  constructor() { this.load(); }
  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.data.dashboard().subscribe({
      next: value => { this.summary.set(value); this.loading.set(false); },
      error: error => { this.summary.set(null); this.loading.set(false); this.error.set(apiErrorMessage(error, 'No pudimos cargar el tablero operativo.')); }
    });
  }
}
