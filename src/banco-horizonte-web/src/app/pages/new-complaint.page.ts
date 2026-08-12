import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { DataService } from '../core/data.service';
import { CatalogResponse, CreateComplaint } from '../core/models';

@Component({
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <header class="page-head"><div><a routerLink="/reclamos">← VOLVER A LA BANDEJA</a><p class="section-code">REGISTRO / NUEVO CASO</p><h1>Registrar reclamo</h1><p>La prioridad y el plazo SLA se calcularán automáticamente.</p></div><div class="step-marker"><span>01</span><b>/ 01</b></div></header>
    @if (success(); as result) {
      <section class="success-card" role="status"><span>✓</span><p class="section-code">CASO REGISTRADO</p><h2>{{ result.code }}</h2><p>El reclamo fue clasificado correctamente y ya aparece en la bandeja operativa.</p><button (click)="openResult(result.id)">ABRIR RECLAMO →</button></section>
    } @else {
      <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <section class="form-panel"><header><span>01</span><div><h2>Identificación del cliente</h2><p>Busca o registra los datos de la persona afectada.</p></div></header>
          <div class="form-grid customer-grid" formGroupName="customer">
            <label>Tipo de documento<select formControlName="documentType"><option value="CÉDULA">Cédula</option><option value="PASAPORTE">Pasaporte</option><option value="RUC">RUC</option></select></label>
            <label>Número de documento<input formControlName="documentNumber" placeholder="0912345678" />@if (invalid('customer.documentNumber')){<small>Mínimo 5 caracteres.</small>}</label>
            <label>Nombres<input formControlName="firstNames" placeholder="Nombres del cliente" />@if (invalid('customer.firstNames')){<small>Campo obligatorio.</small>}</label>
            <label>Apellidos<input formControlName="lastNames" placeholder="Apellidos del cliente" />@if (invalid('customer.lastNames')){<small>Campo obligatorio.</small>}</label>
            <label>Correo electrónico<input type="email" formControlName="email" placeholder="cliente@correo.com" />@if (invalid('customer.email')){<small>Correo inválido.</small>}</label>
            <label>Teléfono<input formControlName="phone" placeholder="099 000 0000" /></label>
          </div>
        </section>
        <section class="form-panel"><header><span>02</span><div><h2>Clasificación del reclamo</h2><p>Estos datos determinan prioridad, responsable y SLA.</p></div></header>
          <div class="form-grid">
            <label>Canal de recepción<select formControlName="receptionChannelId"><option [ngValue]="null">Seleccionar canal</option>@for(item of catalogs()?.channels;track item.id){<option [ngValue]="item.id">{{item.name}}</option>}</select>@if(invalid('receptionChannelId')){<small>Selecciona un canal.</small>}</label>
            <label>Categoría<select formControlName="categoryId"><option [ngValue]="null">Seleccionar categoría</option>@for(item of catalogs()?.categories;track item.id){<option [ngValue]="item.id">{{item.name}}</option>}</select>@if(invalid('categoryId')){<small>Selecciona una categoría.</small>}</label>
            <label>Subcategoría<select formControlName="subcategoryId"><option [ngValue]="null">Sin subcategoría</option>@for(item of filteredSubcategories();track item.id){<option [ngValue]="item.id">{{item.name}}</option>}</select></label>
            <label class="full">Descripción<textarea formControlName="description" rows="5" placeholder="Describe qué ocurrió, cuándo y cuál fue el impacto para el cliente…"></textarea><span class="counter">{{ form.controls.description.value.length }}/4000</span>@if(invalid('description')){<small>Describe el caso con al menos 20 caracteres.</small>}</label>
          </div>
          <div class="severity-grid">
            <fieldset><legend>Impacto</legend><p>¿Qué tan grave es el efecto?</p><div>@for(level of levels;track level.value){<label><input type="radio" formControlName="impact" [value]="level.value"/><span><b>{{level.value}}</b>{{level.label}}</span></label>}</div></fieldset>
            <fieldset><legend>Urgencia</legend><p>¿Qué tan pronto debe resolverse?</p><div>@for(level of levels;track level.value){<label><input type="radio" formControlName="urgency" [value]="level.value"/><span><b>{{level.value}}</b>{{level.label}}</span></label>}</div></fieldset>
          </div>
        </section>
        @if(error()){<div class="form-error" role="alert">{{error()}}</div>}
        @if(duplicates().length){<section class="duplicate-alert"><strong>Posibles duplicados detectados</strong><p>El cliente tiene {{duplicates().length}} caso(s) reciente(s) en esta categoría. Revisa antes de confirmar.</p><button type="button" (click)="confirmDuplicate()">CONFIRMAR REGISTRO DE TODOS MODOS</button></section>}
        <footer class="form-actions"><a routerLink="/reclamos">CANCELAR</a><button [disabled]="saving()">{{saving()?'CALCULANDO…':'CALCULAR Y REGISTRAR'}} <span>→</span></button></footer>
      </form>
    }
  `,
  styleUrl: './new-complaint.page.scss',
})
export class NewComplaintPage {
  private readonly fb=inject(FormBuilder);private readonly data=inject(DataService);private readonly router=inject(Router);
  readonly catalogs=signal<CatalogResponse|null>(null);readonly saving=signal(false);readonly error=signal('');readonly duplicates=signal<unknown[]>([]);readonly success=signal<{id:string;code:string}|null>(null);
  readonly levels=[{value:1,label:'Bajo'},{value:2,label:'Medio'},{value:3,label:'Alto'}];
  readonly form=this.fb.nonNullable.group({customer:this.fb.nonNullable.group({documentType:['CÉDULA',Validators.required],documentNumber:['',[Validators.required,Validators.minLength(5),Validators.maxLength(30)]],firstNames:['',[Validators.required,Validators.minLength(2)]],lastNames:['',[Validators.required,Validators.minLength(2)]],email:['',Validators.email],phone:['']}),receptionChannelId:[null as number|null,Validators.required],categoryId:[null as number|null,Validators.required],subcategoryId:[null as number|null],description:['',[Validators.required,Validators.minLength(20),Validators.maxLength(4000)]],impact:[2,[Validators.required,Validators.min(1),Validators.max(3)]],urgency:[2,[Validators.required,Validators.min(1),Validators.max(3)]],confirmPossibleDuplicate:[false]});
  constructor(){
    this.data.catalogs().subscribe(x=>this.catalogs.set({
      ...x,
      subcategories:x.subcategories.map((item,index)=>({...item,categoryId:item.categoryId??Math.floor(index/2)+1}))
    }));
    this.form.controls.categoryId.valueChanges.subscribe(()=>this.form.controls.subcategoryId.setValue(null));
  }
  filteredSubcategories(){const categoryId=this.form.controls.categoryId.value;return this.catalogs()?.subcategories.filter(x=>x.categoryId===categoryId)??[];}
  invalid(path:string):boolean{const control=this.form.get(path);return !!control?.invalid&&(control.touched||this.form.dirty);}
  submit():void{this.form.markAllAsTouched();if(this.form.invalid)return;this.saving.set(true);this.error.set('');this.data.createComplaint(this.form.getRawValue() as CreateComplaint).subscribe({next:r=>{this.success.set({id:r.id,code:r.code});this.saving.set(false);},error:(e:HttpErrorResponse)=>{this.saving.set(false);if(e.status===409){this.duplicates.set(e.error?.possibleDuplicates??[]);this.error.set('');}else this.error.set(e.error?.detail??e.error?.message??'No fue posible registrar el reclamo.');}});}
  confirmDuplicate():void{this.form.controls.confirmPossibleDuplicate.setValue(true);this.duplicates.set([]);this.submit();}openResult(id:string):void{void this.router.navigate(['/reclamos',id]);}
}
