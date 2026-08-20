import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';

import { AdmissionAdminCopy } from '../admission-admin.copy';
import { AdmissionAdminApiService } from '../data-access/admission-admin-api.service';
import { InstitutionDto } from '../models/admission-admin.model';
import { admissionAdminErrorMessage } from '../utils/admission-admin-error.util';

@Component({
  selector: 'app-institution-management-page',
  imports: [ReactiveFormsModule],
  templateUrl: './institution-management-page.html',
  styleUrl: '../admin-management.scss',
})
export class InstitutionManagementPage implements OnInit {
  protected readonly copy = inject(AdmissionAdminCopy);

  private readonly api = inject(AdmissionAdminApiService);
  private readonly formBuilder = inject(FormBuilder).nonNullable;
  private readonly destroyRef = inject(DestroyRef);

  protected readonly institutions = signal<readonly InstitutionDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly success = signal<string | null>(null);
  protected readonly searchQuery = signal('');
  protected readonly drawerOpen = signal(false);
  protected readonly editingInstitution = signal<InstitutionDto | null>(null);

  protected readonly filteredInstitutions = computed(() => {
    const query = this.searchQuery().toLowerCase().trim();
    const items = this.institutions();
    if (!query) {
      return items;
    }
    return items.filter(
      (institution) =>
        institution.name.toLowerCase().includes(query) ||
        institution.code.toLowerCase().includes(query),
    );
  });

  protected readonly institutionForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    code: ['', [Validators.required, Validators.maxLength(20)]],
  });

  ngOnInit(): void {
    this.loadInstitutions();
  }

  protected loadInstitutions(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api
      .getInstitutions()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (institutions) => this.institutions.set(institutions),
        error: (error: unknown) => this.showError(error, 'Unable to load institutions.'),
      });
  }

  protected onSearch(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  protected openDrawer(institution?: InstitutionDto): void {
    this.clearMessages();
    if (institution) {
      this.editingInstitution.set(institution);
      this.institutionForm.setValue({
        name: institution.name,
        code: institution.code,
      });
    } else {
      this.editingInstitution.set(null);
      this.institutionForm.reset({ name: '', code: '' });
    }
    this.drawerOpen.set(true);
  }

  protected closeDrawer(): void {
    this.drawerOpen.set(false);
    this.editingInstitution.set(null);
    this.institutionForm.reset({ name: '', code: '' });
  }

  protected saveInstitution(): void {
    this.clearMessages();

    if (this.institutionForm.invalid) {
      this.institutionForm.markAllAsTouched();
      return;
    }

    const value = this.institutionForm.getRawValue();
    const request = {
      name: value.name.trim(),
      code: value.code.trim().toUpperCase(),
    };
    const editing = this.editingInstitution();
    const operation = editing
      ? this.api.updateInstitution(editing.id, request)
      : this.api.createInstitution(request);

    this.busy.set(true);
    operation
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.success.set(
            this.copy.text('Institution saved successfully.', 'تم حفظ المؤسسة بنجاح.'),
          );
          this.closeDrawer();
          this.loadInstitutions();
        },
        error: (error: unknown) => this.showError(error, 'Unable to save institution.'),
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
