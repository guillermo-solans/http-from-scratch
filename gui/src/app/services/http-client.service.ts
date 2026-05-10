import { Injectable } from '@angular/core';
import {
  HttpMethod,
  HttpRequestSpec,
  HttpResponseRecord,
} from '../models/http-request.model';

@Injectable({ providedIn: 'root' })
export class HttpClientService {
  private static readonly METHODS_WITHOUT_BODY: ReadonlySet<HttpMethod> = new Set<HttpMethod>([
    'GET',
    'HEAD',
    'DELETE',
    'OPTIONS',
  ]);

  async send(spec: HttpRequestSpec): Promise<HttpResponseRecord> {
    const headers = new Headers();
    for (const pair of spec.headers) {
      if (!pair.enabled) continue;
      const key = pair.key.trim();
      if (!key) continue;
      headers.append(key, pair.value);
    }

    const init: RequestInit = {
      method: spec.method,
      headers,
      mode: 'cors',
      credentials: 'include',
      redirect: 'follow',
    };

    const hasBody = !HttpClientService.METHODS_WITHOUT_BODY.has(spec.method);
    if (hasBody && spec.body && spec.body.trim().length > 0) {
      if (!headers.has('Content-Type')) {
        headers.set('Content-Type', 'application/json');
      }
      init.body = spec.body;
    }

    const start = performance.now();
    try {
      const response = await fetch(spec.url, init);
      const responseHeaders: Array<[string, string]> = [];
      response.headers.forEach((value, key) => responseHeaders.push([key, value]));

      const contentType = response.headers.get('Content-Type') ?? '';
      const text = await response.text();
      const durationMs = Math.round(performance.now() - start);

      return {
        status: response.status,
        statusText: response.statusText,
        headers: responseHeaders,
        body: text,
        bodyContentType: contentType,
        durationMs,
        bytes: new Blob([text]).size,
        ok: response.ok,
      };
    } catch (err) {
      const durationMs = Math.round(performance.now() - start);
      const message = err instanceof Error ? err.message : String(err);
      return {
        status: 0,
        statusText: 'Network Error',
        headers: [],
        body: '',
        bodyContentType: '',
        durationMs,
        bytes: 0,
        ok: false,
        error: message,
      };
    }
  }
}
