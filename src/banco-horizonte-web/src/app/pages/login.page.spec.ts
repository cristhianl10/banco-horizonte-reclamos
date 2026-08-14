import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { LoginPage } from './login.page';

describe('LoginPage', () => {
  beforeEach(() => TestBed.configureTestingModule({
    imports: [LoginPage], providers: [provideRouter([]), { provide: AuthService, useValue: { isDemo: true, isAuthenticated: () => false, login: jasmine.createSpy('login').and.resolveTo() } }]
  }));

  it('rejects malformed email and short password', () => {
    const fixture = TestBed.createComponent(LoginPage); const component = fixture.componentInstance;
    component.form.setValue({ firstNames: '', lastNames: '', email: 'correo-invalido', password: '123' });
    expect(component.form.invalid).toBeTrue();
    expect(component.form.controls.email.hasError('email')).toBeTrue();
    expect(component.form.controls.password.hasError('minlength')).toBeTrue();
  });

  it('accepts a valid institutional login form', () => {
    const component = TestBed.createComponent(LoginPage).componentInstance;
    component.form.setValue({ firstNames: '', lastNames: '', email: 'supervisor@bancohorizonte.com', password: 'segura123' });
    expect(component.form.valid).toBeTrue();
  });

  it('shows and hides the password without changing its value', () => {
    const fixture = TestBed.createComponent(LoginPage);
    const component = fixture.componentInstance;
    component.form.controls.password.setValue('segura123');
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('input[formcontrolname="password"]') as HTMLInputElement;
    expect(input.type).toBe('password');

    component.togglePasswordVisibility();
    fixture.detectChanges();
    expect(input.type).toBe('text');
    expect(input.value).toBe('segura123');

    component.togglePasswordVisibility();
    fixture.detectChanges();
    expect(input.type).toBe('password');
  });

  it('explains when the email has not been confirmed', async () => {
    const auth = TestBed.inject(AuthService);
    (auth.login as jasmine.Spy).and.rejectWith(new HttpErrorResponse({
      status: 400,
      error: { code: 'email_not_confirmed' }
    }));
    const component = TestBed.createComponent(LoginPage).componentInstance;
    component.form.setValue({ firstNames: '', lastNames: '', email: 'operador@bancohorizonte.com', password: 'segura123' });

    await component.submit();

    expect(component.error()).toEqual(jasmine.objectContaining({
      title: 'Aún falta confirmar tu correo'
    }));
  });
});
