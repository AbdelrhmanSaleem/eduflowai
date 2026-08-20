import { Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApplicationsStore } from '../../data-access/applications.store';
import { TrackService } from '../../data-access/track.service';
import { TrackDto, BranchOfferingDto } from '../../models/track.model';
import { UpdateApplicationPreferencesDto, PreferenceDto } from '../../models/application.model';

// Custom validator to ensure Preference 1 and 2 are different (Track + Branch combination)
function distinctPreferencesValidator(control: AbstractControl): ValidationErrors | null {
  const track1 = control.get('preference1.trackId')?.value;
  const branch1 = control.get('preference1.branchId')?.value;
  
  const track2 = control.get('preference2.trackId')?.value;
  const branch2 = control.get('preference2.branchId')?.value;

  if (track1 && track2 && branch1 && branch2 && (track1 === track2) && (branch1 === branch2)) {
    return { duplicatePreferences: true };
  }
  return null;
}

@Component({
  selector: 'app-preference-selection-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './preference-selection-page.html',
  styleUrl: './preference-selection-page.scss',
})
export class PreferenceSelectionPage implements OnInit {
  readonly store = inject(ApplicationsStore);
  private readonly trackService = inject(TrackService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  preferencesForm: FormGroup = this.fb.group({
    preference1: this.fb.group({
      trackId: ['', Validators.required],
      branchId: ['', Validators.required]
    }),
    preference2: this.fb.group({
      trackId: [''],
      branchId: ['']
    })
  }, { validators: distinctPreferencesValidator });

  applicationId = signal<string>('');
  availableTracks = signal<TrackDto[]>([]);

  // Flag to determine if the component is acting as an Edit page or Create page
  isEditMode = signal<boolean>(false);

  // Independent signals to track selections for Two-way filtering
  selectedTrackId1 = signal<string | null>(null);
  selectedBranchId1 = signal<string | null>(null);
  
  selectedTrackId2 = signal<string | null>(null);
  selectedBranchId2 = signal<string | null>(null);

  // Modal visibility signal
  showPrerequisitesModal = signal<boolean>(false);

  // --- Computed Properties for Two-Way Filtering ---

  private allDistinctBranches = computed(() => {
    const branchesMap = new Map<string, BranchOfferingDto>();
    this.availableTracks().forEach(track => {
      track.offerings?.forEach(offering => {
        if (!branchesMap.has(offering.branchId)) {
          branchesMap.set(offering.branchId, offering);
        }
      });
    });
    return Array.from(branchesMap.values());
  });

  // Preference 1 Filters
  displayTracks1 = computed(() => {
    const branchId = this.selectedBranchId1();
    const trackId = this.selectedTrackId1();
    
    if (!branchId || trackId) return this.availableTracks();
    return this.availableTracks().filter(t => t.offerings?.some(o => o.branchId === branchId));
  });

  displayBranches1 = computed(() => {
    const trackId = this.selectedTrackId1();
    if (trackId) {
      const track = this.availableTracks().find(t => t.id.toLowerCase() === trackId.toLowerCase());
      return track ? track.offerings : [];
    }
    return this.allDistinctBranches();
  });

  // Preference 2 Filters
  displayTracks2 = computed(() => {
    const branchId = this.selectedBranchId2();
    const trackId = this.selectedTrackId2();
    
    if (!branchId || trackId) return this.availableTracks();
    return this.availableTracks().filter(t => t.offerings?.some(o => o.branchId === branchId));
  });

  displayBranches2 = computed(() => {
    const trackId = this.selectedTrackId2();
    if (trackId) {
      const track = this.availableTracks().find(t => t.id.toLowerCase() === trackId.toLowerCase());
      return track ? track.offerings : [];
    }
    return this.allDistinctBranches();
  });

  // Computed property to aggregate prerequisites for selected tracks
  prerequisitesList = computed(() => {
    const list: { trackName: string, topics: string[] }[] = [];
    const t1Id = this.selectedTrackId1();
    const t2Id = this.selectedTrackId2();

    if (t1Id) {
      const t1 = this.availableTracks().find(t => t.id.toLowerCase() === t1Id.toLowerCase());
      if (t1 && (t1 as any).prerequisiteTopics?.length > 0) {
        list.push({ trackName: t1.name, topics: (t1 as any).prerequisiteTopics });
      }
    }
    
    if (t2Id && t2Id !== t1Id) {
      const t2 = this.availableTracks().find(t => t.id.toLowerCase() === t2Id.toLowerCase());
      if (t2 && (t2 as any).prerequisiteTopics?.length > 0) {
        list.push({ trackName: t2.name, topics: (t2 as any).prerequisiteTopics });
      }
    }
    return list;
  });

  daysRemaining = computed(() => {
    const details = this.store.applicationDetails();
    if (!details || !details.cycleDeadlineUtc) return 0;
    
    const deadline = new Date(details.cycleDeadlineUtc).getTime();
    const now = new Date().getTime();
    const diff = deadline - now;
    return Math.max(0, Math.ceil(diff / (1000 * 3600 * 24)));
  });

  constructor() {
    effect(() => {
      const details = this.store.applicationDetails();
      if (details) {
        this.loadTracksForCycle(details.cycleId);
        
        // If there are existing preferences, patch the form automatically
        if (details.preferences && details.preferences.length > 0) {
          const pref1 = details.preferences.find((p: any) => p.rank === 1);
          const pref2 = details.preferences.find((p: any) => p.rank === 2);
          
          if (pref1) {
            this.preferencesForm.get('preference1')?.patchValue({
              trackId: pref1.trackId,
              branchId: pref1.branchId
            }, { emitEvent: false});
            this.selectedTrackId1.set(pref1.trackId);
            this.selectedBranchId1.set(pref1.branchId);
          }
          if (pref2) {
            this.preferencesForm.get('preference2')?.patchValue({
              trackId: pref2.trackId,
              branchId: pref2.branchId
            }, { emitEvent: false });
            this.selectedTrackId2.set(pref2.trackId);
            this.selectedBranchId2.set(pref2.branchId);
          }
        }

        if (details.status === 'DocumentsRequired' || details.status === 'EligibilityFailed') {
          this.router.navigate(['/applications', details.id]);
        }

        if (!this.store.canEdit()) {
          this.preferencesForm.disable();
        }
      }
    });
  }

  ngOnInit(): void {
    // Check if the current URL contains '/edit' to set the mode flag dynamically
    this.isEditMode.set(this.router.url.includes('/edit'));

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.applicationId.set(id);
      this.store.loadApplicationDetails(id);
    }
  }

