export type IntegrationType = 'WhatsApp' | 'Gmail' | 'Slack' | 'GoogleSheets';

export interface IntegrationDto {
  type: IntegrationType;
  isEnabled: boolean;
  isConfigured: boolean;
  lastTestedAt?: string;
  lastTestSuccess?: boolean;
}

export interface UpsertIntegrationDto {
  isEnabled: boolean;
  config: Record<string, string>;
}

export interface IntegrationTestResult {
  success: boolean;
  message: string;
}

/** A single credential field rendered in the configure dialog. */
export interface IntegrationField {
  key: string; // matches the backend config key (camelCase)
  label: string;
  secret?: boolean; // password input; blank means "keep existing"
  textarea?: boolean; // multi-line (e.g. the Google service-account JSON)
  hint?: string;
}

/** UI metadata for each integration: display name, icon, and its credential fields. */
export const INTEGRATION_META: Record<
  IntegrationType,
  { name: string; icon: string; fields: IntegrationField[] }
> = {
  WhatsApp: {
    name: 'WhatsApp Business',
    icon: 'chat',
    fields: [
      { key: 'accessToken', label: 'Access Token', secret: true },
      { key: 'phoneNumberId', label: 'Phone Number ID' },
    ],
  },
  Gmail: {
    name: 'Email (Gmail)',
    icon: 'mail',
    fields: [
      { key: 'email', label: 'Gmail Address' },
      {
        key: 'appPassword',
        label: 'App Password',
        secret: true,
        hint: 'Google Account → Security → App passwords',
      },
      { key: 'fromName', label: 'From Name (optional)' },
    ],
  },
  Slack: {
    name: 'Slack',
    icon: 'tag',
    fields: [
      { key: 'webhookUrl', label: 'Webhook URL', secret: true },
      { key: 'defaultChannel', label: 'Default Channel (optional)' },
    ],
  },
  GoogleSheets: {
    name: 'Google Sheets',
    icon: 'table_chart',
    fields: [
      {
        key: 'serviceAccountJson',
        label: 'Service Account JSON',
        secret: true,
        textarea: true,
        hint: 'Paste the full service-account key JSON',
      },
      { key: 'spreadsheetId', label: 'Spreadsheet ID' },
      { key: 'sheetName', label: 'Sheet Name (optional)' },
    ],
  },
};

export const INTEGRATION_ORDER: IntegrationType[] = ['WhatsApp', 'Gmail', 'Slack', 'GoogleSheets'];
