import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { HttpResponseRecord } from '../../models/http-request.model';
import { formatBytes, statusClass, tryFormatJson } from '../../utils/format';

type Tab = 'body' | 'headers' | 'raw';

@Component({
  selector: 'app-response-viewer',
  standalone: true,
  imports: [],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './response-viewer.html',
  styleUrl: './response-viewer.scss',
})
export class ResponseViewerComponent {
  readonly response = input<HttpResponseRecord | null>(null);
  readonly sending = input<boolean>(false);

  readonly activeTab = signal<Tab>('body');

  readonly statusClass = computed(() => {
    const r = this.response();
    return r ? statusClass(r.status) : 'pending';
  });

  readonly bodyView = computed(() => {
    const r = this.response();
    if (!r) return { formatted: '', isJson: false };
    if (r.bodyContentType.toLowerCase().includes('json')) {
      return tryFormatJson(r.body);
    }
    return tryFormatJson(r.body);
  });

  readonly bytesLabel = computed(() => {
    const r = this.response();
    return r ? formatBytes(r.bytes) : '';
  });

  setTab(tab: Tab): void {
    this.activeTab.set(tab);
  }

  copyBody(): void {
    const r = this.response();
    if (!r) return;
    void navigator.clipboard?.writeText(r.body);
  }
}
