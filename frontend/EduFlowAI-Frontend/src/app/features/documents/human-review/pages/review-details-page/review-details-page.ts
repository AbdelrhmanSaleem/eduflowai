import { Component, computed, DestroyRef, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';

import { LocaleStore } from '../../../../../core/i18n/locale.store';
import {
  ConfirmationDialog,
  ConfirmationDialogConfig,
} from '../../../../../shared/ui/confirmation-dialog/confirmation-dialog';
import {
  ReasonDialog,
  ReasonDialogConfig,
} from '../../../../../shared/ui/reason-dialog/reason-dialog';
import {
  SecureFileViewer,
  SecureFileViewerLabels,
} from '../../../../../shared/ui/secure-file-viewer/secure-file-viewer';
import { HumanReviewApi } from '../../data-access/human-review.api';
import { HUMAN_REVIEW_COPY } from '../../human-review.copy';
import {
  DocumentReviewDto,
  DocumentStatus,
  DocumentType,
  VerificationField,
} from '../../models/human-review.model';
import { parseVerificationDetails } from '../../utils/verification-details.parser';

type ReviewDialog = 'approve' | 'reject' | 'replacement' | null;

@Component({
  selector: 'app-review-details-page',
  imports: [ConfirmationDialog, ReasonDialog, RouterLink, SecureFileViewer],
  templateUrl: './review-details-page.html',
  styleUrl: './review-details-page.scss',
})
export class ReviewDetailsPage implements OnInit, OnDestroy {
  protected readonly locale = inject(LocaleStore);
  protected readonly copy = computed(() => HUMAN_REVIEW_COPY[this.locale.locale()]);
  protected readonly review = signal<DocumentReviewDto | null>(null);
  protected readonly verification = computed(() =>
    parseVerificationDetails(this.review()?.verificationDetailsJson),
  );
  protected readonly hasMismatch = computed(() =>
    this.verification()?.fields.some((field) => !field.isMatch),
  );
  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly fileLoading = signal(false);
  protected readonly fileError = signal(false);
  protected readonly fileUrl = signal('');
  protected readonly fileMimeType = signal('application/octet-stream');
  protected readonly activeDialog = signal<ReviewDialog>(null);
  protected readonly submitting = signal(false);
  protected readonly actionError = signal('');
  protected readonly viewerLabels = computed(
    () => this.copy().viewer as unknown as SecureFileViewerLabels,
  );
  protected readonly approveConfig = computed<ConfirmationDialogConfig>(() => ({
    title: this.copy().approve.title,
    message: this.copy().approve.message,
    cancelLabel: this.copy().common.cancel,
    confirmLabel: this.copy().approve.confirm,
    submittingLabel: this.copy().approve.confirming,
  }));
  protected readonly rejectConfig = computed<ReasonDialogConfig>(() => ({
    title: this.copy().reject.title,
    explanation: this.copy().reject.explanation,
    label: this.copy().reject.label,
    requiredLabel: this.copy().reject.required,
    placeholder: this.copy().reject.placeholder,
    helper: this.copy().reject.helper,
    quickInsertLabel: this.copy().reject.quickInsert,
    quickReasons: [
      this.copy().reject.illegible,
      this.copy().reject.missingPages,
      this.copy().reject.unofficial,
      this.copy().reject.incorrect,
    ],
    cancelLabel: this.copy().common.cancel,
    confirmLabel: this.copy().reject.confirm,
    submittingLabel: this.copy().reject.confirming,
    requiredError: this.copy().reject.requiredError,
    lengthError: this.copy().reject.lengthError,
    variant: 'danger',
  }));
  protected readonly replacementConfig = computed<ReasonDialogConfig>(() => ({
    title: this.copy().replacement.title,
    explanation: this.copy().replacement.explanation,
    label: this.copy().replacement.label,
    requiredLabel: this.copy().replacement.required,
    placeholder: this.copy().replacement.placeholder,
    helper: this.copy().replacement.helper,
    quickInsertLabel: this.copy().replacement.quickInsert,
    quickReasons: [
      this.copy().replacement.expired,
      this.copy().replacement.illegible,
      this.copy().replacement.missingPages,
      this.copy().replacement.incorrectDocument,
      this.copy().replacement.mismatch,
    ],
    cancelLabel: this.copy().common.cancel,
    confirmLabel: this.copy().replacement.confirm,
    submittingLabel: this.copy().replacement.confirming,
    requiredError: this.copy().replacement.requiredError,
    lengthError: this.copy().replacement.lengthError,
    variant: 'primary',
  }));

  private readonly api = inject(HumanReviewApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly documentId = this.route.snapshot.paramMap.get('documentId') ?? '';
  private currentObjectUrl = '';

  ngOnInit(): void {
    this.loadReview();
  }

  ngOnDestroy(): void {
    this.revokeFileUrl();
  }

  protected retry(): void {
    this.loadReview();
  }

  protected retryFile(): void {
    this.loadFile();
  }

  protected openDialog(dialog: Exclude<ReviewDialog, null>): void {
    if (!this.isActionable() || this.submitting()) {
      return;
    }
    this.actionError.set('');
    this.activeDialog.set(dialog);
  }

  protected closeDialog(): void {
    if (!this.submitting()) {
      this.activeDialog.set(null);
      this.actionError.set('');
    }
  }

  protected approve(): void {
    this.executeAction(this.api.approve(this.documentId), 'approved');
  }

  protected reject(reason: string): void {
    this.executeAction(this.api.reject(this.documentId, reason), 'rejected');
  }

  protected requestReplacement(reason: string): void {
    const review = this.review();
    if (!review) {
      return;
    }

    this.executeAction(
      this.api.requestReplacement({
        documentId: review.documentId,
        applicantId: review.applicantId,
        reason,
      }),
      'replacement',
      'ReplacementRequested',
    );
  }

  protected isActionable(): boolean {
    return this.review()?.status === 'NeedsHumanReview';
  }

  protected documentTypeLabel(type: DocumentType): string {
    return this.copy().common.documentTypes[type];
  }

  protected statusLabel(status: DocumentStatus): string {
    return this.copy().common.statuses[status];
  }

  protected fieldLabel(fieldName: string): string {
    const labels = this.copy().common.fieldNames as Readonly<Record<string, string>>;
    return labels[fieldName] ?? fieldName;
  }

  protected fieldValue(value?: string | null): string {
    return value?.trim() || this.copy().common.unavailable;
  }

  protected fieldNotes(field: VerificationField): string {
    return field.notes?.trim() || this.copy().details.discrepancy;
  }

  protected confidenceLabel(value: number): string {
    return `${Math.round(value * 100)}%`;
  }

  protected trackField(_: number, field: VerificationField): string {
    return field.fieldName;
  }

  private loadReview(): void {
    if (!this.documentId) {
      this.loading.set(false);
      this.error.set(true);
      return;
    }

    this.loading.set(true);
    this.error.set(false);
    this.api
      .getReview(this.documentId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (review) => {
          this.review.set(review);
          this.loading.set(false);
          this.loadFile();
        },
        error: () => {
          this.review.set(null);
          this.loading.set(false);
          this.error.set(true);
        },
      });
  }

  private loadFile(): void {
    this.revokeFileUrl();
    this.fileLoading.set(true);
    this.fileError.set(false);

    this.api
      .getDocumentFile(this.documentId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          if (!response.body) {
            this.fileLoading.set(false);
            this.fileError.set(true);
            return;
          }

          this.currentObjectUrl = URL.createObjectURL(response.body);
          this.fileUrl.set(this.currentObjectUrl);
          this.fileMimeType.set(
            response.headers.get('content-type') ||
              response.body.type ||
              'application/octet-stream',
          );
          this.fileLoading.set(false);
        },
        error: () => {
          this.fileLoading.set(false);
          this.fileError.set(true);
        },
      });
  }

  private executeAction(
    request: Observable<string>,
    outcome: string,
    updatedStatus?: DocumentStatus,
  ): void {
    if (this.submitting() || !this.isActionable()) {
      return;
    }

    this.submitting.set(true);
    this.actionError.set('');
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.submitting.set(false);
        this.activeDialog.set(null);
        if (updatedStatus) {
          this.review.update((review) => (review ? { ...review, status: updatedStatus } : review));
          return;
        }
        void this.router.navigate(['/operations/document-reviews'], {
          state: { reviewOutcome: outcome },
        });
      },
      error: () => {
        this.submitting.set(false);
        this.actionError.set(this.copy().details.actionError);
      },
    });
  }

  private revokeFileUrl(): void {
    if (this.currentObjectUrl) {
      URL.revokeObjectURL(this.currentObjectUrl);
      this.currentObjectUrl = '';
    }
    this.fileUrl.set('');
  }
}
