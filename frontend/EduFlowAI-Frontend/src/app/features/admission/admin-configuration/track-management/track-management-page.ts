import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';

import { AdmissionAdminCopy } from '../admission-admin.copy';
import { AdmissionAdminApiService } from '../data-access/admission-admin-api.service';
import { AdminTrackDto, ProgramDto } from '../models/admission-admin.model';
import { admissionAdminErrorMessage } from '../utils/admission-admin-error.util';

@Component({
  selector: 'app-track-management-page',
  imports: [ReactiveFormsModule],
  templateUrl: './track-management-page.html',
  styleUrl: '../admin-management.scss',
})
export class TrackManagementPage implements OnInit {
  protected readonly copy = inject(AdmissionAdminCopy);

  private readonly api = inject(AdmissionAdminApiService);
  private readonly formBuilder = inject(FormBuilder).nonNullable;
  private readonly destroyRef = inject(DestroyRef);

  protected readonly programs = signal<readonly ProgramDto[]>([]);
  protected readonly tracks = signal<readonly AdminTrackDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly success = signal<string | null>(null);
  protected readonly searchQuery = signal('');
  protected readonly drawerOpen = signal(false);
  protected readonly editingTrack = signal<AdminTrackDto | null>(null);
  protected readonly editablePrograms = computed(() => this.programs());

  protected readonly filteredTracks = computed(() => {
    const query = this.searchQuery().toLowerCase().trim();
    const items = this.tracks();
    if (!query) {
      return items;
    }
    return items.filter((track) =>
      [
        track.name,
        this.programName(track.programId),
        track.category,
        track.minimumGrade,
        track.eligibilitySummary,
        track.totalHours?.toString(),
        track.intake?.toString(),
        track.year?.toString(),
        ...track.prerequisiteTopics,
        ...track.locations.flatMap((location) => [location.branchName, location.governorate]),
      ]
        .filter((value): value is string => Boolean(value))
        .some((value) => value.toLowerCase().includes(query)),
    );
  });

  protected readonly trackForm = this.formBuilder.group({
    programId: ['', Validators.required],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', Validators.maxLength(4000)],
    prerequisiteTopics: ['', Validators.maxLength(2000)],
    isActive: [true],
  });

  ngOnInit(): void {
    this.loadData();
  }

  protected loadData(): void {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      programs: this.api.getPrograms(),
      tracks: this.api.getTracks(),
    })
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ({ programs, tracks }) => {
          this.programs.set(programs);
          this.tracks.set(tracks);
        },
        error: (error: unknown) => this.showError(error, 'Unable to load tracks.'),
      });
  }

  protected onSearch(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  protected openDrawer(track?: AdminTrackDto): void {
    this.clearMessages();
    if (track) {
      this.editingTrack.set(track);
      this.trackForm.setValue({
        programId: track.programId,
        name: track.name,
        description: track.description ?? '',
        prerequisiteTopics: track.prerequisiteTopics.join(', '),
        isActive: track.isActive,
      });
      // Tracks cannot be moved between programs after creation.
      this.trackForm.controls.programId.disable();
    } else {
      const program = this.editablePrograms()[0];
      if (!program) {
        this.error.set(this.copy.text('Create a program before adding tracks.', 'أنشئ برنامجًا قبل إضافة المسارات.'));
        return;
      }
      this.editingTrack.set(null);
      this.trackForm.reset({
        programId: program.id,
        name: '',
        description: '',
        prerequisiteTopics: '',
        isActive: true,
      });
      this.trackForm.controls.programId.enable();
    }
    this.drawerOpen.set(true);
  }

  protected closeDrawer(): void {
    this.drawerOpen.set(false);
    this.editingTrack.set(null);
    this.trackForm.controls.programId.enable();
    this.trackForm.reset({
      programId: this.editablePrograms()[0]?.id ?? '',
      name: '',
      description: '',
      prerequisiteTopics: '',
      isActive: true,
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
    const editing = this.editingTrack();
    const common = {
      name: value.name.trim(),
      description: value.description.trim() || null,
      prerequisiteTopics: topics,
      isActive: value.isActive,
    };
    const operation = editing
      ? this.api.updateTrack(editing.id, common)
      : this.api.createTrack({ programId: value.programId, ...common });

    this.busy.set(true);
    operation
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.success.set(this.copy.text('Track saved successfully.', 'تم حفظ المسار بنجاح.'));
          this.closeDrawer();
          this.loadData();
        },
        error: (error: unknown) => this.showError(error, 'Unable to save track.'),
      });
  }

  protected programName(programId: string): string {
    return this.programs().find((program) => program.id === programId)?.name ?? '';
  }

  private parseTopics(value: string): string[] {
    return [
      ...new Set(
        value
          .split(/[\n,]/)
          .map((topic) => topic.trim())
          .filter(Boolean),
      ),
    ].slice(0, 50);
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
