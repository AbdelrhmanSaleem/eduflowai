import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';

import { AdmissionAdminCopy } from '../admission-admin.copy';
import { AdmissionAdminApiService } from '../data-access/admission-admin-api.service';
import {
  AdminTrackDto,
  BranchDto,
  ProgramDto,
} from '../models/admission-admin.model';
import { admissionAdminErrorMessage } from '../utils/admission-admin-error.util';

@Component({
  selector: 'app-catalog-management-page',
  imports: [ReactiveFormsModule, RouterLink, RouterLinkActive],
  templateUrl: './catalog-management-page.html',
  styleUrl: '../admin-management.scss',
})
export class CatalogManagementPage implements OnInit {
  protected readonly copy = inject(AdmissionAdminCopy);

  private readonly api = inject(AdmissionAdminApiService);
  private readonly formBuilder = inject(FormBuilder).nonNullable;
  private readonly destroyRef = inject(DestroyRef);

  protected readonly programs = signal<readonly ProgramDto[]>([]);
  protected readonly tracks = signal<readonly AdminTrackDto[]>([]);
  protected readonly branches = signal<readonly BranchDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly success = signal<string | null>(null);
  protected readonly editingTrackId = signal<string | null>(null);
  protected readonly editingBranchId = signal<string | null>(null);

  protected readonly trackForm = this.formBuilder.group({
    programId: ['', Validators.required],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', Validators.maxLength(4000)],
    prerequisiteTopics: ['', Validators.maxLength(2000)],
    isActive: [true],
  });

  protected readonly branchForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    governorate: ['', Validators.maxLength(100)],
    isActive: [true],
  });

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      programs: this.api.getPrograms(),
      tracks: this.api.getTracks(),
      branches: this.api.getBranches(),
    })
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ({ programs, tracks, branches }) => {
          this.programs.set(programs);
          this.tracks.set(tracks);
          this.branches.set(branches);

          if (!this.trackForm.controls.programId.value && programs[0]) {
            this.trackForm.controls.programId.setValue(programs[0].id);
          }
        },
        error: (error: unknown) => this.showError(error, 'Unable to load catalog configuration.'),
      });
  }

  protected saveTrack(): void {
    this.clearMessages();

    if (this.trackForm.invalid) {
      this.trackForm.markAllAsTouched();
      return;
    }

    const value = this.trackForm.getRawValue();
    const topics = this.parseTopics(value.prerequisiteTopics);
    const editingId = this.editingTrackId();
    const common = {
      name: value.name.trim(),
      description: value.description.trim() || null,
      prerequisiteTopics: topics,
      isActive: value.isActive,
    };
    const operation = editingId
      ? this.api.updateTrack(editingId, common)
      : this.api.createTrack({ programId: value.programId, ...common });

    this.busy.set(true);
    operation
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.success.set(
            this.copy.text('Track saved successfully.', 'تم حفظ المسار بنجاح.'),
          );
          this.resetTrackForm();
          this.load();
        },
        error: (error: unknown) => this.showError(error, 'Unable to save track.'),
      });
  }

  protected editTrack(track: AdminTrackDto): void {
    this.clearMessages();
    this.editingTrackId.set(track.id);
    this.trackForm.setValue({
      programId: track.programId,
      name: track.name,
      description: track.description ?? '',
      prerequisiteTopics: track.prerequisiteTopics.join(', '),
      isActive: track.isActive,
    });
  }

  protected resetTrackForm(): void {
    this.editingTrackId.set(null);
    this.trackForm.reset({
      programId: this.programs()[0]?.id ?? '',
      name: '',
      description: '',
      prerequisiteTopics: '',
      isActive: true,
    });
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
    const editingId = this.editingBranchId();
    const operation = editingId
      ? this.api.updateBranch(editingId, request)
      : this.api.createBranch(request);

    this.busy.set(true);
    operation
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.success.set(
            this.copy.text('Branch saved successfully.', 'تم حفظ الفرع بنجاح.'),
          );
          this.resetBranchForm();
          this.load();
        },
        error: (error: unknown) => this.showError(error, 'Unable to save branch.'),
      });
  }

  protected editBranch(branch: BranchDto): void {
    this.clearMessages();
    this.editingBranchId.set(branch.id);
    this.branchForm.setValue({
      name: branch.name,
      governorate: branch.governorate ?? '',
      isActive: branch.isActive,
    });
  }

  protected resetBranchForm(): void {
    this.editingBranchId.set(null);
    this.branchForm.reset({ name: '', governorate: '', isActive: true });
  }

  protected programName(programId: string): string {
    return this.programs().find((program) => program.id === programId)?.name ?? '';
  }

  private parseTopics(value: string): string[] {
    return [...new Set(
      value
        .split(/[\n,]/)
        .map((topic) => topic.trim())
        .filter(Boolean),
    )].slice(0, 50);
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
