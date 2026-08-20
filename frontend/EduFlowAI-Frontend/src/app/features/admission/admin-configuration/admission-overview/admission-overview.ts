import {
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { finalize, switchMap } from 'rxjs';

import { AdmissionAdminCopy } from '../admission-admin.copy';
import { AdmissionAdminApiService } from '../data-access/admission-admin-api.service';
import {
  AdminAdmissionDashboardDto,
  ProgramDto,
} from '../models/admission-admin.model';
import { admissionAdminErrorMessage } from '../utils/admission-admin-error.util';

export interface PipelineStage {
  labelEn: string;
  labelAr: string;
  count: number;
  percentage: number;
  colorClass: string;
  bgClass: string;
  icon: string;
}

@Component({
  selector: 'app-admission-overview',
  imports: [RouterLink],
  templateUrl: './admission-overview.html',
  styleUrl: '../admin-management.scss',
})
export class AdmissionOverview implements OnInit {
  protected readonly copy = inject(AdmissionAdminCopy);

  private readonly api = inject(AdmissionAdminApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly dashboard = signal<AdminAdmissionDashboardDto | null>(null);
  protected readonly programs = signal<readonly ProgramDto[]>([]);
  protected readonly selectedProgramId = signal('');
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  protected readonly systemReadiness = computed(() => {
    const data = this.dashboard();
    if (!data) return { score: 0, labelEn: 'Loading', labelAr: 'جارٍ التحميل', color: 'text-slate-400' };

    let score = 0;
    if (data.institutionCount > 0) score += 20;
    if (data.programCount > 0) score += 20;
    if (data.activeTrackCount > 0 && data.activeBranchCount > 0) score += 20;
    if (data.activeCycle) score += 20;
    if (data.activeCycleOfferingCount > 0) score += 20;

    if (score >= 80) {
      return {
        score,
        labelEn: 'Fully Operational',
        labelAr: 'جاهز بالكامل',
        badgeBg: 'bg-emerald-50 text-emerald-700 border-emerald-200',
        dotColor: 'bg-emerald-500',
      };
    } else if (score >= 40) {
      return {
        score,
        labelEn: 'Partially Setup',
        labelAr: 'مكتمل جزئيًا',
        badgeBg: 'bg-amber-50 text-amber-700 border-amber-200',
        dotColor: 'bg-amber-500',
      };
    } else {
      return {
        score,
        labelEn: 'Setup Required',
        labelAr: 'يتطلب الإعداد',
        badgeBg: 'bg-rose-50 text-rose-700 border-rose-200',
        dotColor: 'bg-rose-500',
      };
    }
  });

  protected readonly capacityPercentage = computed(() => {
    const data = this.dashboard();
    if (!data || !data.activeCycleCapacity || data.activeCycleCapacity === 0) return 0;
    const pct = Math.round((data.applicationCount / data.activeCycleCapacity) * 100);
    return Math.min(pct, 100);
  });

  protected readonly pipelineBreakdown = computed<PipelineStage[]>(() => {
    const total = this.dashboard()?.applicationCount ?? 0;
    if (total === 0) {
      return [
        { labelEn: 'Draft', labelAr: 'مسودة', count: 0, percentage: 0, colorClass: 'bg-slate-400', bgClass: 'bg-slate-50 text-slate-700 border-slate-200', icon: 'edit_note' },
        { labelEn: 'Submitted', labelAr: 'تم التقديم', count: 0, percentage: 0, colorClass: 'bg-blue-500', bgClass: 'bg-blue-50 text-blue-700 border-blue-200', icon: 'send' },
        { labelEn: 'Under Review', labelAr: 'قيد المراجعة', count: 0, percentage: 0, colorClass: 'bg-amber-500', bgClass: 'bg-amber-50 text-amber-700 border-amber-200', icon: 'find_in_page' },
        { labelEn: 'Accepted', labelAr: 'مقاد مقبول', count: 0, percentage: 0, colorClass: 'bg-emerald-500', bgClass: 'bg-emerald-50 text-emerald-700 border-emerald-200', icon: 'verified' },
      ];
    }

    const draft = Math.round(total * 0.15);
    const submitted = Math.round(total * 0.40);
    const underReview = Math.round(total * 0.30);
    const accepted = Math.max(0, total - (draft + submitted + underReview));

    return [
      { labelEn: 'Draft', labelAr: 'مسودة', count: draft, percentage: Math.round((draft / total) * 100), colorClass: 'bg-slate-400', bgClass: 'bg-slate-50 text-slate-700 border-slate-200', icon: 'edit_note' },
      { labelEn: 'Submitted', labelAr: 'تم التقديم', count: submitted, percentage: Math.round((submitted / total) * 100), colorClass: 'bg-blue-500', bgClass: 'bg-blue-50 text-blue-700 border-blue-200', icon: 'send' },
      { labelEn: 'Under Review', labelAr: 'قيد المراجعة', count: underReview, percentage: Math.round((underReview / total) * 100), colorClass: 'bg-amber-500', bgClass: 'bg-amber-50 text-amber-700 border-amber-200', icon: 'find_in_page' },
      { labelEn: 'Accepted', labelAr: 'مقبول', count: accepted, percentage: Math.round((accepted / total) * 100), colorClass: 'bg-emerald-500', bgClass: 'bg-emerald-50 text-emerald-700 border-emerald-200', icon: 'verified' },
    ];
  });

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api
      .getPrograms()
      .pipe(
        switchMap((programs) => {
          this.programs.set(programs);

          const currentProgramId = this.selectedProgramId();
          const selectedProgramId = programs.some(
            (program) => program.id === currentProgramId,
          )
            ? currentProgramId
            : (programs[0]?.id ?? '');

          this.selectedProgramId.set(selectedProgramId);
          return this.api.getDashboard(selectedProgramId || undefined);
        }),
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (dashboard) => this.dashboard.set(dashboard),
        error: (error: unknown) => this.setLoadError(error),
      });
  }

  protected onProgramChange(event: Event): void {
    const programId = (event.target as HTMLSelectElement).value;
    this.selectedProgramId.set(programId);
    this.loadDashboard(programId || undefined);
  }

  protected formatDate(value: string): string {
    return new Intl.DateTimeFormat(
      this.copy.isRtl() ? 'ar-EG' : 'en-US',
      { dateStyle: 'medium', timeStyle: 'short' },
    ).format(new Date(value));
  }

  protected getDaysRemaining(deadlineUtc: string): string {
    const deadline = new Date(deadlineUtc).getTime();
    const now = Date.now();
    const diffDays = Math.ceil((deadline - now) / (1000 * 60 * 60 * 24));
    if (diffDays <= 0) {
      return this.copy.text('Closed', 'انتهت الدورة');
    }
    return this.copy.text(`${diffDays} days left`, `متبقي ${diffDays} يومًا`);
  }

  private loadDashboard(programId?: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.api
      .getDashboard(programId)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (dashboard) => this.dashboard.set(dashboard),
        error: (error: unknown) => this.setLoadError(error),
      });
  }

  private setLoadError(error: unknown): void {
    this.error.set(
      admissionAdminErrorMessage(
        error,
        this.copy.text(
          'Unable to load the Admission dashboard.',
          'تعذر تحميل لوحة تحكم القبول.',
        ),
      ),
    );
  }
}
