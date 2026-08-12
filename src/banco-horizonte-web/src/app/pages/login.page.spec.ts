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
    component.form.setValue({ email: 'correo-invalido', password: '123' });
    expect(component.form.invalid).toBeTrue();
    expect(component.form.controls.email.hasError('email')).toBeTrue();
    expect(component.form.controls.password.hasError('minlength')).toBeTrue();
  });

  it('accepts a valid institutional login form', () => {
    const component = TestBed.createComponent(LoginPage).componentInstance;
    component.form.setValue({ email: 'supervisor@bancohorizonte.com', password: 'segura123' });
    expect(component.form.valid).toBeTrue();
  });
});