  private loadTracksForCycle(cycleId: string): void {
    this.trackService.getTracks(cycleId).subscribe({
      next: (response) => {
        if (response.isSuccess) {
          this.availableTracks.set(response.data);
        }
      },
      error: (err) => console.error('Failed to load tracks', err)
    });
  }

  // --- Event Handlers for Dropdowns ---

  onTrackChange(prefIndex: 1 | 2): void {
    const trackCtrl = this.preferencesForm.get(`preference${prefIndex}.trackId`);
    const branchCtrl = this.preferencesForm.get(`preference${prefIndex}.branchId`);
    const newTrackId = trackCtrl?.value || null;
    
    if (prefIndex === 1) this.selectedTrackId1.set(newTrackId);
    else this.selectedTrackId2.set(newTrackId);

    const currentBranchId = branchCtrl?.value;
    if (newTrackId && currentBranchId) {
      const track = this.availableTracks().find(t => t.id === newTrackId);
      const isValid = track?.offerings?.some(o => o.branchId === currentBranchId);
      
      if (!isValid) {
        branchCtrl?.setValue('');
        if (prefIndex === 1) this.selectedBranchId1.set(null);
        else this.selectedBranchId2.set(null);
      }
    }

    this.handleBackupValidation(prefIndex, trackCtrl, branchCtrl);
  }

  onBranchChange(prefIndex: 1 | 2): void {
    const trackCtrl = this.preferencesForm.get(`preference${prefIndex}.trackId`);
    const branchCtrl = this.preferencesForm.get(`preference${prefIndex}.branchId`);
    const newBranchId = branchCtrl?.value || null;

    if (prefIndex === 1) this.selectedBranchId1.set(newBranchId);
    else this.selectedBranchId2.set(newBranchId);

    const currentTrackId = trackCtrl?.value;
    if (newBranchId && currentTrackId) {
      const track = this.availableTracks().find(t => t.id === currentTrackId);
      const isValid = track?.offerings?.some(o => o.branchId === newBranchId);
      
      if (!isValid) {
        trackCtrl?.setValue('');
        if (prefIndex === 1) this.selectedTrackId1.set(null);
        else this.selectedTrackId2.set(null);
      }
    }

    this.handleBackupValidation(prefIndex, trackCtrl, branchCtrl);
  }

  private handleBackupValidation(prefIndex: number, trackCtrl: any, branchCtrl: any): void {
    if (prefIndex === 2) {
      if (trackCtrl?.value) {
        branchCtrl?.setValidators(Validators.required);
      } else {
        branchCtrl?.clearValidators();
      }
      branchCtrl?.updateValueAndValidity();
    }
  }

  // --- Modal Logic ---
  openPrerequisitesModal(): void {
    this.showPrerequisitesModal.set(true);
  }

  closePrerequisitesModal(): void {
    this.showPrerequisitesModal.set(false);
  }

  // --- API Actions ---
  savePreferences(): void {
    if (this.preferencesForm.invalid) {
      this.preferencesForm.markAllAsTouched();
      return;
    }

    const formValue = this.preferencesForm.value;
    const preferences: PreferenceDto[] = [];

    if (formValue.preference1.trackId && formValue.preference1.branchId) {
      preferences.push({ trackId: formValue.preference1.trackId, branchId: formValue.preference1.branchId, rank: 1 });
    }
    if (formValue.preference2.trackId && formValue.preference2.branchId) {
      preferences.push({ trackId: formValue.preference2.trackId, branchId: formValue.preference2.branchId, rank: 2 });
    }

    this.store.updatePreferences({ applicationId: this.applicationId(), request: { preferences } });
    this.preferencesForm.markAsPristine();
  }

  submitApplication(): void {
    if (this.preferencesForm.valid && this.store.canEdit() && !this.preferencesForm.dirty) {
      this.store.submitApplication(this.applicationId());
    }
  }

  goBack(): void {
    this.router.navigate(['/applications', this.applicationId()]);
  }
}