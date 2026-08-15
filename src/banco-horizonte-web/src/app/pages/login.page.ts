import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthFlowError, AuthService } from '../core/auth.service';

interface AuthFeedback {
  title: string;
  detail: string;
  action?: string;
}

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
          <h2>{{ registering() ? 'Crea tu acceso' : 'Bienvenido de vuelta' }}</h2>
          <p>{{ registering() ? 'Registra tu identidad y confirma el enlace enviado a tu correo.' : 'Ingresa con tus credenciales institucionales.' }}</p>
            @if (auth.isDemo) { <div class="demo-note"><strong>Modo demostración</strong><span>Usa cualquier correo y contraseña de al menos 8 caracteres.</span></div> }
          <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            @if (registering()) {
              <div class="name-grid">
                <label>Nombres<input formControlName="firstNames" autocomplete="given-name" placeholder="Tus nombres" />
                  @if (showNameError('firstNames')) { <small>Mínimo 2 caracteres.</small> }</label>
                <label>Apellidos<input formControlName="lastNames" autocomplete="family-name" placeholder="Tus apellidos" />
                  @if (showNameError('lastNames')) { <small>Mínimo 2 caracteres.</small> }</label>
              </div>
            }
            <label>Correo institucional<input type="email" formControlName="email" autocomplete="email" placeholder="nombre@bancohorizonte.com" />
              @if (showError('email')) { <small>Ingresa un correo válido.</small> }</label>
            <label>Contraseña
              <div class="password-control">
                <input [type]="passwordVisible() ? 'text' : 'password'" formControlName="password" [attr.autocomplete]="registering() ? 'new-password' : 'current-password'" placeholder="••••••••" />
                <button class="password-toggle" type="button" (click)="togglePasswordVisibility()"
                  [attr.aria-label]="passwordVisible() ? 'Ocultar contraseña' : 'Mostrar contraseña'" [attr.aria-pressed]="passwordVisible()">
                  @if (passwordVisible()) {
                    <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 3l18 18M10.6 10.6a2 2 0 002.8 2.8M9.9 4.2A10.8 10.8 0 0112 4c5.5 0 9.5 5.2 9.5 5.2a15.8 15.8 0 01-2.4 3.1M6.2 6.2C3.9 7.7 2.5 9.2 2.5 9.2S6.5 16 12 16a9.7 9.7 0 004.1-.9"/></svg>
                  } @else {
                    <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M2.5 12S6.5 5.2 12 5.2 21.5 12 21.5 12 17.5 18.8 12 18.8 2.5 12 2.5 12z"/><circle cx="12" cy="12" r="2.8"/></svg>
                  }
                </button>
              </div>
              @if (showError('password')) { <small>Mínimo 8 caracteres.</small> }</label>
            @if (error(); as feedback) {
              <div class="form-error" role="alert" aria-live="assertive">
                <span class="form-error-icon" aria-hidden="true">!</span>
                <div>
                  <strong>{{ feedback.title }}</strong>
                  <p>{{ feedback.detail }}</p>
                  @if (feedback.action) { <small>{{ feedback.action }}</small> }
                </div>
              </div>
            }
            @if (success()) { <div class="form-success" role="status">{{ success() }}</div> }
            <button class="primary-button" [disabled]="loading()">{{ loading() ? 'PROCESANDO…' : registering() ? 'CREAR CUENTA' : 'INGRESAR AL SISTEMA' }} <span>→</span></button>
          </form>
          <button class="mode-button" type="button" (click)="toggleMode()">{{ registering() ? '¿Ya confirmaste tu cuenta? Inicia sesión' : '¿Primera vez? Crear una cuenta' }}</button>
          <p class="access-foot">Acceso exclusivo para personal autorizado · v1.0</p>
        </div>
      </section>
    </main>
  `,
  styleUrl: './login.page.scss',
  styles: [`
    .name-grid{display:grid;grid-template-columns:1fr 1fr;gap:14px}
    .password-control{position:relative}.password-control input{width:100%;box-sizing:border-box;padding-right:48px}.password-toggle{position:absolute;right:5px;top:5px;width:40px;height:40px;display:grid;place-items:center;border:0;border-radius:4px;background:transparent;color:var(--muted);cursor:pointer}.password-toggle:hover,.password-toggle:focus-visible{color:var(--signal-dark);background:#edf2ef;outline:0}.password-toggle svg{width:20px;height:20px;fill:none;stroke:currentColor;stroke-width:1.8;stroke-linecap:round;stroke-linejoin:round}
    .form-error{display:grid;grid-template-columns:28px 1fr;gap:11px;align-items:start;padding:14px;background:#fff3ef;color:#642c22;border:1px solid #e7b8ad;border-left:3px solid #a74734;border-radius:3px}.form-error-icon{display:grid;place-items:center;width:24px;height:24px;border-radius:50%;background:#a74734;color:#fff;font:800 13px var(--mono)}.form-error strong{display:block;font-size:13px;line-height:1.35}.form-error p{margin:4px 0 0;font-size:12px;line-height:1.5;color:#744036}.form-error small{display:block;margin-top:7px;font:700 10px/1.4 var(--mono);letter-spacing:.02em;color:#642c22}
    .form-success{padding:12px;background:#e6f1ec;color:#155e4b;border-left:3px solid var(--signal-dark);font-size:12px}
    .mode-button{width:100%;margin-top:18px;border:0;background:transparent;color:var(--signal-dark);font:700 11px var(--mono);cursor:pointer;text-decoration:underline;text-underline-offset:4px}
    @media(max-width:480px){.name-grid{grid-template-columns:1fr}}
  `],
})
export class LoginPage {
  readonly auth = inject(AuthService); private readonly fb = inject(FormBuilder); private readonly router = inject(Router);
  readonly loading = signal(false); readonly error = signal<AuthFeedback | null>(null); readonly success = signal(''); readonly registering = signal(false); readonly passwordVisible = signal(false);
  readonly form = this.fb.nonNullable.group({
    firstNames: [{ value: '', disabled: true }, [Validators.required, Validators.minLength(2)]],
    lastNames: [{ value: '', disabled: true }, [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });
  constructor() { if (this.auth.isAuthenticated() && !this.auth.isDemo) void this.auth.navigateHome(); }
  showError(name: 'email'|'password'): boolean { const control = this.form.controls[name]; return control.invalid && (control.touched || this.form.dirty); }
  showNameError(name: 'firstNames'|'lastNames'): boolean { const control = this.form.controls[name]; return control.invalid && (control.touched || this.form.dirty); }
  togglePasswordVisibility(): void { this.passwordVisible.update(value => !value); }
  toggleMode(): void {
    this.registering.update(value => !value); this.error.set(null); this.success.set(''); this.passwordVisible.set(false); this.form.controls.password.setValue('');
    for (const control of [this.form.controls.firstNames, this.form.controls.lastNames]) this.registering() ? control.enable() : control.disable();
  }
  async submit(): Promise<void> {
    this.form.markAllAsTouched(); if (this.form.invalid) return;
    this.loading.set(true); this.error.set(null); this.success.set('');
    try {
      if (this.registering()) {
        await this.auth.signUp(this.form.controls.firstNames.value, this.form.controls.lastNames.value, this.form.controls.email.value, this.form.controls.password.value);
        this.success.set('Cuenta creada. Revisa tu correo, confirma el enlace y luego inicia sesión.');
        this.registering.set(false); this.form.controls.firstNames.disable(); this.form.controls.lastNames.disable(); this.form.controls.password.setValue('');
      } else await this.auth.login(this.form.controls.email.value, this.form.controls.password.value);
    } catch (e) { this.error.set(this.feedbackFor(e)); } finally { this.loading.set(false); }
  }
  private feedbackFor(error: unknown): AuthFeedback {
    const stage = error instanceof AuthFlowError ? error.stage : undefined;
    const source = error instanceof AuthFlowError ? error.source : error;

    if (source instanceof HttpErrorResponse) {
      const payload = source.error ?? {};
      const code = String(payload.code ?? payload.error_code ?? payload.error ?? '').toLowerCase();
      const serverMessage = String(payload.msg ?? payload.message ?? payload.error_description ?? payload.detail ?? '').toLowerCase();
      const clue = `${code} ${serverMessage}`;

      if (clue.includes('email_not_confirmed') || clue.includes('email not confirmed')) {
        return {
          title: 'Aún falta confirmar tu correo',
          detail: 'Abre el mensaje que te enviamos y selecciona el enlace de confirmación. Después vuelve aquí para ingresar.',
          action: 'Si ya lo confirmaste, espera unos segundos y vuelve a intentarlo.'
        };
      }
      if (clue.includes('invalid_credentials') || clue.includes('invalid login credentials') || clue.includes('invalid_grant')) {
        return {
          title: 'El correo o la contraseña no coinciden',
          detail: 'Revisa que el correo esté bien escrito y vuelve a ingresar tu contraseña.',
          action: 'La contraseña distingue entre mayúsculas y minúsculas.'
        };
      }
      if (clue.includes('user_banned') || clue.includes('banned')) {
        return {
          title: 'Esta cuenta no está disponible',
          detail: 'El acceso de esta cuenta fue suspendido. Comunícate con el supervisor para revisar su estado.'
        };
      }
      if (source.status === 0) {
        return stage === 'profile'
          ? { title: 'Tu cuenta fue validada, pero no pudimos abrir el sistema', detail: 'El servicio interno de Banco Horizonte no está respondiendo.', action: 'Espera un momento y vuelve a intentarlo.' }
          : { title: 'No pudimos conectar con el servicio de acceso', detail: 'Tu cuenta no fue bloqueada ni se modificó tu contraseña.', action: 'Revisa tu conexión e intenta nuevamente.' };
      }
      if (source.status === 401 && stage === 'profile') {
        return {
          title: 'Confirmamos tu cuenta, pero no pudimos abrir tu perfil',
          detail: 'El sistema no pudo validar la sesión con Banco Horizonte. No guardamos una sesión incompleta.',
          action: 'Intenta ingresar nuevamente. Si se repite, el administrador debe revisar la configuración de acceso.'
        };
      }
      if (source.status === 403) {
        return { title: 'Tu cuenta no tiene acceso al sistema', detail: 'Un supervisor debe asignarte un rol antes de que puedas ingresar.' };
      }
      if (source.status === 404 && stage === 'profile') {
        return {
          title: 'Tu cuenta está confirmada, pero falta habilitar tu perfil',
          detail: 'Encontramos tu cuenta, pero su perfil interno no existe o está desactivado.',
          action: 'Pide a un supervisor que revise tu usuario y el rol asignado.'
        };
      }
      if (source.status === 429) {
        return { title: 'Hiciste varios intentos seguidos', detail: 'Pausamos temporalmente nuevos intentos para proteger tu cuenta.', action: 'Espera un minuto y vuelve a intentarlo.' };
      }
      if (source.status >= 500) {
        return { title: 'El servicio de acceso está temporalmente indisponible', detail: 'El problema está de nuestro lado; tu cuenta no fue modificada.', action: 'Intenta nuevamente en unos minutos.' };
      }
      if (stage === 'sign-in') {
        return { title: 'No pudimos validar esos datos', detail: 'Revisa el correo y la contraseña. Si acabas de registrarte, confirma primero el enlace enviado a tu correo.' };
      }
      if (stage === 'profile') {
        return { title: 'Tu cuenta es correcta, pero no pudimos abrir tu perfil', detail: 'No guardamos una sesión incompleta.', action: 'Intenta nuevamente o solicita al supervisor que revise tu acceso.' };
      }
    }

    if (source instanceof Error && source.message.includes('ya está registrado')) {
      return { title: 'Este correo ya tiene una cuenta', detail: 'No necesitas registrarte otra vez.', action: 'Selecciona “Inicia sesión” e ingresa con tu contraseña.' };
    }
    return {
      title: this.registering() ? 'No pudimos crear tu cuenta' : 'No pudimos iniciar sesión',
      detail: 'Ocurrió un problema inesperado, pero tu información no se perdió.',
      action: 'Intenta nuevamente. Si se repite, comunícate con el administrador.'
    };
  }
}
