import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable, finalize, forkJoin } from 'rxjs';

import { AdmissionAdminCopy } from '../admission-admin.copy';
import { AdmissionAdminApiService } from '../data-access/admission-admin-api.service';
import {
  AdmissionCycleDto,
  AdminTrackDto,
  BranchDto,
  CumulativeGrade,
  CycleStatus,
  OfferingDto,
  ProgramDto,
} from '../models/admission-admin.model';
import { admissionAdminErrorMessage } from '../utils/admission-admin-error.util';

@Component({
  selector: 'app-cycle-management-page',
  imports: [ReactiveFormsModule],
  templateUrl: './cycle-management-page.html',
  styleUrl: '../admin-management.scss',
})
export class CycleManagementPage implements OnInit {
  protected readonly copy = inject(AdmissionAdminCopy);
  protected readonly CycleStatus = CycleStatus;

  private readonly api = inject(AdmissionAdminApiService);
  private readonly formBuilder = inject(FormBuilder).nonNullable;
  private readonly destroyRef = inject(DestroyRef);

  protected readonly programs = signal<readonly ProgramDto[]>([]);
  protected readonly tracks = signal<readonly AdminTrackDto[]>([]);
  protected readonly branches = signal<readonly BranchDto[]>([]);
  protected readonly cycles = signal<readonly AdmissionCycleDto[]>([]);
  protected readonly selectedCycleId = signal('');
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly success = signal<string | null>(null);
  protected readonly activeSubTab = signal<'overview' | 'eligibility' | 'offerings'>('overview');

  protected readonly selectedCycle = computed(
    () => this.cycles().find((cycle) => cycle.id === this.selectedCycleId()) ?? null,
  );

  protected readonly selectedProgramTracks = computed(() => {
    const cycle = this.selectedCycle();
    return cycle ? this.tracks().filter((track) => track.programId === cycle.programId) : [];
  });

  protected availableOfferingBranches(): readonly BranchDto[] {
    const track = this.tracks().find(
      (item) => item.id === this.offeringForm.controls.trackId.value,
    );
    return this.branchesForTrack(track, this.branches());
  }

  protected readonly cycleForm = this.formBuilder.group({
    programId: ['', Validators.required],
    label: ['', [Validators.required, Validators.maxLength(200)]],
    startDate: ['', Validators.required],
    deadlineLocal: ['', Validators.required],
  });

  protected readonly eligibilityForm = this.formBuilder.group({
    requiredNationality: ['EG', [Validators.required, Validators.maxLength(5)]],
    requiredDegreeLevel: ['Bachelor', [Validators.required, Validators.maxLength(30)]],
    maxYearsSinceGraduation: [5, [Validators.required, Validators.min(0), Validators.max(100)]],
    minGrade: [CumulativeGrade.Good, Validators.required],
  });

  protected readonly offeringForm = this.formBuilder.group({
    trackId: ['', Validators.required],
    branchId: ['', Validators.required],
    capacity: [1, [Validators.required, Validators.min(1)]],
  });

  protected readonly gradeOptions = [
    { value: CumulativeGrade.Acceptable, en: 'Acceptable', ar: 'مقبول' },
    { value: CumulativeGrade.Good, en: 'Good', ar: 'جيد' },
    { value: CumulativeGrade.VeryGood, en: 'Very good', ar: 'جيد جدًا' },
    { value: CumulativeGrade.Excellent, en: 'Excellent', ar: 'ممتاز' },
  ] as const;

  ngOnInit(): void {
    // Configuration controls must not look editable until a cycle is selected.
    this.setConfigurationFormsEditable(false);
    this.load();
  }

