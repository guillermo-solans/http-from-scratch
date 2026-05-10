export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'DELETE' | 'HEAD' | 'PATCH' | 'OPTIONS';

export interface HeaderPair {
  key: string;
  value: string;
  enabled: boolean;
}

export interface HttpRequestSpec {
  method: HttpMethod;
  url: string;
  headers: HeaderPair[];
  body: string;
}

export interface HttpResponseRecord {
  status: number;
  statusText: string;
  headers: Array<[string, string]>;
  body: string;
  bodyContentType: string;
  durationMs: number;
  bytes: number;
  ok: boolean;
  error?: string;
}

export interface HistoryEntry {
  id: string;
  timestamp: number;
  request: HttpRequestSpec;
  response: HttpResponseRecord;
}
