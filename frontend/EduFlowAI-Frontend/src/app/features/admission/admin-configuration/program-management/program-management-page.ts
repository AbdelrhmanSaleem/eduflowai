import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';

import { AdmissionAdminCopy } from '../admission-admin.copy';
import { AdmissionAdminApiService } from '../data-access/admission-admin-api.service';
import { InstitutionDto, ProgramDto } from '../models/admission-admin.model';
import { admissionAdminErrorMessage } from '../utils/admission-admin-error.util';

@Component({
  selector: 'app-program-management-page',
  imports: [ReactiveFormsModule],
  templateUrl: './program-management-page.html',
  styleUrl: '../admin-management.scss',
})
export class ProgramManagementPage implements OnInit {
  protected readonly copy = inject(AdmissionAdminCopy);

  private readonly api = inject(AdmissionAdminApiService);
  private readonly formBuilder = inject(FormBuilder).nonNullable;
  private readonly destroyRef = inject(DestroyRef);

  protected readonly institutions = signal<readonly InstitutionDto[]>([]);
  protected readonly programs = signal<readonly ProgramDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly success = signal<string | null>(null);
  protected readonly searchQuery = signal('');
  protected readonly drawerOpen = signal(false);
  protected readonly editingProgram = signal<ProgramDto | null>(null);

  protected readonly filteredPrograms = computed(() => {
    const query = this.searchQuery().toLowerCase().trim();
    const items = this.programs();
    if (!query) {
      return items;
    }
    return items.filter(
      (program) =>
        program.name.toLowerCase().includes(query) ||
        program.code.toLowerCase().includes(query) ||
        program.institutionName.toLowerCase().includes(query),
    );
  });

  protected readonly programForm = this.formBuilder.group({
    institutionId: ['', Validators.required],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    code: ['', [Validators.required, Validators.maxLength(30)]],
    durationMonths: [9, [Validators.required, Validators.min(1), Validators.max(60)]],
  });

  ngOnInit(): void {
    this.loadData();
  }

  protected loadData(): void {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      institutions: this.api.getInstitutions(),
      programs: this.api.getPrograms(),
    })
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ({ institutions, programs }) => {
          this.institutions.set(institutions);
          this.programs.set(programs);
        },
        error: (error: unknown) => this.showError(error, 'Unable to load programs.'),
      });
  }

  protected onSearch(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  protected openDrawer(program?: ProgramDto): void {
    this.clearMessages();
    if (program) {
      this.editingProgram.set(program);
      this.programForm.setValue({
        institutionId: program.institutionId,
        name: program.name,
        code: program.code,
        durationMonths: program.durationMonths,
      });
    } else {
      this.editingProgram.set(null);
      this.programForm.reset({
        institutionId: this.institutions()[0]?.id ?? '',
        name: '',
        code: '',
        durationMonths: 9,
      });
    }
    this.drawerOpen.set(true);
  }

  protected closeDrawer(): void {
    this.drawerOpen.set(false);
    this.editingProgram.set(null);
    this.programForm.reset({
      institutionId: this.institutions()[0]?.id ?? '',
      name: '',
      code: '',
      durationMonths: 9,
    });
  }

  protected saveProgram(): void {
    this.clearMessages();

    if (this.programForm.invalid) {
      this.programForm.markAllAsTouched();
      return;
    }

    const value = this.programForm.getRawValue();
    const editing = this.editingProgram();
    const operation = editing
      ? this.api.updateProgram(editing.id, {
          name: value.name.trim(),
          code: value.code.trim().toUpperCase(),
          durationMonths: value.durationMonths,
        })
      : this.api.createProgram({
          institutionId: value.institutionId,
          name: value.name.trim(),
          code: value.code.trim().toUpperCase(),
          durationMonths: value.durationMonths,
        });

    this.busy.set(true);
    operation
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.success.set(this.copy.text('Program saved successfully.', 'تم حفظ البرنامج بنجاح.'));
          this.closeDrawer();
          this.loadData();
        },
        error: (error: unknown) => this.showError(error, 'Unable to save program.'),
      });
  }

  protected deleteProgram(program: ProgramDto): void {
    this.clearMessages();

    const confirmed = window.confirm(
      this.copy.text(
        `Delete ${program.name}? This also deletes its tracks, cycles, offerings, eligibility rules, and document requirements. This cannot be undone.`,
        `هل تريد حذف برنامج ${program.name}؟ سيتم أيضًا حذف المسارات والدورات والعروض وقواعد الأهلية ومتطلبات المستندات التابعة له. لا يمكن التراجع عن هذا الإجراء.`,
      ),
    );
    if (!confirmed) {
      return;
    }

    this.busy.set(true);
    this.api
      .deleteProgram(program.id)
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.success.set(
            this.copy.text(
              'Program and its configuration data were deleted successfully.',
              'تم حذف البرنامج وكل بيانات الإعداد التابعة له بنجاح.',
            ),
          );
          this.loadData();
        },
        error: (error: unknown) => this.showError(error, 'Unable to delete program.'),
      });
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
