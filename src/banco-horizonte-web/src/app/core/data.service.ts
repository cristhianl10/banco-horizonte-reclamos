import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable, of } from 'rxjs';
import { environment } from '../../environments/environment';
import { CatalogResponse, ComplaintDetail, ComplaintListItem, CreateComplaint, DashboardSummary, PagedResult } from './models';

const now = Date.now();
const hours = (value: number) => new Date(now + value * 3_600_000).toISOString();

const DEMO_COMPLAINTS: ComplaintDetail[] = [
  { id: '11111111-1111-1111-1111-111111111111', code: 'BH-20260811-A7F321', customer: 'Sofía Vega', document: 'CÉDULA 0912345678', email: 'sofia.vega@example.com', phone: '099 420 1890', channel: 'Aplicación móvil', category: 'Transferencias', subcategory: 'Transferencia no recibida', description: 'Transferencia interbancaria debitada de la cuenta, pero el beneficiario todavía no refleja los fondos.', impact: 3, urgency: 3, status: 'En análisis', statusId: 3, priority: 'Crítica', priorityScore: 9, assignee: 'Luis Mendoza', assigneeId: 'a1', receivedAt: hours(-3), slaDeadline: hours(-0.4), slaState: 'Vencido', timeline: [
    { type: 'ESTADO_CAMBIADO', description: 'El caso pasó a En análisis', actor: 'Luis Mendoza', at: hours(-2) },
    { type: 'RECLAMO_ASIGNADO', description: 'Responsable: Luis Mendoza', actor: 'María Andrade', at: hours(-2.5) },
    { type: 'RECLAMO_CREADO', description: 'Prioridad crítica calculada automáticamente', actor: 'Carolina Ponce', at: hours(-3) },
  ]},
  { id: '22222222-2222-2222-2222-222222222222', code: 'BH-20260811-D92AC0', customer: 'Daniel Ortiz', document: 'CÉDULA 0923456789', email: 'daniel.ortiz@example.com', channel: 'Llamada', category: 'Tarjetas', subcategory: 'Consumo no reconocido', description: 'Cliente reporta un consumo internacional que no reconoce y solicita bloqueo preventivo de su tarjeta.', impact: 3, urgency: 3, status: 'Asignado', statusId: 2, priority: 'Crítica', priorityScore: 9, assignee: 'Ana Torres', assigneeId: 'a2', receivedAt: hours(-1.2), slaDeadline: hours(0.8), slaState: 'Próximo', timeline: [
    { type: 'RECLAMO_ASIGNADO', description: 'Responsable: Ana Torres', actor: 'María Andrade', at: hours(-1) },
    { type: 'RECLAMO_CREADO', description: 'Prioridad crítica calculada automáticamente', actor: 'Carolina Ponce', at: hours(-1.2) },
  ]},
  { id: '33333333-3333-3333-3333-333333333333', code: 'BH-20260810-02BB91', customer: 'Elena Castro', document: 'CÉDULA 0934567890', channel: 'Correo electrónico', category: 'Cobros', subcategory: 'Cobro duplicado', description: 'Se visualizan dos débitos correspondientes a una misma compra realizada con tarjeta de débito.', impact: 2, urgency: 3, status: 'En espera de cliente', statusId: 4, priority: 'Alta', priorityScore: 6, assignee: 'Luis Mendoza', assigneeId: 'a1', receivedAt: hours(-8), slaDeadline: hours(2.5), slaState: 'Próximo', timeline: [
    { type: 'ESTADO_CAMBIADO', description: 'Pendiente de comprobante del cliente', actor: 'Luis Mendoza', at: hours(-4) },
    { type: 'RECLAMO_CREADO', description: 'Prioridad alta calculada automáticamente', actor: 'Carolina Ponce', at: hours(-8) },
  ]},
  { id: '44444444-4444-4444-4444-444444444444', code: 'BH-20260810-8AD120', customer: 'Miguel León', document: 'PASAPORTE A0943212', channel: 'Formulario web', category: 'Canales digitales', subcategory: 'No puede iniciar sesión', description: 'El usuario no logra acceder a la banca web después de restablecer correctamente su contraseña.', impact: 2, urgency: 2, status: 'Nuevo', statusId: 1, priority: 'Media', priorityScore: 4, assignee: null, receivedAt: hours(-5), slaDeadline: hours(18), slaState: 'En tiempo', timeline: [{ type: 'RECLAMO_CREADO', description: 'Prioridad media calculada automáticamente', actor: 'Carolina Ponce', at: hours(-5) }]},
  { id: '55555555-5555-5555-5555-555555555555', code: 'BH-20260809-C41E89', customer: 'Paola Ruiz', document: 'CÉDULA 0945678901', channel: 'Sucursal', category: 'Atención al cliente', subcategory: 'Demora en atención', description: 'La cliente reporta una espera mayor a noventa minutos sin recibir información sobre su turno.', impact: 1, urgency: 1, status: 'Resuelto', statusId: 5, priority: 'Baja', priorityScore: 0, assignee: 'Ana Torres', assigneeId: 'a2', receivedAt: hours(-40), slaDeadline: hours(30), slaState: 'En tiempo', timeline: [{ type: 'ESTADO_CAMBIADO', description: 'Caso resuelto con disculpa y seguimiento', actor: 'Ana Torres', at: hours(-10) }]},
];

