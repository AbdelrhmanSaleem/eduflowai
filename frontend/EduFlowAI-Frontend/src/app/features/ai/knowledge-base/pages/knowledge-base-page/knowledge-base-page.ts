import { Component, computed, inject, signal } from '@angular/core';

import { LocaleStore } from '../../../../../core/i18n/locale.store';
import {
  KnowledgeBaseDocument,
  KnowledgeBaseSyncResult,
} from '../../models/knowledge-base.models';
import { KnowledgeBaseApiService } from '../../services/knowledge-base-api.service';

@Component({
  selector: 'app-knowledge-base-page',
  imports: [],
  templateUrl: './knowledge-base-page.html',
  styleUrls: [
    '../../../../admission/admin-configuration/admin-management.scss',
    './knowledge-base-page.scss',
  ],
})
export class KnowledgeBasePage {
  readonly locale = inject(LocaleStore);

  private readonly knowledgeBaseApi = inject(KnowledgeBaseApiService);

  readonly documents = signal<KnowledgeBaseDocument[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly isUploading = signal(false);
  readonly uploadError = signal<string | null>(null);

  readonly isAddingText = signal(false);
  readonly textError = signal<string | null>(null);

  readonly deletingDocumentId = signal<string | null>(null);
  readonly deleteError = signal<string | null>(null);

  readonly isSyncing = signal(false);
  readonly syncError = signal<string | null>(null);
  readonly syncResult = signal<KnowledgeBaseSyncResult | null>(null);

  readonly searchTerm = signal('');
  readonly statusFilter = signal('all');
  readonly currentPage = signal(1);
  readonly pageSize = signal(10);

  readonly filteredDocuments = computed(() => {
    const search = this.searchTerm().trim().toLowerCase();
    const status = this.statusFilter().trim().toLowerCase();

    return this.documents().filter(document => {
      const matchesSearch =
        !search ||
        document.fileName.toLowerCase().includes(search);

      const matchesStatus =
        status === 'all' ||
        document.status.trim().toLowerCase() === status;

      return matchesSearch && matchesStatus;
    });
  });

  readonly totalPages = computed(() =>
    Math.max(
      1,
      Math.ceil(this.filteredDocuments().length / this.pageSize()),
    ),
  );

  readonly pagedDocuments = computed(() => {
    const page = Math.min(this.currentPage(), this.totalPages());
    const start = (page - 1) * this.pageSize();

    return this.filteredDocuments().slice(
      start,
      start + this.pageSize(),
    );
  });

  readonly resultStart = computed(() => {
    if (this.filteredDocuments().length === 0) {
      return 0;
    }

    return (this.currentPage() - 1) * this.pageSize() + 1;
  });

  readonly resultEnd = computed(() =>
    Math.min(
      this.currentPage() * this.pageSize(),
      this.filteredDocuments().length,
    ),
  );

  selectedFile: File | null = null;
  textTitle = '';
  textContent = '';

  constructor() {
    this.loadDocuments();
  }

  loadDocuments(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.knowledgeBaseApi.getDocuments().subscribe({
      next: documents => {
        this.documents.set(documents);
        this.ensureValidPage();
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set(
          this.locale.t('knowledgeBase.error.load'),
        );
        this.isLoading.set(false);
      },
    });
  }

  onSearchInput(event: Event): void {
    const input = event.target as HTMLInputElement;

    this.searchTerm.set(input.value);
    this.currentPage.set(1);
  }

  onStatusFilterChange(event: Event): void {
    const select = event.target as HTMLSelectElement;

    this.statusFilter.set(select.value);
    this.currentPage.set(1);
  }

  onPageSizeChange(event: Event): void {
    const select = event.target as HTMLSelectElement;

    this.pageSize.set(Number(select.value));
    this.currentPage.set(1);
  }

  previousPage(): void {
    if (this.currentPage() > 1) {
      this.currentPage.update(page => page - 1);
    }
  }

  nextPage(): void {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update(page => page + 1);
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;

    this.selectedFile = input.files?.[0] ?? null;
    this.uploadError.set(null);
  }

  onTitleInput(event: Event): void {
    const input = event.target as HTMLInputElement;

    this.textTitle = input.value;
  }

  onContentInput(event: Event): void {
    const textarea = event.target as HTMLTextAreaElement;

    this.textContent = textarea.value;
  }

  uploadSelectedFile(): void {
    if (!this.selectedFile || this.isUploading()) {
      return;
    }

    this.isUploading.set(true);
    this.uploadError.set(null);

    this.knowledgeBaseApi.uploadFile(this.selectedFile).subscribe({
      next: () => {
        this.selectedFile = null;
        this.isUploading.set(false);
        this.loadDocuments();
      },
      error: () => {
        this.uploadError.set(
          this.locale.t('knowledgeBase.error.upload'),
        );
        this.isUploading.set(false);
      },
    });
  }

  addTextContent(): void {
    const content = this.textContent.trim();

    if (!content || this.isAddingText()) {
      return;
    }

    this.isAddingText.set(true);
    this.textError.set(null);

    this.knowledgeBaseApi
      .addText({
        title: this.textTitle.trim() || null,
        content,
      })
      .subscribe({
        next: () => {
          this.textTitle = '';
          this.textContent = '';
          this.isAddingText.set(false);
          this.loadDocuments();
        },
        error: () => {
          this.textError.set(
            this.locale.t('knowledgeBase.error.addText'),
          );
          this.isAddingText.set(false);
        },
      });
  }

  deleteDocument(documentId: string): void {
    if (this.deletingDocumentId()) {
      return;
    }

    this.deletingDocumentId.set(documentId);
    this.deleteError.set(null);

    this.knowledgeBaseApi.deleteDocument(documentId).subscribe({
      next: () => {
        this.deletingDocumentId.set(null);
        this.loadDocuments();
      },
      error: () => {
        this.deleteError.set(
          this.locale.t('knowledgeBase.error.delete'),
        );
        this.deletingDocumentId.set(null);
      },
    });
  }

  syncAll(): void {
    if (this.isSyncing()) {
      return;
    }

    this.isSyncing.set(true);
    this.syncError.set(null);
    this.syncResult.set(null);

    this.knowledgeBaseApi.syncAll().subscribe({
      next: result => {
        this.syncResult.set(result);
        this.isSyncing.set(false);
        this.loadDocuments();
      },
      error: () => {
        this.syncError.set(
          this.locale.t('knowledgeBase.error.sync'),
        );
        this.isSyncing.set(false);
      },
    });
  }

  statusLabel(status: string): string {
    switch (status.trim().toLowerCase()) {
      case 'pending':
        return this.locale.t('knowledgeBase.status.pending');

      case 'indexing':
        return this.locale.t('knowledgeBase.status.indexing');

      case 'indexed':
        return this.locale.t('knowledgeBase.status.indexed');

      case 'failed':
        return this.locale.t('knowledgeBase.status.failed');

      default:
        return status || this.locale.t('knowledgeBase.status.unknown');
    }
  }

  syncResultMessage(): string {
    const result = this.syncResult();

    if (!result) {
      return '';
    }

    return this.locale.t('knowledgeBase.syncCompleted', {
      indexed: result.indexed,
      failed: result.failed,
      total: result.totalDocuments,
    });
  }

  paginationSummary(): string {
    return this.locale.t('knowledgeBase.pagination.showing', {
      start: this.resultStart(),
      end: this.resultEnd(),
      total: this.filteredDocuments().length,
    });
  }

  pageSummary(): string {
    return this.locale.t('knowledgeBase.pagination.page', {
      current: this.currentPage(),
      total: this.totalPages(),
    });
  }

  private ensureValidPage(): void {
    if (this.currentPage() > this.totalPages()) {
      this.currentPage.set(this.totalPages());
    }
  }
}