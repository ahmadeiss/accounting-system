// Shared API / error types

export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  traceId?: string;
}

export interface ApiError {
  status: number;
  message: string;
}