const DEMO_CATALOGS: CatalogResponse = {
  channels: ['Correo electrónico', 'Llamada', 'Formulario web', 'Sucursal', 'Aplicación móvil'].map((name, i) => ({ id: i + 1, name })),
  categories: ['Transferencias', 'Tarjetas', 'Cobros', 'Canales digitales', 'Atención al cliente'].map((name, i) => ({ id: i + 1, name })),
  subcategories: ['Transferencia no recibida', 'Transferencia duplicada', 'Tarjeta bloqueada', 'Consumo no reconocido', 'Cobro duplicado', 'Débito no autorizado', 'No puede iniciar sesión', 'Operación no disponible', 'Demora en atención', 'Información incorrecta'].map((name, i) => ({ id: i + 1, name })),
  statuses: ['Nuevo', 'Asignado', 'En análisis', 'En espera de cliente', 'Resuelto', 'Cerrado', 'Cancelado'].map((name, i) => ({ id: i + 1, name })),
  priorities: ['Baja', 'Media', 'Alta', 'Crítica'].map((name, i) => ({ id: i + 1, name })),
  analysts: [{ id: 'a1', name: 'Luis Mendoza', email: 'luis@demo.com' }, { id: 'a2', name: 'Ana Torres', email: 'ana@demo.com' }],
};

@Injectable({ providedIn: 'root' })
export class DataService {
  private readonly http = inject(HttpClient);
  private demoComplaints = [...DEMO_COMPLAINTS];

  dashboard(): Observable<DashboardSummary> {
    if (!environment.demoMode) return this.http.get<DashboardSummary>(`${environment.apiUrl}/dashboard/resumen`);
    const open = this.demoComplaints.filter(x => !['Cerrado', 'Cancelado'].includes(x.status));
    return of({ open: open.length, critical: open.filter(x => x.priority === 'Crítica').length,
      nearDeadline: open.filter(x => x.slaState === 'Próximo').length, overdue: open.filter(x => x.slaState === 'Vencido').length,
      slaCompliance: 91.7,
      byStatus: this.groupCounts(this.demoComplaints.map(x => x.status)),
      byCategory: this.groupCounts(this.demoComplaints.map(x => x.category)),
      analystLoads: [{ analyst: 'Luis Mendoza', activeCases: 8 }, { analyst: 'Ana Torres', activeCases: 6 }, { analyst: 'José Zamora', activeCases: 4 }],
      immediateAttention: open.filter(x => x.slaState !== 'En tiempo' || x.priority === 'Crítica').slice(0, 8) });
  }

  complaints(search = '', sla = ''): Observable<PagedResult<ComplaintListItem>> {
    if (!environment.demoMode) {
      let params = new HttpParams().set('page', 1).set('pageSize', 50);
      if (search) params = params.set('search', search); if (sla) params = params.set('sla', sla);
      return this.http.get<PagedResult<ComplaintListItem>>(`${environment.apiUrl}/reclamos`, { params });
    }
    const term = search.toLowerCase();
    const items = this.demoComplaints.filter(x => (!term || `${x.code} ${x.customer} ${x.category}`.toLowerCase().includes(term)) &&
      (!sla || (sla === 'overdue' ? x.slaState === 'Vencido' : x.slaState === 'Próximo')));
    return of({ items, page: 1, pageSize: 50, total: items.length });
  }

