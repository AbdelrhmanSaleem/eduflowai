import {
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';

import { AdmissionAdminCopy } from '../admission-admin.copy';
import { AdmissionAdminApiService } from '../data-access/admission-admin-api.service';
import {
  DocumentType,
  ProgramDocumentRequirementInput,
  ProgramDto,
  RequirementGender,
} from '../models/admission-admin.model';
import { admissionAdminErrorMessage } from '../utils/admission-admin-error.util';

@Component({
  selector: 'app-document-requirements-page',
  imports: [ReactiveFormsModule],
  templateUrl: './document-requirements-page.html',
  styleUrl: '../admin-management.scss',
})
export class DocumentRequirementsPage implements OnInit {
  protected readonly copy = inject(AdmissionAdminCopy);
  protected readonly DocumentType = DocumentType;
  protected readonly RequirementGender = RequirementGender;

  private readonly api = inject(AdmissionAdminApiService);
  private readonly formBuilder = inject(FormBuilder).nonNullable;
  private readonly destroyRef = inject(DestroyRef);

  protected readonly programs = signal<readonly ProgramDto[]>([]);
  protected readonly requirements = signal<ProgramDocumentRequirementInput[]>([]);
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly success = signal<string | null>(null);
  protected readonly requirementsProgramId = signal<string>('');
  protected readonly drawerOpen = signal(false);

  protected readonly selectedProgram = computed(() =>
    this.programs().find(
      (program) => program.id === this.requirementsProgramId(),
    ) ?? null,
  );

  protected readonly documentTypes = [
    { value: DocumentType.NationalId, en: 'National ID', ar: 'بطاقة الرقم القومي' },
    { value: DocumentType.BirthCertificate, en: 'Birth Certificate', ar: 'شهادة الميلاد' },
    {
      value: DocumentType.GraduationCertificate,
      en: 'Graduation Certificate',
      ar: 'شهادة التخرج',
    },
    {
      value: DocumentType.MilitaryCertificate,
      en: 'Military Certificate',
      ar: 'شهادة الموقف من التجنيد',
    },
  ] as const;

  protected readonly genders = [
    { value: null, en: 'All genders', ar: 'جميع المتقدمين' },
    { value: RequirementGender.Male, en: 'Male only', ar: 'ذكور فقط' },
    { value: RequirementGender.Female, en: 'Female only', ar: 'إناث فقط' },
  ] as const;

  protected readonly requirementForm = this.formBuilder.group({
    documentType: [DocumentType.NationalId, Validators.required],
    requiredForGender: this.formBuilder.control<RequirementGender | null>(null),
  });

  ngOnInit(): void {
    this.loadPrograms();
  }

  protected loadPrograms(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api
      .getPrograms()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (programs) => {
          this.programs.set(programs);
          if (!this.requirementsProgramId() && programs[0]) {
            this.selectProgram(programs[0].id, false);
          }
        },
        error: (error: unknown) =>
          this.showError(error, 'Unable to load programs.'),
      });
  }

  protected onProgramChange(event: Event): void {
    this.selectProgram((event.target as HTMLSelectElement).value);
  }

  protected selectProgram(programId: string, clearMessages = true): void {
    if (clearMessages) {
      this.clearMessages();
    }

    this.requirementsProgramId.set(programId);
    this.requirements.set([]);

    if (!programId) {
      return;
    }

    this.busy.set(true);
    this.api
      .getProgramRequirements(programId)
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (requirements) =>
          this.requirements.set(
            requirements.map((requirement) => ({
              documentType: requirement.documentType,
              requiredForGender: requirement.requiredForGender,
            })),
          ),
        error: (error: unknown) =>
          this.showError(error, 'Unable to load requirements.'),
      });
  }

  protected openDrawer(): void {
    this.clearMessages();
    this.requirementForm.reset({
      documentType: DocumentType.NationalId,
      requiredForGender: null,
    });
    this.drawerOpen.set(true);
  }

  protected closeDrawer(): void {
    this.drawerOpen.set(false);
  }

  protected addRequirement(): void {
    const value = this.requirementForm.getRawValue();
    const duplicate = this.requirements().some(
      (requirement) =>
        requirement.documentType === value.documentType &&
        requirement.requiredForGender === value.requiredForGender,
    );

    if (duplicate) {
      this.error.set(
        this.copy.text(
          'That document and gender combination already exists.',
          'تمت إضافة نفس المستند ونوع المتقدم من قبل.',
        ),
      );
      return;
    }

    if (this.requirements().length >= 12) {
      this.error.set(
        this.copy.text(
          'A program can contain at most 12 requirement combinations.',
          'يمكن أن يحتوي البرنامج على 12 مجموعة متطلبات بحد أقصى.',
        ),
      );
      return;
    }

    this.clearMessages();
    this.requirements.update((requirements) => [...requirements, value]);
    this.closeDrawer();
  }

  protected removeRequirement(index: number): void {
    this.requirements.update((requirements) =>
      requirements.filter((_, currentIndex) => currentIndex !== index),
    );
  }

  protected saveRequirements(): void {
    this.clearMessages();
    const programId = this.requirementsProgramId();

    if (!programId || this.requirements().length === 0) {
      this.error.set(
        this.copy.text(
          'Select a program and add at least one requirement.',
          'اختر برنامجًا وأضف مستندًا مطلوبًا واحدًا على الأقل.',
        ),
      );
      return;
    }

    this.busy.set(true);
    this.api
      .updateProgramRequirements(programId, {
        requirements: this.requirements(),
      })
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (requirements) => {
          this.requirements.set(
            requirements.map((requirement) => ({
              documentType: requirement.documentType,
              requiredForGender: requirement.requiredForGender,
            })),
          );
          this.success.set(
            this.copy.text(
              'Document requirements saved successfully.',
              'تم حفظ المستندات المطلوبة بنجاح.',
            ),
          );
        },
        error: (error: unknown) =>
          this.showError(error, 'Unable to save requirements.'),
      });
  }

  protected documentTypeLabel(documentType: DocumentType): string {
    const option = this.documentTypes.find((item) => item.value === documentType);
    return option
      ? this.copy.text(option.en, option.ar)
      : String(documentType);
  }

  protected genderLabel(gender: RequirementGender | null): string {
    const option = this.genders.find((item) => item.value === gender);
    return option ? this.copy.text(option.en, option.ar) : '';
  }

  protected genderBadgeClasses(gender: RequirementGender | null): string {
    if (gender === RequirementGender.Male) {
      return 'bg-indigo-50 text-indigo-700 border-indigo-200';
    }
    if (gender === RequirementGender.Female) {
      return 'bg-pink-50 text-pink-700 border-pink-200';
    }
    return 'bg-secondary-container/50 text-on-secondary-container border-secondary-container';
  }

  private clearMessages(): void {
    this.error.set(null);
    this.success.set(null);
  }

  private showError(error: unknown, fallbackEnglish: string): void {
    this.error.set(
      admissionAdminErrorMessage(
        error,
        this.copy.text(fallbackEnglish, 'تعذر إكمال العملية المطلوبة.'),
      ),
    );
  }
}
