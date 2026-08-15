import { DatePipe } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, merge } from 'rxjs';
import { apiErrorMessage } from '../core/api-error';
import { AuthService } from '../core/auth.service';
import { DataService } from '../core/data.service';
import { CatalogResponse, ComplaintListItem } from '../core/models';

@Component({
  imports: [DatePipe, RouterLink, ReactiveFormsModule],
  template: `
    <header class="page-head">
      <div><p class="section-code">OPERACIÓN / BANDEJA</p><h1>Reclamos</h1><p>{{ total() }} casos encontrados · ordenados por riesgo SLA</p></div>
      @if(auth.hasAnyRole('Operador','Supervisor','Administrador')){<a class="action-button" routerLink="/reclamos/nuevo">＋ NUEVO RECLAMO</a>}
    </header>
    <section class="filter-bar" aria-label="Filtros de reclamos">
      <label><span>⌕</span><input [formControl]="search" placeholder="Buscar código, cliente o cédula" aria-label="Buscar reclamos" /></label>
      <button type="button" [class.active]="sla() === ''" (click)="setSla('')">TODOS</button>
      <button type="button" [class.active]="sla() === 'near'" (click)="setSla('near')">PRÓXIMOS</button>
      <button type="button" [class.active]="sla() === 'overdue'" (click)="setSla('overdue')">VENCIDOS</button>
    </section>
    <section class="filter-bar" aria-label="Filtros detallados">
      <select [formControl]="statusId"><option [ngValue]="null">Todos los estados</option>@for(item of catalogs()?.statuses;track item.id){<option [ngValue]="item.id">{{item.name}}</option>}</select>
      <select [formControl]="priorityId"><option [ngValue]="null">Todas las prioridades</option>@for(item of catalogs()?.priorities;track item.id){<option [ngValue]="item.id">{{item.name}}</option>}</select>
      <select [formControl]="categoryId"><option [ngValue]="null">Todas las categorías</option>@for(item of catalogs()?.categories;track item.id){<option [ngValue]="item.id">{{item.name}}</option>}</select>
      <select [formControl]="channelId"><option [ngValue]="null">Todos los canales</option>@for(item of catalogs()?.channels;track item.id){<option [ngValue]="item.id">{{item.name}}</option>}</select>
      <select [formControl]="assigneeId"><option value="">Todos los responsables</option>@for(item of catalogs()?.analysts;track item.id){<option [value]="item.id">{{item.name}}</option>}</select>
    </section>

    @if (error()) {
      <section class="feedback-panel" role="alert"><div><strong>No pudimos cargar la bandeja</strong><p>{{ error() }}</p></div><button type="button" (click)="load()">REINTENTAR</button></section>
    }
    <section class="table-panel" [attr.aria-busy]="loading()">
      <div class="table-head"><span>CASO / CLIENTE</span><span>CATEGORÍA</span><span>PRIORIDAD</span><span>ESTADO</span><span>RESPONSABLE</span><span>SLA</span></div>
      @if (loading()) {
        <div class="loading-state">Actualizando bandeja…</div>
      } @else {
        @for (item of items(); track item.id) {
          <a class="table-row" [routerLink]="['/reclamos', item.id]">
            <div><strong>{{ item.code }}</strong><small>{{ item.customer }}</small></div><span>{{ item.category }}</span>
            <span class="priority" [attr.data-priority]="item.priority">{{ item.priority }}</span><span class="state">{{ item.status }}</span>
            <span>{{ item.assignee || 'Sin asignar' }}</span><div class="sla-cell" [attr.data-state]="item.slaState"><strong>{{ item.slaState }}</strong><small>{{ item.slaDeadline | date:'dd MMM · HH:mm' }}</small></div>
          </a>
        } @empty { <div class="empty-state"><strong>Sin coincidencias</strong><p>Ajusta los filtros o registra un nuevo reclamo.</p></div> }
      }
    </section>
    @if (!loading() && total() > 0) {
      <nav class="pagination" aria-label="Paginación de reclamos">
        <button type="button" (click)="changePage(page() - 1)" [disabled]="page() === 1">← ANTERIOR</button>
        <span>PÁGINA {{ page() }} DE {{ pageCount() }} <small>{{ rangeLabel() }}</small></span>
        <button type="button" (click)="changePage(page() + 1)" [disabled]="page() === pageCount()">SIGUIENTE →</button>
      </nav>
    }
  `,
  styleUrls: ['./complaints.page.scss', './complaints.production.scss'],
})
export class ComplaintsPage {
  private readonly data = inject(DataService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly pageSize = 20;
  private requestId = 0;
  readonly auth = inject(AuthService);
  readonly items = signal<ComplaintListItem[]>([]);
  readonly catalogs = signal<CatalogResponse | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly page = signal(1);
  readonly total = signal(0);
  readonly pageCount = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));
  readonly rangeLabel = computed(() => {
    const first = (this.page() - 1) * this.pageSize + 1;
    const last = Math.min(this.page() * this.pageSize, this.total());
    return `${first}–${last} de ${this.total()}`;
  });
  readonly search = new FormControl('', { nonNullable: true });
  readonly sla = signal(this.route.snapshot.queryParamMap.get('sla') ?? '');
  readonly statusId = new FormControl<number | null>(null);
  readonly priorityId = new FormControl<number | null>(null);
  readonly categoryId = new FormControl<number | null>(null);
  readonly channelId = new FormControl<number | null>(null);
  readonly assigneeId = new FormControl('', { nonNullable: true });

  constructor() {
    this.data.catalogs().subscribe({
      next: value => this.catalogs.set(value),
      error: error => this.error.set(apiErrorMessage(error, 'No pudimos cargar los filtros disponibles.'))
    });
    this.load();
    this.search.valueChanges.pipe(debounceTime(250), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.resetAndLoad());
    merge(this.statusId.valueChanges, this.priorityId.valueChanges, this.categoryId.valueChanges,
      this.channelId.valueChanges, this.assigneeId.valueChanges).pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.resetAndLoad());
  }

  setSla(value: string): void { this.sla.set(value); this.resetAndLoad(); }
  changePage(value: number): void {
    if (value < 1 || value > this.pageCount() || value === this.page()) return;
    this.page.set(value);
    this.load();
  }
  load(): void {
    const currentRequest = ++this.requestId;
    this.loading.set(true);
    this.error.set('');
    this.data.complaints(this.search.value, this.sla(), {
      statusId: this.statusId.value ?? undefined,
      priorityId: this.priorityId.value ?? undefined,
      categoryId: this.categoryId.value ?? undefined,
      channelId: this.channelId.value ?? undefined,
      assigneeId: this.assigneeId.value || undefined
    }, this.page(), this.pageSize).subscribe({
      next: result => {
        if (currentRequest !== this.requestId) return;
        this.items.set(result.items);
        this.total.set(result.total);
        this.loading.set(false);
      },
      error: error => {
        if (currentRequest !== this.requestId) return;
        this.items.set([]);
        this.total.set(0);
        this.loading.set(false);
        this.error.set(apiErrorMessage(error, 'No pudimos cargar los reclamos.'));
      }
    });
  }
  private resetAndLoad(): void { this.page.set(1); this.load(); }
}