  complaint(id: string): Observable<ComplaintDetail> {
    if (!environment.demoMode) return this.http.get<ComplaintDetail>(`${environment.apiUrl}/reclamos/${id}`);
    const item = this.demoComplaints.find(x => x.id === id);
    if (!item) throw new Error('Reclamo no encontrado'); return of(item);
  }

  catalogs(): Observable<CatalogResponse> { return environment.demoMode ? of(DEMO_CATALOGS) : this.http.get<CatalogResponse>(`${environment.apiUrl}/catalogos`); }

  createComplaint(value: CreateComplaint): Observable<{ id: string; code: string; slaDeadline: string }> {
    if (!environment.demoMode) return this.http.post<{ id: string; code: string; slaDeadline: string }>(`${environment.apiUrl}/reclamos`, value);
    const id = crypto.randomUUID(); const code = `BH-${new Date().toISOString().slice(0,10).replaceAll('-', '')}-${id.slice(0,6).toUpperCase()}`;
    const category = DEMO_CATALOGS.categories.find(x => x.id === value.categoryId)?.name ?? 'Sin categoría';
    this.demoComplaints.unshift({ id, code, customer: `${value.customer.firstNames} ${value.customer.lastNames}`, document: `${value.customer.documentType} ${value.customer.documentNumber}`,
      email: value.customer.email, phone: value.customer.phone, channel: DEMO_CATALOGS.channels.find(x => x.id === value.receptionChannelId)?.name ?? '',
      category, description: value.description, impact: value.impact, urgency: value.urgency, status: 'Nuevo', statusId: 1,
      priority: value.impact + value.urgency >= 6 ? 'Crítica' : value.impact + value.urgency >= 5 ? 'Alta' : 'Media', priorityScore: value.impact + value.urgency,
      assignee: null, receivedAt: new Date().toISOString(), slaDeadline: hours(8), slaState: 'En tiempo', timeline: [{ type: 'RECLAMO_CREADO', description: 'Caso registrado en modo demostración', actor: 'María Andrade', at: new Date().toISOString() }] });
    return of({ id, code, slaDeadline: hours(8) });
  }

  assign(id: string, analystId: string, reason: string): Observable<void> {
    if (!environment.demoMode) return this.http.post<void>(`${environment.apiUrl}/reclamos/${id}/asignaciones`, { analystId, reason });
    const item = this.demoComplaints.find(x => x.id === id); const analyst = DEMO_CATALOGS.analysts.find(x => x.id === analystId);
    if (item && analyst) { item.assignee = analyst.name; item.assigneeId = analyst.id; item.timeline.unshift({ type: 'RECLAMO_ASIGNADO', description: `Responsable: ${analyst.name}`, actor: 'María Andrade', at: new Date().toISOString() }); }
    return of(undefined);
  }

  changeStatus(id: string, statusId: number, observation: string): Observable<void> {
    if (!environment.demoMode) return this.http.patch<void>(`${environment.apiUrl}/reclamos/${id}/estado`, { statusId, observation });
    const item = this.demoComplaints.find(x => x.id === id); const status = DEMO_CATALOGS.statuses.find(x => x.id === statusId);
    if (item && status) { item.status = status.name; item.statusId = statusId; item.timeline.unshift({ type: 'ESTADO_CAMBIADO', description: observation || `Estado: ${status.name}`, actor: 'María Andrade', at: new Date().toISOString() }); }
    return of(undefined);
  }

  take(id: string): Observable<void> {
    if (!environment.demoMode) return this.http.post<void>(`${environment.apiUrl}/reclamos/${id}/asumir`, {});
    const item = this.demoComplaints.find(x => x.id === id);
    if (item) {
      item.assignee = 'María Andrade'; item.assigneeId = '00000000-0000-0000-0000-000000000001';
      item.timeline.unshift({ type: 'RECLAMO_ASIGNADO', description: 'Caso asumido por el usuario actual', actor: 'María Andrade', at: new Date().toISOString() });
    }
    return of(undefined);
  }

  private groupCounts(values: string[]): { label: string; value: number }[] {
    const counts = values.reduce<Record<string, number>>((result, value) => {
      result[value] = (result[value] ?? 0) + 1; return result;
    }, {});
    return Object.entries(counts).map(([label, value]) => ({ label, value }));
  }
}
