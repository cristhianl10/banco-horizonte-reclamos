import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DataService } from '../core/data.service';
import { CatalogResponse, ComplaintDetail } from '../core/models';
import { AuthService } from '../core/auth.service';

@Component({
  imports:[DatePipe,RouterLink,FormsModule],
  template:`
    @if(item();as claim){
      <header class="detail-head"><div><a routerLink="/reclamos">← VOLVER A RECLAMOS</a><p class="section-code">EXPEDIENTE / {{claim.code}}</p><div class="title-line"><h1>{{claim.category}}</h1><span class="priority" [attr.data-priority]="claim.priority">{{claim.priority}}</span></div><p>{{claim.customer}} · {{claim.document}}</p></div><div class="sla-clock" [attr.data-state]="claim.slaState"><small>{{claim.slaState==='Vencido'?'SLA INCUMPLIDO':'FECHA LÍMITE SLA'}}</small><strong>{{claim.slaDeadline|date:'dd MMM · HH:mm'}}</strong><span>{{slaMessage(claim.slaDeadline,claim.slaState)}}</span></div></header>
      <div class="detail-grid">
        <section class="main-column">
          <article class="panel"><header><p class="section-code">01 / DESCRIPCIÓN</p><span>{{claim.channel}}</span></header><div class="description"><h2>{{claim.subcategory||claim.category}}</h2><p>{{claim.description}}</p><div><span>MONTO AFECTADO <b>{{claim.affectedAmount == null ? 'No informado' : ('$ ' + claim.affectedAmount)}}</b></span><span>CANAL INDISPONIBLE <b>{{claim.digitalChannelUnavailable?'Sí':'No'}}</b></span><span>PUNTAJE <b>{{claim.priorityScore}} pts</b></span></div>@if(claim.priorityRules.length){<h3>Reglas aplicadas</h3><ul>@for(rule of claim.priorityRules;track rule.rule){<li>{{rule.rule}} <strong>+{{rule.points}}</strong></li>}</ul>}@else{<p>Sin condiciones adicionales: prioridad base baja.</p>}</div></article>
          <article class="panel timeline"><header><p class="section-code">02 / TRAZABILIDAD</p><span>{{claim.timeline.length}} EVENTOS</span></header><div>@for(event of claim.timeline;track event.at+event.type){<div class="event"><i></i><div><strong>{{event.type.replaceAll('_',' ')}}</strong><p>{{event.description}}</p><small>{{event.actor}} · {{event.at|date:'dd MMM yyyy, HH:mm'}}</small></div></div>}</div></article>
        </section>
        <aside>
          @if(auth.hasAnyRole('Analista','Supervisor','Administrador')){
            <article class="panel action-card"><header><p class="section-code">GESTIÓN DEL CASO</p></header><div>
              <label>Estado actual<select [(ngModel)]="statusId">@for(status of catalogs()?.statuses;track status.id){<option [ngValue]="status.id">{{status.name}}</option>}</select></label>
              <label>Observación obligatoria<textarea [(ngModel)]="observation" rows="3" placeholder="Explica el motivo del cambio…"></textarea></label><button (click)="updateStatus()" [disabled]="observation.trim().length < 3 || statusId === claim.statusId">ACTUALIZAR ESTADO</button>
            </div></article>
          }
          <article class="panel action-card"><header><p class="section-code">RESPONSABLE</p></header><div><div class="current-owner"><span>{{ownerInitials(claim.assignee)}}</span><div><small>ASIGNADO A</small><strong>{{claim.assignee||'Sin responsable'}}</strong></div></div>
            @if(auth.hasAnyRole('Supervisor','Administrador')){
              <label>Asignar analista<select [(ngModel)]="analystId"><option value="">Seleccionar</option>@for(a of catalogs()?.analysts;track a.id){<option [value]="a.id">{{a.name}}</option>}</select></label><button class="secondary" (click)="assign()" [disabled]="!analystId">ASIGNAR RESPONSABLE</button>
            } @else if(auth.hasAnyRole('Analista')&&!claim.assignee){<button class="secondary" (click)="take()">ASUMIR ESTE CASO</button>}
          </div></article>
          <article class="panel customer-card"><header><p class="section-code">DATOS DEL CLIENTE</p></header><dl><dt>Nombre</dt><dd>{{claim.customer}}</dd><dt>Documento</dt><dd>{{claim.document}}</dd><dt>Correo</dt><dd>{{claim.email||'No registrado'}}</dd><dt>Teléfono</dt><dd>{{claim.phone||'No registrado'}}</dd></dl></article>
          @if(message()){<div class="message" role="status">{{message()}}</div>}
        </aside>
      </div>
    }@else{<div class="loading-state">Cargando expediente…</div>}
  `,
  styleUrl:'./complaint-detail.page.scss'
})
export class ComplaintDetailPage{
  private readonly data=inject(DataService);private readonly route=inject(ActivatedRoute);readonly auth=inject(AuthService);readonly item=signal<ComplaintDetail|null>(null);readonly catalogs=signal<CatalogResponse|null>(null);readonly message=signal('');statusId=1;analystId='';observation='';private id='';
  constructor(){this.id=this.route.snapshot.paramMap.get('id')!;this.data.catalogs().subscribe(x=>this.catalogs.set(x));this.load();}
  load():void{this.data.complaint(this.id).subscribe(x=>{this.item.set(x);this.statusId=x.statusId;this.analystId=x.assigneeId||'';});}
  updateStatus():void{if(this.observation.trim().length<3)return;this.data.changeStatus(this.id,this.statusId,this.observation.trim()).subscribe(()=>{this.message.set('Estado actualizado correctamente.');this.observation='';this.load();});}
  assign():void{this.data.assign(this.id,this.analystId,'Asignación desde el expediente').subscribe(()=>{this.message.set('Responsable actualizado correctamente.');this.load();});}
  take():void{this.data.take(this.id).subscribe(()=>{this.message.set('El caso fue asignado a tu usuario.');this.load();});}
  ownerInitials(name:string|null):string{return(name||'SA').split(' ').slice(0,2).map(x=>x[0]).join('');}
  slaMessage(deadline:string,state:string):string{const minutes=Math.max(0,Math.floor(Math.abs(new Date(deadline).getTime()-Date.now())/60_000));const text=`${Math.floor(minutes/60)}h ${minutes%60}m`;return state==='Vencido'?`Vencido hace ${text}`:`Restan ${text}`;}
}
