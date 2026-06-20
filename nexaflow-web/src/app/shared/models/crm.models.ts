export type CustomerStatus = 'Active' | 'Inactive' | 'Lead' | 'Churned';

export type LeadStage =
  | 'Prospect'
  | 'Qualified'
  | 'Proposal'
  | 'Negotiation'
  | 'Won'
  | 'Lost';

export interface CustomerDto {
  id: string;
  name: string;
  email?: string;
  phone?: string;
  company?: string;
  status: CustomerStatus;
  assignedToName?: string;
  leadsCount: number;
  createdAt: string;
}

export interface CreateCustomerDto {
  name: string;
  email?: string;
  phone?: string;
  company?: string;
  notes?: string;
  assignedToId?: string;
}

export interface UpdateCustomerDto {
  name: string;
  email?: string;
  phone?: string;
  company?: string;
  notes?: string;
  status: CustomerStatus;
  assignedToId?: string;
}

export interface LeadDto {
  id: string;
  title: string;
  value: number;
  stage: LeadStage;
  customerId: string;
  customerName: string;
  assignedToName?: string;
  expectedCloseDate?: string;
}

export interface CreateLeadDto {
  title: string;
  value: number;
  customerId: string;
  assignedToId?: string;
  expectedCloseDate?: string;
}

export interface UpdateLeadStageDto {
  stage: LeadStage;
}

/** 'StatusChange' is system-generated; users may only create the first four. */
export type ActivityType = 'Note' | 'Call' | 'Email' | 'Meeting' | 'StatusChange';

export type ManualActivityType = Exclude<ActivityType, 'StatusChange'>;

export interface ActivityDto {
  id: string;
  customerId: string;
  type: ActivityType;
  content: string;
  createdByName?: string;
  createdAt: string;
}

export interface CreateActivityDto {
  type: ManualActivityType;
  content: string;
}
