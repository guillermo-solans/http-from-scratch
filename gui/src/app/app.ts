import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RequestBuilderComponent } from './components/request-builder/request-builder';
import { ResponseViewerComponent } from './components/response-viewer/response-viewer';
import { HistorySidebarComponent } from './components/history-sidebar/history-sidebar';
import { HttpClientService } from './services/http-client.service';
import { HistoryService } from './services/history.service';
import {
  HistoryEntry,
  HttpRequestSpec,
  HttpResponseRecord,
} from './models/http-request.model';

const DEFAULT_BASE = (() => {
  if (typeof window === 'undefined') return 'http://localhost:8080';
  const { protocol, host } = window.location;
  if (protocol === 'http:' || protocol === 'https:') {
    return `${protocol}//${host}`;
  }
  return 'http://localhost:8080';
})();

const INITIAL_REQUEST: HttpRequestSpec = {
  method: 'GET',
  url: `${DEFAULT_BASE}/cats`,
  headers: [{ key: 'Accept', value: 'application/json', enabled: true }],
  body: '',
};

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RequestBuilderComponent, ResponseViewerComponent, HistorySidebarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly http = inject(HttpClientService);
  private readonly historyStore = inject(HistoryService);

  readonly request = signal<HttpRequestSpec>(INITIAL_REQUEST);
  readonly response = signal<HttpResponseRecord | null>(null);
  readonly sending = signal(false);
  readonly selectedHistoryId = signal<string | null>(null);
  readonly sidebarOpen = signal(true);

  readonly history = this.historyStore.entries;
  readonly historyCount = computed(() => this.history().length);

  onRequestChange(next: HttpRequestSpec): void {
    this.request.set(next);
  }

  async onSend(): Promise<void> {
    if (this.sending()) return;
    this.sending.set(true);
    this.response.set(null);
    try {
      const current = this.request();
      const result = await this.http.send(current);
      this.response.set(result);
      const entry = this.historyStore.add(current, result);
      this.selectedHistoryId.set(entry.id);
    } finally {
      this.sending.set(false);
    }
  }

  onSelectHistory(entry: HistoryEntry): void {
    this.request.set({
      ...entry.request,
      headers: entry.request.headers.map((h) => ({ ...h })),
    });
    this.response.set(entry.response);
    this.selectedHistoryId.set(entry.id);
  }

  onRemoveHistory(id: string): void {
    this.historyStore.remove(id);
    if (this.selectedHistoryId() === id) {
      this.selectedHistoryId.set(null);
    }
  }

  onClearHistory(): void {
    this.historyStore.clear();
    this.selectedHistoryId.set(null);
  }

  toggleSidebar(): void {
    this.sidebarOpen.update((v) => !v);
  }
}
