import { Injectable, signal } from '@angular/core';
import { HistoryEntry, HttpRequestSpec, HttpResponseRecord } from '../models/http-request.model';

const STORAGE_KEY = 'cats-gui-history-v1';
const MAX_ENTRIES = 30;

@Injectable({ providedIn: 'root' })
export class HistoryService {
  readonly entries = signal<HistoryEntry[]>(this.load());

  add(request: HttpRequestSpec, response: HttpResponseRecord): HistoryEntry {
    const entry: HistoryEntry = {
      id: this.generateId(),
      timestamp: Date.now(),
      request: this.cloneRequest(request),
      response,
    };
    const next = [entry, ...this.entries()].slice(0, MAX_ENTRIES);
    this.entries.set(next);
    this.persist(next);
    return entry;
  }

  remove(id: string): void {
    const next = this.entries().filter((e) => e.id !== id);
    this.entries.set(next);
    this.persist(next);
  }

  clear(): void {
    this.entries.set([]);
    this.persist([]);
  }

  private cloneRequest(request: HttpRequestSpec): HttpRequestSpec {
    return {
      method: request.method,
      url: request.url,
      headers: request.headers.map((h) => ({ ...h })),
      body: request.body,
    };
  }

  private load(): HistoryEntry[] {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw) as HistoryEntry[];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  private persist(entries: HistoryEntry[]): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(entries));
    } catch {
      // ignore quota errors
    }
  }

  private generateId(): string {
    if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
      return crypto.randomUUID();
    }
    return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
  }
}
