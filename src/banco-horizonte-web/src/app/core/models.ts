export type SlaState = 'Vencido' | 'Próximo' | 'En tiempo';

export interface ComplaintListItem {
  id: string; code: string; customer: string; category: string; status: string; priority: string;
  assignee: string | null; receivedAt: string; slaDeadline: string; slaState: SlaState;
}

export interface TimelineItem { type: string; description: string; actor: string; at: string; }
export interface ComplaintDetail extends ComplaintListItem {
  document: string; email?: string; phone?: string; channel: string; subcategory?: string;
  description: string; impact: number; urgency: number; statusId: number; priorityScore: number;
  assigneeId?: string; timeline: TimelineItem[];
}

export interface MetricSlice { label: string; value: number; }
export interface AnalystLoad { analyst: string; activeCases: number; }
export interface DashboardSummary {
  open: number; critical: number; nearDeadline: number; overdue: number; slaCompliance: number;
  byStatus: MetricSlice[]; byCategory: MetricSlice[]; analystLoads: AnalystLoad[];
  immediateAttention: ComplaintListItem[];
}

export interface CatalogItem { id: number; name: string; }
export interface SubcategoryItem extends CatalogItem { categoryId?: number; }
export interface AnalystItem { id: string; name: string; email: string; }
export interface CatalogResponse {
  channels: CatalogItem[]; categories: CatalogItem[]; subcategories: SubcategoryItem[];
  statuses: CatalogItem[]; priorities: CatalogItem[]; analysts: AnalystItem[];
}

export interface CurrentUser { id: string; name: string; email: string; roles: string[]; }
export interface PagedResult<T> { items: T[]; page: number; pageSize: number; total: number; }

export interface CreateComplaint {
  customer: { documentType: string; documentNumber: string; firstNames: string; lastNames: string; email?: string; phone?: string };
  receptionChannelId: number; categoryId: number; subcategoryId?: number; description: string;
  impact: number; urgency: number; confirmPossibleDuplicate?: boolean;
}
