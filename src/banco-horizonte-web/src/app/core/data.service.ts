import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { environment } from '../../environments/environment';
import { CatalogResponse, ComplaintDetail, ComplaintListItem, CreateComplaint, CreateComplaintResult, DashboardSummary, PagedResult } from './models';

const DEMO_CATALOGS: CatalogResponse = {
  channels: ['Correo electrónico', 'Llamada', 'Formulario web', 'Sucursal', 'Aplicación móvil'].map((name, index) => ({ id: index + 1, name })),
  categories: ['Transferencias', 'Tarjetas', 'Cobros', 'Canales digitales', 'Atención al cliente'].map((name, index) => ({ id: index + 1, name })),
  subcategories: [
    { id: 1, categoryId: 1, name: 'Transferencia no acreditada' }, { id: 2, categoryId: 2, name: 'Compra no reconocida' },
    { id: 3, categoryId: 3, name: 'Transacción no reconocida' }, { id: 4, categoryId: 4, name: 'Acceso o canal bloqueado' }
  ],
  statuses: ['Nuevo', 'En análisis', 'Resuelto', 'Rechazado'].map((name, index) => ({ id: index + 1, name })),
  priorities: ['Baja', 'Media', 'Alta', 'Crítica'].map((name, index) => ({ id: index + 1, name })),
  analysts: [{ id: 'a1', name: 'Ana Torres', email: 'ana@demo.com' }]
};

const receivedAt = new Date(Date.now() - 90 * 60_000).toISOString();
const deadline = new Date(Date.now() + 30 * 60_000).toISOString();
const DEMO_COMPLAINTS: ComplaintDetail[] = [{
  id: '11111111-1111-1111-1111-111111111111', code: 'BH-DEMO-0001', customer: 'Sofía Vega', document: 'CÉDULA 0912345678',
  email: 'sofia@example.com', phone: '0990000000', channel: 'Aplicación móvil', category: 'Tarjetas', subcategory: 'Compra no reconocida',
  description: 'Compra no reconocida por USD 780 reportada desde la aplicación móvil.', affectedAmount: 780,
  digitalChannelUnavailable: false, status: 'En análisis', statusId: 2, priority: 'Crítica', priorityScore: 7,
  assignee: 'Ana Torres', assigneeId: 'a1', receivedAt, slaDeadline: deadline, slaState: 'Próximo',
  priorityRules: [{ rule: 'Transacción o compra no reconocida', points: 4 }, { rule: 'Monto afectado igual o superior a USD 500', points: 3 }],
  timeline: [{ type: 'RECLAMO_CREADO', description: 'Prioridad crítica calculada automáticamente', actor: 'Operador demo', at: receivedAt }]
}];

@Injectable({ providedIn: 'root' })
export class DataService {
  private readonly http = inject(HttpClient);
  private readonly demoComplaints = [...DEMO_COMPLAINTS];

  dashboard(): Observable<DashboardSummary> {
    if (!environment.demoMode) return this.http.get<DashboardSummary>(`${environment.apiUrl}/dashboard/resumen`);
    const open = this.demoComplaints.filter(item => !['Resuelto', 'Rechazado'].includes(item.status));
    return of({ total: this.demoComplaints.length, open: open.length, resolved: this.demoComplaints.filter(item => item.status === 'Resuelto').length,
      critical: open.filter(item => item.priority === 'Crítica').length, nearDeadline: open.filter(item => item.slaState === 'Próximo').length,
      overdue: open.filter(item => item.slaState === 'Vencido').length, slaCompliance: 100,
      byStatus: this.groupCounts(this.demoComplaints.map(item => item.status)), byPriority: this.groupCounts(this.demoComplaints.map(item => item.priority)),
      byCategory: this.groupCounts(this.demoComplaints.map(item => item.category)), analystLoads: [{ analyst: 'Ana Torres', activeCases: open.length }],
      immediateAttention: open.filter(item => item.slaState !== 'En tiempo' || item.priority === 'Crítica') });
  }

  complaints(search = '', sla = '', filters: { statusId?: number; priorityId?: number; categoryId?: number; channelId?: number; assigneeId?: string } = {}): Observable<PagedResult<ComplaintListItem>> {
    if (!environment.demoMode) {
      let params = new HttpParams().set('page', 1).set('pageSize', 50);
      if (search) params = params.set('search', search);
      if (sla) params = params.set('sla', sla);
      for (const [key, value] of Object.entries(filters)) if (value != null && value !== '') params = params.set(key, value);
      return this.http.get<PagedResult<ComplaintListItem>>(`${environment.apiUrl}/reclamos`, { params });
    }
    const term = search.toLowerCase();
    const items = this.demoComplaints.filter(item => (!term || `${item.code} ${item.customer} ${item.category}`.toLowerCase().includes(term)) &&
      (!sla || (sla === 'overdue' ? item.slaState === 'Vencido' : item.slaState === 'Próximo')));
    return of({ items, page: 1, pageSize: 50, total: items.length });
  }

  complaint(id: string): Observable<ComplaintDetail> {
    if (!environment.demoMode) return this.http.get<ComplaintDetail>(`${environment.apiUrl}/reclamos/${id}`);
    const item = this.demoComplaints.find(complaint => complaint.id === id);
    if (!item) throw new Error('Reclamo no encontrado');
    return of(item);
  }

  catalogs(): Observable<CatalogResponse> {
    return environment.demoMode ? of(DEMO_CATALOGS) : this.http.get<CatalogResponse>(`${environment.apiUrl}/catalogos`);
  }

  createComplaint(value: CreateComplaint): Observable<CreateComplaintResult> {
    if (!environment.demoMode) return this.http.post<CreateComplaintResult>(`${environment.apiUrl}/reclamos`, value);
    const id = crypto.randomUUID();
    const code = `BH-${new Date().toISOString().slice(0, 10).replaceAll('-', '')}-${id.slice(0, 6).toUpperCase()}`;
    return of({ id, code, priorityScore: 0, priority: 'Baja', slaDeadline: new Date(Date.now() + 24 * 3_600_000).toISOString(), priorityRules: [] });
  }

  assign(id: string, analystId: string, reason: string): Observable<void> {
    if (!environment.demoMode) return this.http.post<void>(`${environment.apiUrl}/reclamos/${id}/asignaciones`, { analystId, reason });
    return of(undefined);
  }

  changeStatus(id: string, statusId: number, observation: string): Observable<void> {
    if (!environment.demoMode) return this.http.patch<void>(`${environment.apiUrl}/reclamos/${id}/estado`, { statusId, observation });
    return of(undefined);
  }

  take(id: string): Observable<void> {
    if (!environment.demoMode) return this.http.post<void>(`${environment.apiUrl}/reclamos/${id}/asumir`, {});
    return of(undefined);
  }

  private groupCounts(values: string[]): { label: string; value: number }[] {
    const counts = values.reduce<Record<string, number>>((result, value) => { result[value] = (result[value] ?? 0) + 1; return result; }, {});
    return Object.entries(counts).map(([label, value]) => ({ label, value }));
  }
}
