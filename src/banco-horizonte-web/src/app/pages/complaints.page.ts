import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, merge } from 'rxjs';
import { DataService } from '../core/data.service';
import { CatalogResponse, ComplaintListItem } from '../core/models';
import { AuthService } from '../core/auth.service';

@Component({
  imports: [DatePipe, RouterLink, ReactiveFormsModule],
  template: `
    <header class="page-head"><div><p class="section-code">OPERACIÓN / BANDEJA</p><h1>Reclamos</h1><p>{{ items().length }} casos visibles · ordenados por riesgo SLA</p></div>@if(auth.hasAnyRole('Operador','Supervisor','Administrador')){<a class="action-button" routerLink="/reclamos/nuevo">＋ NUEVO RECLAMO</a>}</header>
    <section class="filter-bar" aria-label="Filtros de reclamos"><label><span>⌕</span><input [formControl]="search" placeholder="Buscar código, cliente o cédula" aria-label="Buscar reclamos" /></label><button [class.active]="sla() === ''" (click)="setSla('')">TODOS</button><button [class.active]="sla() === 'near'" (click)="setSla('near')">PRÓXIMOS</button><button [class.active]="sla() === 'overdue'" (click)="setSla('overdue')">VENCIDOS</button></section>
    <section class="filter-bar" aria-label="Filtros detallados">
      <select [formControl]="statusId"><option [ngValue]="null">Todos los estados</option>@for(item of catalogs()?.statuses;track item.id){<option [ngValue]="item.id">{{item.name}}</option>}</select>
      <select [formControl]="priorityId"><option [ngValue]="null">Todas las prioridades</option>@for(item of catalogs()?.priorities;track item.id){<option [ngValue]="item.id">{{item.name}}</option>}</select>
      <select [formControl]="categoryId"><option [ngValue]="null">Todas las categorías</option>@for(item of catalogs()?.categories;track item.id){<option [ngValue]="item.id">{{item.name}}</option>}</select>
      <select [formControl]="channelId"><option [ngValue]="null">Todos los canales</option>@for(item of catalogs()?.channels;track item.id){<option [ngValue]="item.id">{{item.name}}</option>}</select>
      <select [formControl]="assigneeId"><option value="">Todos los responsables</option>@for(item of catalogs()?.analysts;track item.id){<option [value]="item.id">{{item.name}}</option>}</select>
    </section>
    <section class="table-panel">
      <div class="table-head"><span>CASO / CLIENTE</span><span>CATEGORÍA</span><span>PRIORIDAD</span><span>ESTADO</span><span>RESPONSABLE</span><span>SLA</span></div>
      @for (item of items(); track item.id) {
        <a class="table-row" [routerLink]="['/reclamos', item.id]">
          <div><strong>{{ item.code }}</strong><small>{{ item.customer }}</small></div><span>{{ item.category }}</span>
          <span class="priority" [attr.data-priority]="item.priority">{{ item.priority }}</span><span class="state">{{ item.status }}</span>
          <span>{{ item.assignee || 'Sin asignar' }}</span><div class="sla-cell" [attr.data-state]="item.slaState"><strong>{{ item.slaState }}</strong><small>{{ item.slaDeadline | date:'dd MMM · HH:mm' }}</small></div>
        </a>
      } @empty { <div class="empty-state"><strong>Sin coincidencias</strong><p>Ajusta los filtros o registra un nuevo reclamo.</p></div> }
    </section>
  `,
  styleUrl: './complaints.page.scss',
})
export class ComplaintsPage {
  private readonly data = inject(DataService); private readonly route = inject(ActivatedRoute); readonly auth = inject(AuthService);
  readonly items = signal<ComplaintListItem[]>([]); readonly catalogs = signal<CatalogResponse|null>(null); readonly search = new FormControl('', { nonNullable: true }); readonly sla = signal(this.route.snapshot.queryParamMap.get('sla') ?? '');
  readonly statusId=new FormControl<number|null>(null);readonly priorityId=new FormControl<number|null>(null);readonly categoryId=new FormControl<number|null>(null);readonly channelId=new FormControl<number|null>(null);readonly assigneeId=new FormControl('',{nonNullable:true});
  constructor() { this.data.catalogs().subscribe(value=>this.catalogs.set(value));this.load(); this.search.valueChanges.pipe(debounceTime(250), distinctUntilChanged()).subscribe(() => this.load());merge(this.statusId.valueChanges,this.priorityId.valueChanges,this.categoryId.valueChanges,this.channelId.valueChanges,this.assigneeId.valueChanges).subscribe(()=>this.load()); }
  setSla(value: string): void { this.sla.set(value); this.load(); }
  private load(): void { this.data.complaints(this.search.value, this.sla(),{statusId:this.statusId.value??undefined,priorityId:this.priorityId.value??undefined,categoryId:this.categoryId.value??undefined,channelId:this.channelId.value??undefined,assigneeId:this.assigneeId.value||undefined}).subscribe(result => this.items.set(result.items)); }
}
