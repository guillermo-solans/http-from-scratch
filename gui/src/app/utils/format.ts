export function tryFormatJson(text: string): { formatted: string; isJson: boolean } {
  if (!text) return { formatted: '', isJson: false };
  try {
    const parsed = JSON.parse(text);
    return { formatted: JSON.stringify(parsed, null, 2), isJson: true };
  } catch {
    return { formatted: text, isJson: false };
  }
}

export function statusClass(status: number): 'pending' | 'ok' | 'redirect' | 'client-err' | 'server-err' {
  if (status === 0) return 'pending';
  if (status >= 200 && status < 300) return 'ok';
  if (status >= 300 && status < 400) return 'redirect';
  if (status >= 400 && status < 500) return 'client-err';
  return 'server-err';
}

export function methodColor(method: string): string {
  switch (method.toUpperCase()) {
    case 'GET':
      return 'method-get';
    case 'POST':
      return 'method-post';
    case 'PUT':
      return 'method-put';
    case 'PATCH':
      return 'method-patch';
    case 'DELETE':
      return 'method-delete';
    case 'HEAD':
      return 'method-head';
    case 'OPTIONS':
      return 'method-options';
    default:
      return 'method-other';
  }
}

export function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / 1024 / 1024).toFixed(2)} MB`;
}

export function shortPath(url: string): string {
  try {
    const parsed = new URL(url);
    return `${parsed.pathname}${parsed.search}`;
  } catch {
    return url;
  }
}
