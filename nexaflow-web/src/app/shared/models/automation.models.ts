export type TriggerType =
  | 'StockLow'
  | 'EmployeeAbsent'
  | 'LeaveRequestPending'
  | 'ScheduledDaily'
  | 'ScheduledWeekly';

export type WorkflowLogStatus = 'Success' | 'PartialSuccess' | 'Failed';

export type ActionType = 'SendEmail' | 'SendWhatsApp' | 'SendSlack' | 'CreateActivity';

export interface WorkflowRuleDto {
  id: string;
  name: string;
  description?: string;
  triggerType: TriggerType;
  triggerConfig: string; // raw JSON
  actionsConfig: string; // raw JSON array
  isActive: boolean;
  lastExecutedAt?: string;
  totalExecutions: number;
  successfulExecutions: number;
  createdAt: string;
}

export interface CreateWorkflowRuleDto {
  name: string;
  description?: string;
  triggerType: TriggerType;
  triggerConfig: string;
  actionsConfig: string;
}

export type UpdateWorkflowRuleDto = CreateWorkflowRuleDto;

export interface WorkflowLogDto {
  id: string;
  ruleName: string;
  executedAt: string;
  status: WorkflowLogStatus;
  details: string;
  triggerData?: string;
}

export interface WorkflowLogPageDto {
  items: WorkflowLogDto[];
  total: number;
  page: number;
  pageSize: number;
}

/** Real-time payload pushed over SignalR when a rule fires. */
export interface WorkflowNotification {
  ruleName: string;
  status: WorkflowLogStatus;
  summary: string;
  executedAt: string;
}

/** A single configurable action block in the no-code builder. */
export interface ActionBlock {
  id: string;
  type: ActionType;
  config: Record<string, unknown>;
}

export const TRIGGER_LABELS: Record<TriggerType, string> = {
  StockLow: 'Stock is low',
  EmployeeAbsent: 'Employee is absent',
  LeaveRequestPending: 'Leave request pending',
  ScheduledDaily: 'Every day at a time',
  ScheduledWeekly: 'Every week on a day',
};

export const ACTION_LABELS: Record<ActionType, string> = {
  SendEmail: 'Send Email',
  SendWhatsApp: 'Send WhatsApp',
  SendSlack: 'Send Slack message',
  CreateActivity: 'Create CRM activity',
};
