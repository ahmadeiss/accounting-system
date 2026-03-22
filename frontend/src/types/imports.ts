// ─── Row-level result ─────────────────────────────────────────────────────────

export type ImportRowStatus = 'Success' | 'Failed' | 'Skipped';

export interface ImportRowResult {
  rowNumber: number;
  status: ImportRowStatus;
  errorMessage: string | null;
  rawData: string;
}

// ─── Job-level result ─────────────────────────────────────────────────────────

export type ImportJobStatus =
  | 'Pending'
  | 'Processing'
  | 'Completed'
  | 'PartialSuccess'
  | 'Failed';

export interface ImportResult {
  jobId: string | null;
  isDryRun: boolean;
  status: ImportJobStatus;
  totalRows: number;
  successRows: number;
  failedRows: number;
  skippedRows: number;
  errorSummary: string | null;
  rows: ImportRowResult[];
  hasErrors: boolean;
}

