import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { DataService } from '../core/data.service';
import { NewComplaintPage } from './new-complaint.page';

describe('NewComplaintPage', () => {
  beforeEach(() => TestBed.configureTestingModule({
    imports: [NewComplaintPage], providers: [provideRouter([]), { provide: DataService, useValue: {
      catalogs: () => of({ channels: [], categories: [], subcategories: [], statuses: [], priorities: [], analysts: [] }),
      createComplaint: jasmine.createSpy('createComplaint').and.returnValue(of({ id: '1', code: 'BH-1', slaDeadline: new Date().toISOString() }))
    }}]
  }));

  it('starts invalid because required customer and classification fields are empty', () => {
    const component = TestBed.createComponent(NewComplaintPage).componentInstance;
    expect(component.form.invalid).toBeTrue();
  });

  it('rejects short description, invalid email and out-of-range severity', () => {
    const component = TestBed.createComponent(NewComplaintPage).componentInstance;
    component.form.patchValue({ customer: { documentNumber: '0912345678', firstNames: 'Ana', lastNames: 'Vega', email: 'invalido' }, receptionChannelId: 1, categoryId: 1, description: 'corta', impact: 4, urgency: 0 });
    expect(component.form.controls.description.hasError('minlength')).toBeTrue();
    expect(component.form.controls.customer.controls.email.hasError('email')).toBeTrue();
    expect(component.form.controls.impact.hasError('max')).toBeTrue();
    expect(component.form.controls.urgency.hasError('min')).toBeTrue();
  });

  it('accepts a complete valid complaint', () => {
    const component = TestBed.createComponent(NewComplaintPage).componentInstance;
    component.form.patchValue({ customer: { documentNumber: '0912345678', firstNames: 'Ana', lastNames: 'Vega', email: 'ana@example.com' }, receptionChannelId: 1, categoryId: 1, description: 'Transferencia debitada y no recibida por beneficiario.', impact: 3, urgency: 3 });
    expect(component.form.valid).toBeTrue();
  });
});
