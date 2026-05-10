import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HeaderPair, HttpMethod, HttpRequestSpec } from '../../models/http-request.model';
import { QUICK_ACTIONS, QuickAction } from '../../models/quick-action.model';
import { methodColor } from '../../utils/format';

const METHODS: HttpMethod[] = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'HEAD', 'OPTIONS'];
const METHODS_WITH_BODY: ReadonlySet<HttpMethod> = new Set(['POST', 'PUT', 'PATCH']);

@Component({
  selector: 'app-request-builder',
  standalone: true,
  imports: [FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './request-builder.html',
  styleUrl: './request-builder.scss',
})
export class RequestBuilderComponent {
  readonly request = input.required<HttpRequestSpec>();
  readonly sending = input<boolean>(false);

  readonly requestChange = output<HttpRequestSpec>();
  readonly send = output<void>();

  readonly methods = METHODS;
  readonly quickActions: ReadonlyArray<QuickAction> = QUICK_ACTIONS;

  readonly showBody = computed(() => METHODS_WITH_BODY.has(this.request().method));

  methodColor(method: string): string {
    return methodColor(method);
  }

  onMethodChange(method: HttpMethod): void {
    this.emit({ ...this.request(), method });
  }

  onUrlChange(url: string): void {
    this.emit({ ...this.request(), url });
  }

  onBodyChange(body: string): void {
    this.emit({ ...this.request(), body });
  }

  onHeaderKeyChange(index: number, value: string): void {
    this.updateHeader(index, { key: value });
  }

  onHeaderValueChange(index: number, value: string): void {
    this.updateHeader(index, { value });
  }

  onHeaderEnabledChange(index: number, value: boolean): void {
    this.updateHeader(index, { enabled: value });
  }

  addHeader(): void {
    const current = this.request();
    this.emit({
      ...current,
      headers: [...current.headers, { key: '', value: '', enabled: true }],
    });
  }

  removeHeader(index: number): void {
    const current = this.request();
    const headers = current.headers.filter((_, i) => i !== index);
    this.emit({ ...current, headers });
  }

  formatBody(): void {
    const current = this.request();
    if (!current.body.trim()) return;
    try {
      const parsed = JSON.parse(current.body);
      this.emit({ ...current, body: JSON.stringify(parsed, null, 2) });
    } catch {
      // not JSON, leave as is
    }
  }

  applyQuickAction(action: QuickAction): void {
    const current = this.request();
    const base = this.deriveBase(current.url);
    const url = `${base}${action.path}`;
    const headers: HeaderPair[] = [...current.headers];
    if (action.body && !headers.some((h) => h.key.toLowerCase() === 'content-type' && h.enabled)) {
      headers.push({ key: 'Content-Type', value: 'application/json', enabled: true });
    }
    this.emit({
      method: action.method,
      url,
      headers,
      body: action.body ?? '',
    });
  }

  onSubmit(): void {
    this.send.emit();
  }

  private updateHeader(index: number, patch: Partial<HeaderPair>): void {
    const current = this.request();
    const headers = current.headers.map((h, i) => (i === index ? { ...h, ...patch } : h));
    this.emit({ ...current, headers });
  }

  private deriveBase(url: string): string {
    try {
      const parsed = new URL(url);
      return `${parsed.protocol}//${parsed.host}`;
    } catch {
      return 'http://localhost:8080';
    }
  }

  private emit(next: HttpRequestSpec): void {
    this.requestChange.emit(next);
  }

  trackByIndex = (index: number): number => index;
}
