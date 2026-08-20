// shared/ui/secure-file-viewer/secure-file-viewer.ts

import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  Input,
  OnChanges,
  OnDestroy,
  SimpleChanges,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { Observable } from 'rxjs';
import { NgxExtendedPdfViewerModule } from 'ngx-extended-pdf-viewer';
import { LucideAngularModule, X, TriangleAlert } from 'lucide-angular';

import { LocaleStore } from '../../../core/i18n/locale.store';

/**
 * Labels used by the inline (embedded) viewer mode.
 * Passed in by the consuming feature via the [labels] input.
 */
export interface SecureFileViewerLabels {
  zoomIn: string;
  zoomOut: string;
  rotate: string;
  reset: string;
  download: string;
  documentPreview: string;
  unsupported: string;
}

type FileKind = 'pdf' | 'image' | 'unsupported';

/**
 * Reusable modal document viewer. Framework-agnostic about WHERE the file comes
 * from — the caller supplies a `fileFetcher` function that returns an
 * Observable<Blob> for a given document id. This keeps shared/ui decoupled from
 * any specific feature's data-access service (e.g. applicant-documents'
 * DocumentsApi), so both this feature and Ali's human-review feature can reuse
 * it by passing in their own fetch function, without shared/ui depending on
 * either feature.
 *
 * Renders as an object URL, never a raw API URL — see business rule 5 in the
 * feature brief.
 */
@Component({
  selector: 'app-secure-file-viewer',
  standalone: true,
  imports: [CommonModule, NgxExtendedPdfViewerModule, LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './secure-file-viewer.html',
  styleUrl: './secure-file-viewer.scss',
})
export class SecureFileViewer implements OnChanges, OnDestroy {
  // ── Modal mode inputs (used by required-documents-page) ──
  /** Provided by the consuming feature — e.g. (id) => this.documentsApi.downloadFile(id) */
  @Input() fileFetcher?: (documentId: string) => Observable<Blob>;

  // ── Inline (embedded) mode inputs (used by review-details-page) ──
  /** Pre-fetched object URL. When provided, the viewer renders inline instead of as a modal. */
  @Input() sourceUrl?: string;
  /** MIME type of the source (e.g. 'application/pdf', 'image/jpeg'). Used in inline mode. */
  @Input() mimeType?: string;
  /** Localised labels for the inline viewer. */
  @Input() labels?: SecureFileViewerLabels;

  protected readonly XIcon = X;
  protected readonly TriangleAlertIcon = TriangleAlert;

  protected readonly locale = inject(LocaleStore);

  // ── Modal mode state ──
  protected readonly isOpen = signal(false);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly objectUrl = signal<string | null>(null);
  protected readonly fileKind = signal<FileKind>('unsupported');
  protected readonly fileName = signal('');

  // ── Inline mode state (derived from inputs) ──
  protected readonly inlineMode = signal(false);
  protected readonly inlineFileKind = signal<FileKind>('unsupported');

  @ViewChild('dialogEl') private dialogEl?: ElementRef<HTMLElement>;

  /**
   * Fires whenever the close button enters/leaves the DOM (i.e. exactly when the
   * modal opens/closes, since it's inside the @if block). Used to move focus into
   * the dialog on open and restore it to whatever triggered the modal on close —
   * standard modal accessibility behavior, not optional polish.
   */
  private previouslyFocusedElement: HTMLElement | null = null;

  @ViewChild('closeButton') private set closeButtonRef(ref: ElementRef<HTMLButtonElement> | undefined) {
    if (ref) {
      this.previouslyFocusedElement = document.activeElement as HTMLElement;
      ref.nativeElement.focus();
    } else if (this.previouslyFocusedElement) {
      this.previouslyFocusedElement.focus();
      this.previouslyFocusedElement = null;
    }
  }

  // ── Lifecycle ──

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['sourceUrl'] || changes['mimeType']) {
      const url = this.sourceUrl;
      this.inlineMode.set(!!url);
      if (url) {
        this.inlineFileKind.set(this.detectFileKindFromMime(this.mimeType));
      }
    }
  }

  /** Call from a parent component to open the viewer for a given document (modal mode). */
  open(documentId: string, fileName: string): void {
    if (!this.fileFetcher) {
      console.error('SecureFileViewer: fileFetcher is required for modal mode.');
      return;
    }
    this.isOpen.set(true);
    this.fileName.set(fileName);
    this.fileKind.set(this.detectFileKind(fileName));
    this.loading.set(true);
    this.error.set(null);
    document.body.style.overflow = 'hidden'; // lock background scroll while modal is open

    this.fileFetcher(documentId).subscribe({
      next: (blob) => {
        this.objectUrl.set(URL.createObjectURL(blob));
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.locale.t('common.fileViewer.error'));
        this.loading.set(false);
      },
    });
  }

  close(): void {
    this.revokeCurrentUrl();
    this.isOpen.set(false);
    this.error.set(null);
    document.body.style.overflow = '';
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    if (this.isOpen()) this.close();
  }

  /** Keeps Tab/Shift+Tab cycling within the dialog instead of leaking into the page behind it. */
  @HostListener('document:keydown.tab', ['$event'])
  protected onTab(event: KeyboardEvent): void {
    this.trapFocus(event, false);
  }

  @HostListener('document:keydown.shift.tab', ['$event'])
  protected onShiftTab(event: KeyboardEvent): void {
    this.trapFocus(event, true);
  }

  private trapFocus(event: KeyboardEvent, reverse: boolean): void {
    if (!this.isOpen() || !this.dialogEl) return;

    const focusable = this.dialogEl.nativeElement.querySelectorAll<HTMLElement>(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])',
    );
    if (focusable.length === 0) return;

    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    const active = document.activeElement;

    if (reverse && active === first) {
      event.preventDefault();
      last.focus();
    } else if (!reverse && active === last) {
      event.preventDefault();
      first.focus();
    }
  }

  ngOnDestroy(): void {
    this.revokeCurrentUrl();
    document.body.style.overflow = '';
  }

  private revokeCurrentUrl(): void {
    const url = this.objectUrl();
    if (url) {
      URL.revokeObjectURL(url);
      this.objectUrl.set(null);
    }
  }

  private detectFileKind(fileName: string): FileKind {
    const extension = fileName.split('.').pop()?.toLowerCase() ?? '';
    if (extension === 'pdf') return 'pdf';
    if (['jpg', 'jpeg', 'png'].includes(extension)) return 'image';
    return 'unsupported';
  }

  private detectFileKindFromMime(mime?: string): FileKind {
    if (!mime) return 'unsupported';
    if (mime === 'application/pdf') return 'pdf';
    if (mime.startsWith('image/')) return 'image';
    return 'unsupported';
  }
}