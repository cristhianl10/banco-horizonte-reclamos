import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  imports: [ReactiveFormsModule],
  template: `
    <main class="login-page">
      <section class="story" aria-label="Presentación">
        <div class="story-grid"></div>
        <a class="login-brand" href="/"><span>BH</span><div><strong>Banco Horizonte</strong><small>Centro de operaciones</small></div></a>
        <div class="story-copy"><p class="eyebrow">CONTROL / TRAZABILIDAD / SLA</p><h1>Cada reclamo<br><em>tiene un horizonte.</em></h1><p>Una operación visible, medible y responsable desde el primer contacto hasta el cierre.</p></div>
        <div class="story-stats"><div><strong>24/7</strong><span>Monitoreo operativo</span></div><div><strong>100%</strong><span>Trazabilidad de cambios</span></div></div>
      </section>
      <section class="access">
        <div class="access-card">
          <div class="access-index">ACCESO SEGURO <span>02—06</span></div>
          <h2>Bienvenido de vuelta</h2><p>Ingresa con tus credenciales institucionales.</p>
          @if (auth.isDemo) { <div class="demo-note"><strong>Modo demostración</strong><span>Usa cualquier correo y contraseña de 6 caracteres.</span></div> }
          <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <label>Correo institucional<input type="email" formControlName="email" autocomplete="email" placeholder="nombre@bancohorizonte.com" />
              @if (showError('email')) { <small>Ingresa un correo válido.</small> }</label>
            <label>Contraseña<input type="password" formControlName="password" autocomplete="current-password" placeholder="••••••••" />
              @if (showError('password')) { <small>Mínimo 6 caracteres.</small> }</label>
            @if (error()) { <div class="form-error" role="alert">{{ error() }}</div> }
            <button class="primary-button" [disabled]="loading()">{{ loading() ? 'VALIDANDO…' : 'INGRESAR AL SISTEMA' }} <span>→</span></button>
          </form>
          <p class="access-foot">Acceso exclusivo para personal autorizado · v1.0</p>
        </div>
      </section>
    </main>
  `,
  styleUrl: './login.page.scss',
})
export class LoginPage {
  readonly auth = inject(AuthService); private readonly fb = inject(FormBuilder); private readonly router = inject(Router);
  readonly loading = signal(false); readonly error = signal('');
  readonly form = this.fb.nonNullable.group({ email: ['supervisor@bancohorizonte.demo', [Validators.required, Validators.email]], password: ['demo123', [Validators.required, Validators.minLength(6)]] });
  constructor() { if (this.auth.isAuthenticated() && !this.auth.isDemo) void this.auth.navigateHome(); }
  showError(name: 'email'|'password'): boolean { const control = this.form.controls[name]; return control.invalid && (control.touched || this.form.dirty); }
  async submit(): Promise<void> { this.form.markAllAsTouched(); if (this.form.invalid) return; this.loading.set(true); this.error.set(''); try { await this.auth.login(this.form.value.email!, this.form.value.password!); } catch (e) { this.error.set(e instanceof Error ? e.message : 'No fue posible iniciar sesión.'); } finally { this.loading.set(false); } }
}
