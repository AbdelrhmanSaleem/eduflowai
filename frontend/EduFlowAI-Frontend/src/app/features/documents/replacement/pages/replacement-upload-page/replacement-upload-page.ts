import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Params, RouterLink } from '@angular/router';

import { LocaleStore } from '../../../../../core/i18n/locale.store';
import { ReplacementRequestApi } from '../../data-access/replacement-request.api';
import {
  ReplacementRequestItem,
  ReplacementRequestStatus,
} from '../../models/replacement-request.model';
import { REPLACEMENT_COPY } from '../../replacement.copy';

const MAX_FILE_SIZE = 10 * 1024 * 1024;
const ALLOWED_EXTENSIONS = ['pdf', 'jpg', 'jpeg', 'png'];

@Component({
  selector: 'app-replacement-upload-page',
  imports: [RouterLink],
  templateUrl: './replacement-upload-page.html',
  styleUrl: './replacement-upload-page.scss',
})
export class ReplacementUploadPage implements OnInit {
  protected readonly locale = inject(LocaleStore);
  protected readonly copy = computed(() => REPLACEMENT_COPY[this.locale.locale()]);
  protected readonly request = signal<ReplacementRequestItem | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly selectedFile = signal<File | null>(null);
  protected readonly fileError = signal<string | null>(null);
  protected readonly uploading = signal(false);
  protected readonly uploadError = signal(false);
  protected readonly uploadSuccess = signal(false);
  protected readonly dragging = signal(false);
  protected readonly returnQueryParams: Params;

  private readonly api = inject(ReplacementRequestApi);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private requestId = '';

  constructor() {
    this.returnQueryParams = this.route.snapshot.queryParams;
  }

  ngOnInit(): void {
    this.requestId = this.route.snapshot.paramMap.get('id') ?? '';
    this.loadRequest();
  }

  protected retry(): void {
    this.loadRequest();
  }

  protected selectFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) {
      this.acceptFile(file);
    }
    input.value = '';
  }

  protected dragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(true);
  }

  protected dragLeave(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
  }

  protected dropFile(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
    const file = event.dataTransfer?.files[0];
    if (file) {
      this.acceptFile(file);
    }
  }

  protected clearFile(): void {
    this.selectedFile.set(null);
    this.fileError.set(null);
    this.uploadError.set(false);
  }

  protected submit(): void {
    const file = this.selectedFile();
    const request = this.request();
    if (!file || !request || request.status !== 'Open' || this.uploading()) {
      return;
    }

    this.uploading.set(true);
    this.uploadError.set(false);
    this.uploadSuccess.set(false);

    this.api
      .upload(request.id, file)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.request.update((current) =>
            current ? { ...current, status: 'Fulfilled' } : current,
          );
          this.selectedFile.set(null);
          this.uploadSuccess.set(true);
          this.uploading.set(false);
        },
        error: () => {
          this.uploadError.set(true);
          this.uploading.set(false);
        },
      });
  }

  protected statusLabel(status: ReplacementRequestStatus): string {
    return this.copy().common.statuses[status];
  }

  protected documentLabel(type: string | null): string {
    if (!type) {
      return this.copy().common.unknownDocument;
    }

    return this.copy().common.documentTypes[type] ?? type.replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  protected formatDate(value: string): string {
    return new Intl.DateTimeFormat(this.locale.locale() === 'ar' ? 'ar-EG' : 'en-US', {
      dateStyle: 'long',
    }).format(new Date(value));
  }

  protected formatFileSize(file: File): string {
    return file.size < 1024 * 1024
      ? `${Math.max(1, Math.round(file.size / 1024))} KB`
      : `${(file.size / (1024 * 1024)).toFixed(1)} MB`;
  }

  private loadRequest(): void {
    if (!this.requestId) {
      this.error.set(true);
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.error.set(false);

    this.api
      .getById(this.requestId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (request) => {
          this.request.set(request);
          this.loading.set(false);
        },
        error: () => {
          this.request.set(null);
          this.error.set(true);
          this.loading.set(false);
        },
      });
  }

  private acceptFile(file: File): void {
    this.uploadError.set(false);
    this.uploadSuccess.set(false);

    if (file.size === 0) {
      this.selectedFile.set(null);
      this.fileError.set(this.copy().details.emptyFile);
      return;
    }

    if (file.size > MAX_FILE_SIZE) {
      this.selectedFile.set(null);
      this.fileError.set(this.copy().details.fileTooLarge);
      return;
    }

    const extension = file.name.split('.').pop()?.toLocaleLowerCase() ?? '';
    if (!ALLOWED_EXTENSIONS.includes(extension)) {
      this.selectedFile.set(null);
      this.fileError.set(this.copy().details.invalidType);
      return;
    }

    this.fileError.set(null);
    this.selectedFile.set(file);
  }
}