  protected load(preferredCycleId?: string): void {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      programs: this.api.getPrograms(),
      tracks: this.api.getTracks(),
      branches: this.api.getBranches(),
      cycles: this.api.getCycles(),
    })
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ({ programs, tracks, branches, cycles }) => {
          this.programs.set(programs);
          this.tracks.set(tracks);
          this.branches.set(branches);
          this.cycles.set(cycles);

          if (!this.cycleForm.controls.programId.value && programs[0]) {
            this.cycleForm.controls.programId.setValue(programs[0].id);
          }

          // Only auto-select a cycle when we were asked to land on a specific
          // one (e.g. after creating or saving). On the initial page open
          // (no preferredCycleId) we stay on the list view so the user can
          // pick the cycle they want.
          if (preferredCycleId) {
            const exists = cycles.some((c) => c.id === preferredCycleId);
            this.selectCycle(exists ? preferredCycleId : '');
          }
        },
        error: (error: unknown) => this.showError(error, 'Unable to load cycle configuration.'),
      });
  }

  protected createCycle(): void {
    this.clearMessages();

    if (this.cycleForm.invalid) {
      this.cycleForm.markAllAsTouched();
      return;
    }

    const value = this.cycleForm.getRawValue();
    const deadline = new Date(value.deadlineLocal);

    if (Number.isNaN(deadline.getTime())) {
      this.error.set(
        this.copy.text(
          'Enter a valid application deadline.',
          'أدخل موعدًا نهائيًا صحيحًا للتقديم.',
        ),
      );
      return;
    }

    this.busy.set(true);
    this.api
      .createCycle({
        programId: value.programId,
        label: value.label.trim(),
        startDate: value.startDate,
        deadlineUtc: deadline.toISOString(),
      })
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (cycle) => {
          this.success.set(
            this.copy.text('Draft cycle created successfully.', 'تم إنشاء دورة مسودة بنجاح.'),
          );
          this.cycleForm.reset({
            programId: this.programs()[0]?.id ?? '',
            label: '',
            startDate: '',
            deadlineLocal: '',
          });
          this.load(cycle.id);
        },
        error: (error: unknown) => this.showError(error, 'Unable to create cycle.'),
      });
  }

  protected onCycleChange(event: Event): void {
    this.selectCycle((event.target as HTMLSelectElement).value);
  }

  protected selectCycle(cycleId: string): void {
    this.selectedCycleId.set(cycleId);
    this.activeSubTab.set('overview');
    const cycle = this.cycles().find((item) => item.id === cycleId);

    if (!cycle) {
      this.eligibilityForm.reset({
        requiredNationality: 'EG',
        requiredDegreeLevel: 'Bachelor',
        maxYearsSinceGraduation: 5,
        minGrade: CumulativeGrade.Good,
      });
      this.offeringForm.reset({
        trackId: '',
        branchId: '',
        capacity: 1,
      });
      this.setConfigurationFormsEditable(false);
      return;
    }

    const rule = cycle.eligibilityRule;
    this.eligibilityForm.reset({
      requiredNationality: rule?.requiredNationality ?? 'EG',
      requiredDegreeLevel: rule?.requiredDegreeLevel ?? 'Bachelor',
      maxYearsSinceGraduation: rule?.maxYearsSinceGraduation ?? 5,
      minGrade: rule?.minGrade ?? CumulativeGrade.Good,
    });

    const tracks = this.tracks().filter(
      (track) => track.programId === cycle.programId && track.isActive,
    );
    const branches = this.branchesForTrack(tracks[0], this.branches()).filter(
      (branch) => branch.isActive,
    );
    this.offeringForm.reset({
      trackId: tracks[0]?.id ?? '',
      branchId: branches[0]?.id ?? '',
      capacity: 1,
    });

    // Draft is the only editable configuration state. Active/Closed must
    // look read-only as well as reject writes.
    this.setConfigurationFormsEditable(cycle.status === CycleStatus.Draft);
  }

  protected deselectCycle(): void {
    this.selectedCycleId.set('');
    this.activeSubTab.set('overview');
    this.clearMessages();
  }

  protected setSubTab(tab: 'overview' | 'eligibility' | 'offerings'): void {
    this.activeSubTab.set(tab);
    this.clearMessages();
  }

  protected onOfferingTrackChange(): void {
    const branches = this.availableOfferingBranches().filter((branch) => branch.isActive);
    const selectedBranchId = this.offeringForm.controls.branchId.value;
    if (!branches.some((branch) => branch.id === selectedBranchId)) {
      this.offeringForm.controls.branchId.setValue(branches[0]?.id ?? '');
    }
  }

  protected saveEligibility(): void {
    this.clearMessages();
    const cycle = this.selectedCycle();

    if (!cycle || cycle.status !== CycleStatus.Draft || this.eligibilityForm.invalid) {
      this.eligibilityForm.markAllAsTouched();
      return;
    }

    const value = this.eligibilityForm.getRawValue();
    this.busy.set(true);
    this.api
      .updateEligibilityRule(cycle.id, {
        requiredNationality: value.requiredNationality.trim().toUpperCase(),
        requiredDegreeLevel: value.requiredDegreeLevel.trim(),
        maxYearsSinceGraduation: value.maxYearsSinceGraduation,
        minGrade: value.minGrade,
      })
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.success.set(this.copy.text('Eligibility rule saved.', 'تم حفظ قاعدة الأهلية.'));
          this.load(cycle.id);
        },
        error: (error: unknown) => this.showError(error, 'Unable to save eligibility rule.'),
      });
  }

  protected addOffering(): void {
    this.clearMessages();
    const cycle = this.selectedCycle();

    if (!cycle || cycle.status !== CycleStatus.Draft || this.offeringForm.invalid) {
      this.offeringForm.markAllAsTouched();
      return;
    }

    const value = this.offeringForm.getRawValue();
    const duplicate = cycle.offerings.some(
      (offering) => offering.trackId === value.trackId && offering.branchId === value.branchId,
    );

    if (duplicate) {
      this.error.set(
        this.copy.text(
          'That track and branch combination already exists.',
          'تمت إضافة نفس المسار والفرع من قبل.',
        ),
      );
      return;
    }

    this.busy.set(true);
    this.api
      .createOffering(cycle.id, value)
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.success.set(
            this.copy.text('Offering added successfully.', 'تمت إضافة العرض بنجاح.'),
          );
          this.load(cycle.id);
        },
        error: (error: unknown) => this.showError(error, 'Unable to add the offering.'),
      });
  }

  protected updateOfferingCapacity(offering: OfferingDto, rawCapacity: string): void {
    this.clearMessages();
    const cycle = this.selectedCycle();
    const capacity = Number(rawCapacity);

    if (
      !cycle ||
      cycle.status !== CycleStatus.Draft ||
      !Number.isInteger(capacity) ||
      capacity <= 0
    ) {
      this.error.set(
        this.copy.text('Enter a positive whole-number capacity.', 'أدخل سعة صحيحة أكبر من صفر.'),
      );
      return;
    }

    if (capacity === offering.capacity) {
      return;
    }

    this.busy.set(true);
    this.api
      .updateOffering(cycle.id, offering.id, { capacity })
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.success.set(this.copy.text('Offering capacity updated.', 'تم تحديث سعة العرض.'));
          this.load(cycle.id);
        },
        error: (error: unknown) => this.showError(error, 'Unable to update the offering.'),
      });
  }

  protected removeOffering(offering: OfferingDto): void {
    this.clearMessages();
    const cycle = this.selectedCycle();

    if (!cycle || cycle.status !== CycleStatus.Draft) {
      return;
    }

    const confirmed = window.confirm(
      this.copy.text(
        `Remove ${offering.trackName} from ${offering.branchName}?`,
        `هل تريد حذف ${offering.trackName} من ${offering.branchName}؟`,
      ),
    );
    if (!confirmed) {
      return;
    }

    this.busy.set(true);
    this.api
      .deleteOffering(cycle.id, offering.id)
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.success.set(this.copy.text('Offering removed successfully.', 'تم حذف العرض بنجاح.'));
          this.load(cycle.id);
        },
        error: (error: unknown) => this.showError(error, 'Unable to remove the offering.'),
      });
  }

  protected activateCycle(): void {
    const cycle = this.selectedCycle();
    if (!cycle || cycle.status !== CycleStatus.Draft) {
      return;
    }

    const confirmed = window.confirm(
      this.copy.text(
        'Activate this cycle? Another active cycle for the same program will block activation.',
        'هل تريد تفعيل هذه الدورة؟ وجود دورة نشطة أخرى لنفس البرنامج سيمنع التفعيل.',
      ),
    );
    if (!confirmed) {
      return;
    }

    this.runCycleAction(
      this.api.activateCycle(cycle.id),
      cycle.id,
      this.copy.text('Cycle activated successfully.', 'تم تفعيل الدورة بنجاح.'),
    );
  }

  protected closeCycle(): void {
    const cycle = this.selectedCycle();
    if (!cycle || cycle.status !== CycleStatus.Active) {
      return;
    }

    const confirmed = window.confirm(
      this.copy.text(
        'Close this cycle? Applicants will no longer see its offerings.',
        'هل تريد إغلاق هذه الدورة؟ لن تظهر مساراتها للمتقدمين بعد الإغلاق.',
      ),
    );
    if (!confirmed) {
      return;
    }

    this.runCycleAction(
      this.api.closeCycle(cycle.id),
      cycle.id,
      this.copy.text('Cycle closed successfully.', 'تم إغلاق الدورة بنجاح.'),
    );
  }

  protected cycleCapacity(cycle: AdmissionCycleDto): number {
    return cycle.offerings.reduce((total, offering) => total + offering.capacity, 0);
  }

  protected cycleStatusLabel(status: CycleStatus): string {
    switch (status) {
      case CycleStatus.Draft:
        return this.copy.text('Draft', 'مسودة');
      case CycleStatus.Active:
        return this.copy.text('Active', 'نشطة');
      case CycleStatus.Closed:
        return this.copy.text('Closed', 'مغلقة');
      default:
        return String(status);
    }
  }

  protected formatDeadline(value: string): string {
    return new Intl.DateTimeFormat(this.copy.isRtl() ? 'ar-EG' : 'en-US', {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(value));
  }

  private runCycleAction(
    operation: Observable<AdmissionCycleDto>,
    cycleId: string,
    successMessage: string,
  ): void {
    this.clearMessages();
    this.busy.set(true);
    operation
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.success.set(successMessage);
          this.load(cycleId);
        },
        error: (error: unknown) => this.showError(error, 'Unable to change cycle status.'),
      });
  }

  private setConfigurationFormsEditable(editable: boolean): void {
    if (editable) {
      this.eligibilityForm.enable({ emitEvent: false });
      this.offeringForm.enable({ emitEvent: false });
      return;
    }

    this.eligibilityForm.disable({ emitEvent: false });
    this.offeringForm.disable({ emitEvent: false });
  }

  private branchesForTrack(
    track: AdminTrackDto | undefined,
    branches: readonly BranchDto[],
  ): readonly BranchDto[] {
    if (!track?.isOfficialIntake47) {
      return branches;
    }

    const officialBranchIds = new Set(track.locations.map((location) => location.branchId));
    return branches.filter((branch) => officialBranchIds.has(branch.id));
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
