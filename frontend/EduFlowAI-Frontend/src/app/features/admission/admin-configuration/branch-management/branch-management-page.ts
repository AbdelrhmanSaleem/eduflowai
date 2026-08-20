import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';

import { AdmissionAdminCopy } from '../admission-admin.copy';
import { AdmissionAdminApiService } from '../data-access/admission-admin-api.service';
import { BranchDto } from '../models/admission-admin.model';
import { admissionAdminErrorMessage } from '../utils/admission-admin-error.util';

@Component({
  selector: 'app-branch-management-page',
  imports: [ReactiveFormsModule],
  templateUrl: './branch-management-page.html',
  styleUrl: '../admin-management.scss',
})
export class BranchManagementPage implements OnInit {
  protected readonly copy = inject(AdmissionAdminCopy);

  private readonly api = inject(AdmissionAdminApiService);
  private readonly formBuilder = inject(FormBuilder).nonNullable;
  private readonly destroyRef = inject(DestroyRef);

  protected readonly branches = signal<readonly BranchDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly success = signal<string | null>(null);
  protected readonly searchQuery = signal('');
  protected readonly drawerOpen = signal(false);
  protected readonly editingBranch = signal<BranchDto | null>(null);

  protected readonly filteredBranches = computed(() => {
    const query = this.searchQuery().toLowerCase().trim();
    const items = this.branches();
    if (!query) {
      return items;
    }
    return items.filter(
      (branch) =>
        branch.name.toLowerCase().includes(query) ||
        (branch.governorate ?? '').toLowerCase().includes(query),
    );
  });

  protected readonly branchForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    governorate: ['', Validators.maxLength(100)],
    isActive: [true],
  });

  ngOnInit(): void {
    this.loadBranches();
  }

  protected loadBranches(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api
      .getBranches()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (branches) => this.branches.set(branches),
        error: (error: unknown) => this.showError(error, 'Unable to load branches.'),
      });
  }

  protected onSearch(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  protected openDrawer(branch?: BranchDto): void {
    this.clearMessages();
    if (branch) {
      this.editingBranch.set(branch);
      this.branchForm.setValue({
        name: branch.name,
        governorate: branch.governorate ?? '',
        isActive: branch.isActive,
      });
    } else {
      this.editingBranch.set(null);
      this.branchForm.reset({ name: '', governorate: '', isActive: true });
    }
    this.drawerOpen.set(true);
  }

  protected closeDrawer(): void {
    this.drawerOpen.set(false);
    this.editingBranch.set(null);
    this.branchForm.reset({ name: '', governorate: '', isActive: true });
  }

  protected saveBranch(): void {
    this.clearMessages();

    if (this.branchForm.invalid) {
      this.branchForm.markAllAsTouched();
      return;
    }

    const value = this.branchForm.getRawValue();
    const request = {
      name: value.name.trim(),
      governorate: value.governorate.trim() || null,
      isActive: value.isActive,
    };
    const editing = this.editingBranch();
    const operation = editing
      ? this.api.updateBranch(editing.id, request)
      : this.api.createBranch(request);

    this.busy.set(true);
    operation
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.success.set(this.copy.text('Branch saved successfully.', 'تم حفظ الفرع بنجاح.'));
          this.closeDrawer();
          this.loadBranches();
        },
        error: (error: unknown) => this.showError(error, 'Unable to save branch.'),
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
