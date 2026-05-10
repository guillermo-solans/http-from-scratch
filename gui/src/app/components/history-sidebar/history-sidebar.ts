import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { HistoryEntry } from '../../models/http-request.model';
import { methodColor, shortPath, statusClass } from '../../utils/format';

@Component({
  selector: 'app-history-sidebar',
  standalone: true,
  imports: [],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './history-sidebar.html',
  styleUrl: './history-sidebar.scss',
})
export class HistorySidebarComponent {
  readonly entries = input.required<HistoryEntry[]>();
  readonly selectedId = input<string | null>(null);

  readonly selectEntry = output<HistoryEntry>();
  readonly removeEntry = output<string>();
  readonly clearAll = output<void>();

  methodColor(method: string): string {
    return methodColor(method);
  }

  statusBadge(status: number): string {
    return `sb-${statusClass(status)}`;
  }

  shortPath(url: string): string {
    return shortPath(url);
  }

  relativeTime(timestamp: number): string {
    const diffMs = Date.now() - timestamp;
    const diffSec = Math.floor(diffMs / 1000);
    if (diffSec < 60) return `${diffSec}s ago`;
    const diffMin = Math.floor(diffSec / 60);
    if (diffMin < 60) return `${diffMin}m ago`;
    const diffH = Math.floor(diffMin / 60);
    if (diffH < 24) return `${diffH}h ago`;
    const diffD = Math.floor(diffH / 24);
    return `${diffD}d ago`;
  }

  onSelect(entry: HistoryEntry): void {
    this.selectEntry.emit(entry);
  }

  onRemove(event: MouseEvent, id: string): void {
    event.stopPropagation();
    this.removeEntry.emit(id);
  }

  onClear(): void {
    if (this.entries().length === 0) return;
    this.clearAll.emit();
  }
}
