import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { DataService } from '../core/data.service';
import { CatalogResponse, CreateComplaint, CreateComplaintResult } from '../core/models';

@Component({
  imports: [ReactiveFormsModule, RouterLink, DatePipe],
  template: `
    <header class="page-head"><div><a routerLink="/reclamos">← VOLVER A LA BANDEJA</a><p class="section-code">REGISTRO / NUEVO CASO</p><h1>Registrar reclamo</h1><p>La prioridad y el plazo SLA se calcularán automáticamente.</p></div><div class="step-marker"><span>01</span><b>/ 01</b></div></header>
    @if (success(); as result) {
      <section class="success-card" role="status"><span>✓</span><p class="section-code">CASO REGISTRADO</p><h2>{{ result.code }}</h2><p>Prioridad <strong>{{result.priority}}</strong> · {{result.priorityScore}} puntos · SLA hasta {{result.slaDeadline | date:'dd MMM, HH:mm'}}.</p><button (click)="openResult(result.id)">ABRIR RECLAMO →</button></section>
    } @else {
      <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <section class="form-panel"><header><span>01</span><div><h2>Identificación del cliente</h2><p>Busca o registra los datos de la persona afectada.</p></div></header>
          <div class="form-grid customer-grid" formGroupName="customer">
            <label>Tipo de documento<select formControlName="documentType"><option value="CÉDULA">Cédula</option><option value="PASAPORTE">Pasaporte</option><option value="RUC">RUC</option></select></label>
            <label>Número de documento<input formControlName="documentNumber" [attr.inputmode]="numericDocument() ? 'numeric' : 'text'" [attr.maxlength]="documentMaxLength()" [placeholder]="documentPlaceholder()" autocomplete="off" />@if (invalid('customer.documentNumber')){<small>{{ documentErrorMessage() }}</small>}</label>
            <label>Nombres<input formControlName="firstNames" placeholder="Nombres del cliente" />@if (invalid('customer.firstNames')){<small>Campo obligatorio.</small>}</label>
            <label>Apellidos<input formControlName="lastNames" placeholder="Apellidos del cliente" />@if (invalid('customer.lastNames')){<small>Campo obligatorio.</small>}</label>
            <label>Correo electrónico <span>(opcional)</span><input type="email" formControlName="email" autocomplete="email" inputmode="email" placeholder="cliente@correo.com" />@if (invalid('customer.email')){<small>Escribe un correo válido, por ejemplo: cliente@correo.com.</small>}</label>
            <label>Teléfono <span>(opcional)</span><input type="tel" formControlName="phone" inputmode="numeric" autocomplete="tel" maxlength="10" placeholder="0990000000" />@if (invalid('customer.phone')){<small>El teléfono debe tener exactamente 10 dígitos, sin espacios.</small>}</label>
          </div>
        </section>
        <section class="form-panel"><header><span>02</span><div><h2>Datos del reclamo</h2><p>Registra hechos verificables; el sistema aplicará las reglas de prioridad.</p></div></header>
          <div class="form-grid">
            <label>Canal de recepción<select formControlName="receptionChannelId"><option [ngValue]="null">Seleccionar canal</option>@for(item of catalogs()?.channels;track item.id){<option [ngValue]="item.id">{{item.name}}</option>}</select>@if(invalid('receptionChannelId')){<small>Selecciona un canal.</small>}</label>
            <label>Categoría<select formControlName="categoryId"><option [ngValue]="null">Seleccionar categoría</option>@for(item of catalogs()?.categories;track item.id){<option [ngValue]="item.id">{{item.name}}</option>}</select>@if(invalid('categoryId')){<small>Selecciona una categoría.</small>}</label>
            <label>Subcategoría<select formControlName="subcategoryId"><option [ngValue]="null">Sin subcategoría</option>@for(item of filteredSubcategories();track item.id){<option [ngValue]="item.id">{{item.name}}</option>}</select></label>
            <label>Fecha y hora de recepción<input type="datetime-local" formControlName="receivedAt" />@if(invalid('receivedAt')){<small>Indica cuándo se recibió el reclamo.</small>}</label>
            <label>Monto afectado (USD) <span>(opcional)</span><input type="number" min="0" step="0.01" formControlName="affectedAmount" placeholder="0.00" />@if(invalid('affectedAmount')){<small>El monto no puede ser negativo.</small>}</label>
            <label class="full">Descripción<textarea formControlName="description" rows="5" placeholder="Describe qué ocurrió, cuándo y cuál fue el efecto para el cliente…"></textarea><span class="counter">{{ form.controls.description.value.length }}/4000</span>@if(invalid('description')){<small>Describe el caso con al menos 20 caracteres.</small>}</label>
            <label class="full"><input type="checkbox" formControlName="digitalChannelUnavailable" /> El canal digital está completamente indisponible para el cliente.</label>
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
  private readonly fb = inject(FormBuilder);
  private readonly data = inject(DataService);
  private readonly router = inject(Router);
  readonly catalogs = signal<CatalogResponse | null>(null);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly duplicates = signal<unknown[]>([]);
  readonly success = signal<CreateComplaintResult | null>(null);
  readonly form = this.fb.nonNullable.group({
    customer: this.fb.nonNullable.group({
      documentType: ['CÉDULA', Validators.required], documentNumber: ['', [Validators.required, documentNumberValidator]],
      firstNames: ['', [Validators.required, Validators.minLength(2)]], lastNames: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', Validators.email], phone: ['', Validators.pattern(/^\d{10}$/)]
    }),
    receptionChannelId: [null as number | null, Validators.required], categoryId: [null as number | null, Validators.required],
    subcategoryId: [null as number | null], receivedAt: [toLocalDateTime(new Date()), Validators.required],
    affectedAmount: [null as number | null, Validators.min(0)], digitalChannelUnavailable: [false],
    description: ['', [Validators.required, Validators.minLength(20), Validators.maxLength(4000)]], confirmPossibleDuplicate: [false]
  });

  constructor() {
    this.data.catalogs().subscribe(x => this.catalogs.set(x));
    this.form.controls.categoryId.valueChanges.subscribe(() => this.form.controls.subcategoryId.setValue(null));
    this.form.controls.customer.controls.documentType.valueChanges.subscribe(() =>
      this.form.controls.customer.controls.documentNumber.updateValueAndValidity());
  }

  filteredSubcategories() { const categoryId = this.form.controls.categoryId.value; return this.catalogs()?.subcategories.filter(x => x.categoryId === categoryId) ?? []; }
  invalid(path: string): boolean { const control = this.form.get(path); return !!control?.invalid && (control.touched || this.form.dirty); }
  numericDocument(): boolean { return normalizedDocumentType(this.form.controls.customer.controls.documentType.value) !== 'PASAPORTE'; }
  documentMaxLength(): number { const type = normalizedDocumentType(this.form.controls.customer.controls.documentType.value); return type === 'RUC' ? 13 : type === 'PASAPORTE' ? 20 : 10; }
  documentPlaceholder(): string { const type = normalizedDocumentType(this.form.controls.customer.controls.documentType.value); return type === 'RUC' ? '1790012345001' : type === 'PASAPORTE' ? 'A1234567' : '0912345678'; }
  documentErrorMessage(): string { const type = normalizedDocumentType(this.form.controls.customer.controls.documentType.value); return type === 'RUC' ? 'El RUC debe tener exactamente 13 dígitos.' : type === 'PASAPORTE' ? 'El pasaporte debe tener entre 5 y 20 caracteres, solo letras y números.' : 'La cédula debe tener exactamente 10 dígitos.'; }
  submit(): void {
    this.form.markAllAsTouched(); if (this.form.invalid) return;
    this.saving.set(true); this.error.set('');
    const raw = this.form.getRawValue();
    const request = { ...raw, receivedAt: new Date(raw.receivedAt).toISOString() } as CreateComplaint;
    this.data.createComplaint(request).subscribe({
      next: result => { this.success.set(result); this.saving.set(false); },
      error: (error: HttpErrorResponse) => { this.saving.set(false); if (error.status === 409) { this.duplicates.set(error.error?.possibleDuplicates ?? []); this.error.set(''); } else this.error.set(error.error?.detail ?? error.error?.message ?? 'No fue posible registrar el reclamo.'); }
    });
  }
  confirmDuplicate(): void { this.form.controls.confirmPossibleDuplicate.setValue(true); this.duplicates.set([]); this.submit(); }
  openResult(id: string): void { void this.router.navigate(['/reclamos', id]); }
}

function normalizedDocumentType(value: string): string { return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').trim().toUpperCase(); }
function toLocalDateTime(value: Date): string { const offset = value.getTimezoneOffset() * 60_000; return new Date(value.getTime() - offset).toISOString().slice(0, 16); }
function documentNumberValidator(control: AbstractControl): ValidationErrors | null {
  const value = String(control.value ?? '').trim(); if (!value) return null;
  const type = normalizedDocumentType(String(control.parent?.get('documentType')?.value ?? 'CEDULA'));
  if (type === 'RUC') return /^\d{13}$/.test(value) ? null : { documentFormat: true };
  if (type === 'PASAPORTE') return /^[A-Za-z0-9]{5,20}$/.test(value) ? null : { documentFormat: true };
  return /^\d{10}$/.test(value) ? null : { documentFormat: true };
}
