// features/documents/applicant-documents/pages/required-documents-page/required-documents-page.component.ts

import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  Input,
  OnInit,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { LucideAngularModule, CircleCheck, RotateCcw, TriangleAlert, Info, Eye } from 'lucide-angular';

// TODO: fix this path to match your actual repo location for LocaleStore
import { LocaleStore } from '../../../../../core/i18n/locale.store';

import { ApplicantDocumentsStore } from '../../data-access/applicant-documents.store';
import { DocumentsApi } from '../../data-access/documents.api';
import { ApplicantDocumentDto, DocumentStatus, DocumentType } from '../../models/Document.model';
import { FileDropzone } from '../../../../../shared/ui/file-dropzone/file-dropzone';
import { UploadProgress } from '../../../../../shared/ui/upload-progress/upload-progress';
import { SecureFileViewer } from '../../../../../shared/ui/secure-file-viewer/secure-file-viewer';

/** Maps each DocumentType to its translation key — replaces the old hardcoded DOCUMENT_TYPE_LABELS map. */
const DOCUMENT_TYPE_KEYS: Record<DocumentType, string> = {
  [DocumentType.None]: 'documents.type.unknown',
  [DocumentType.NationalId]: 'documents.type.nationalId',
  [DocumentType.BirthCertificate]: 'documents.type.birthCertificate',
  [DocumentType.GraduationCertificate]: 'documents.type.graduationCertificate',
  [DocumentType.MilitaryCertificate]: 'documents.type.militaryCertificate',
};

/** Maps each DocumentStatus to its translation key, for the status badges. */
const DOCUMENT_STATUS_KEYS: Record<DocumentStatus, string> = {
  [DocumentStatus.None]: 'documents.status.pending',
  [DocumentStatus.Uploaded]: 'documents.status.uploaded',
  [DocumentStatus.Verifying]: 'documents.status.verifying',
  [DocumentStatus.Approved]: 'documents.status.approved',
  [DocumentStatus.NeedsHumanReview]: 'documents.status.needsReview',
  [DocumentStatus.Rejected]: 'documents.status.rejected',
};

@Component({
  selector: 'app-required-documents-page',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, FileDropzone, UploadProgress, SecureFileViewer],
  changeDetection: ChangeDetectionStrategy.OnPush,
  // Route-scoped: fresh store per navigation into this feature, not a singleton.
  providers: [DocumentsApi, ApplicantDocumentsStore],
  templateUrl: './required-documents-page.html',
  styleUrl: './required-documents-page.scss',
})
export class RequiredDocumentsPage implements OnInit {
  /** Bound from the :id route param (matches the applications feature's actual param name) — requires withComponentInputBinding() in app.config.ts */
  @Input({ required: true }) id!: string;

  @ViewChild(SecureFileViewer) private viewer!: SecureFileViewer;

  protected readonly store = inject(ApplicantDocumentsStore);
  protected readonly locale = inject(LocaleStore);

  protected readonly DocumentStatus = DocumentStatus;

  protected readonly CircleCheckIcon = CircleCheck;
  protected readonly RotateCcwIcon = RotateCcw;
  protected readonly TriangleAlertIcon = TriangleAlert;
  protected readonly InfoIcon = Info;
  protected readonly EyeIcon = Eye;

  /** Bound to SecureFileViewer's fileFetcher input. */
  protected readonly fetchFile = (documentId: string) => this.store.downloadFile(documentId);

  protected typeLabel(type: DocumentType): string {
    return this.locale.t(DOCUMENT_TYPE_KEYS[type]);
  }

  protected statusLabel(status: DocumentStatus | undefined): string {
    return this.locale.t(DOCUMENT_STATUS_KEYS[status ?? DocumentStatus.None]);
  }

  ngOnInit(): void {
    this.store.load(this.id);
  }

  protected retry(): void {
    this.store.load(this.id);
  }

  protected viewFile(doc: ApplicantDocumentDto): void {
    this.viewer.open(doc.id, doc.originalFileName);
  }

  /** Document types whose card currently shows the dropzone in place of the file info (Replace clicked). */
  private readonly _replacingTypes = signal<ReadonlySet<DocumentType>>(new Set());
  protected readonly replacingTypes = this._replacingTypes.asReadonly();

  protected isReplacing(type: DocumentType): boolean {
    return this._replacingTypes().has(type);
  }

  protected toggleReplace(type: DocumentType): void {
    const next = new Set(this._replacingTypes());
    next.has(type) ? next.delete(type) : next.add(type);
    this._replacingTypes.set(next);
  }

  protected onFileSelected(documentType: DocumentType, file: File): void {
    this.store.uploadDocument(this.id, documentType, file, () => {
      // Clear the "replacing" flag so the card flips back to file-info view
      const next = new Set(this._replacingTypes());
      if (next.delete(documentType)) {
        this._replacingTypes.set(next);
      }
    });
  }

  protected onSubmit(): void {
    this.store.submitPackage(this.id);
  }

  protected uploadedCount(): number {
    return this.store.requiredTypes().length - this.store.missingTypes().length;
  }

  protected progressPercent(): number {
    const total = this.store.requiredTypes().length;
    return total === 0 ? 0 : Math.round((this.uploadedCount() / total) * 100);
  }
}