// shared/ui/file-dropzone/file-dropzone.component.ts

import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  Output,
  ViewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { LucideAngularModule, UploadCloud, AlertCircle } from 'lucide-angular';

// TODO: fix this path to match your actual repo location for LocaleStore
import { LocaleStore } from '../../../core/i18n/locale.store';

@Component({
  selector: 'app-file-dropzone',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './file-dropzone.html',
  styleUrl: './file-dropzone.scss',
})
export class FileDropzone {
  /** File extensions without the dot, e.g. ['pdf', 'jpg', 'jpeg', 'png'] */
  @Input() acceptedTypes: string[] = ['pdf', 'jpg', 'jpeg', 'png'];
  @Input() maxSizeMb = 10;
  @Input() disabled = false;
  @Input() ariaLabel = 'Upload a document';

  /** Emits only after the file has passed client-side type/size validation. */
  @Output() fileSelected = new EventEmitter<File>();

  @ViewChild('fileInput') private fileInputRef!: ElementRef<HTMLInputElement>;

  protected readonly UploadCloudIcon = UploadCloud;
  protected readonly AlertCircleIcon = AlertCircle;

  protected readonly locale = inject(LocaleStore);

  protected readonly isDragOver = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly hintText = computed(() =>
    this.locale.t('common.fileDropzone.hint', {
      types: this.acceptedTypes.map((t) => t.toUpperCase()).join(', '),
      size: this.maxSizeMb,
    }),
  );

  protected readonly acceptAttr = computed(() =>
    this.acceptedTypes.map((t) => `.${t}`).join(','),
  );

  protected readonly containerClasses = computed(() => {
    const base = 'bg-[var(--surface-raised)]';
    if (this.disabled) {
      return `${base} border-[var(--border)] opacity-60 cursor-not-allowed`;
    }
    if (this.errorMessage()) {
      return `${base} border-[var(--red-700)] bg-[var(--red-100)]/40`;
    }
    if (this.isDragOver()) {
      return `${base} border-[var(--navy-900)] bg-[var(--border)]/20 cursor-pointer`;
    }
    return `${base} border-[var(--border-strong)] hover:border-[var(--navy-700)] hover:bg-[var(--border)]/10 cursor-pointer`;
  });

  protected onZoneClick(): void {
    if (this.disabled) return;
    this.fileInputRef.nativeElement.click();
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    if (this.disabled) return;
    this.isDragOver.set(true);
  }

  protected onDragLeave(event: DragEvent): void {
    event.preventDefault();
    // dragleave fires on every child element crossing — only reset when
    // the pointer has genuinely left the dropzone's bounding rect.
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    const { clientX: x, clientY: y } = event;
    if (x < rect.left || x >= rect.right || y < rect.top || y >= rect.bottom) {
      this.isDragOver.set(false);
    }
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(false);
    if (this.disabled) return;

    const file = event.dataTransfer?.files?.[0];
    if (file) this.validateAndEmit(file);
  }

  protected onInputChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) this.validateAndEmit(file);
    input.value = ''; // allow re-selecting the same file later
  }

  private validateAndEmit(file: File): void {
    const extension = file.name.split('.').pop()?.toLowerCase() ?? '';
    const isAcceptedType = this.acceptedTypes.includes(extension);
    const isWithinSize = file.size <= this.maxSizeMb * 1024 * 1024;

    if (!isAcceptedType) {
      this.errorMessage.set(
        this.locale.t('common.fileDropzone.errorType', { allowed: this.hintText() }),
      );
      return;
    }
    if (!isWithinSize) {
      this.errorMessage.set(
        this.locale.t('common.fileDropzone.errorSize', { size: this.maxSizeMb }),
      );
      return;
    }

    this.errorMessage.set(null);
    this.fileSelected.emit(file);
  }
}