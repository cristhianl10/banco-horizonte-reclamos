import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { DataService } from '../core/data.service';
import { NewComplaintPage } from './new-complaint.page';

describe('NewComplaintPage', () => {
  beforeEach(() => TestBed.configureTestingModule({
    imports: [NewComplaintPage], providers: [provideRouter([]), { provide: DataService, useValue: {
      catalogs: () => of({ channels: [], categories: [], subcategories: [], statuses: [], priorities: [], analysts: [] }),
      createComplaint: jasmine.createSpy('createComplaint').and.returnValue(of({ id: '1', code: 'BH-1', priority: 'Crítica', priorityScore: 7, priorityRules: [], slaDeadline: new Date().toISOString() }))
    }}]
  }));

  it('starts invalid because required customer and classification fields are empty', () => {
    const component = TestBed.createComponent(NewComplaintPage).componentInstance;
    expect(component.form.invalid).toBeTrue();
  });

  it('rejects short description, invalid email and negative amount', () => {
    const component = TestBed.createComponent(NewComplaintPage).componentInstance;
    component.form.patchValue({ customer: { documentNumber: '0912345678', firstNames: 'Ana', lastNames: 'Vega', email: 'invalido' }, receptionChannelId: 1, categoryId: 1, description: 'corta', affectedAmount: -1 });
    expect(component.form.controls.description.hasError('minlength')).toBeTrue();
    expect(component.form.controls.customer.controls.email.hasError('email')).toBeTrue();
    expect(component.form.controls.affectedAmount.hasError('min')).toBeTrue();
  });

  it('accepts a complete valid complaint', () => {
    const component = TestBed.createComponent(NewComplaintPage).componentInstance;
    component.form.patchValue({ customer: { documentNumber: '0912345678', firstNames: 'Ana', lastNames: 'Vega', email: 'ana@example.com', phone: '0990000000' }, receptionChannelId: 1, categoryId: 1, description: 'Transferencia debitada y no recibida por beneficiario.', affectedAmount: 600 });
    expect(component.form.valid).toBeTrue();
  });

  it('requires exactly ten numeric digits for cedula and phone', () => {
    const component = TestBed.createComponent(NewComplaintPage).componentInstance;
    const customer = component.form.controls.customer.controls;

    customer.documentNumber.setValue('09123A5678');
    customer.phone.setValue('099000000');
    expect(customer.documentNumber.hasError('documentFormat')).toBeTrue();
    expect(customer.phone.hasError('pattern')).toBeTrue();

    customer.documentNumber.setValue('0912345678');
    customer.phone.setValue('0990000000');
    expect(customer.documentNumber.valid).toBeTrue();
    expect(customer.phone.valid).toBeTrue();
  });
});
