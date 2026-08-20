import { inject, computed } from '@angular/core';
import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { tapResponse } from '@ngrx/operators';
import { ApplicationService } from '../../applications/data-access/application.service'; 
import { SimulatedStageDto, SelectionStage } from '../../applications/models/application.model'; 
import { AssessmentStageStatus } from '../../../../shared/ui/stage-stepper/stage-stepper'; 
import { AssessmentService } from './assessment.service';
import { extractErrorMessage } from '../../../../shared/utils/error.utils';


// Define the shape of our state
interface AssessmentState {
  stages: SimulatedStageDto[];
  isLoading: boolean;
  error: string | null;

  isOverriding: boolean;
  overrideError: string | null;
  overrideSuccessMessage: string | null;

  isSimulating: boolean;
  simulateError: string | null;
  bulkSimulateSuccessMessage: string | null;
  completedStages: number[];

  isAllocating: boolean;
  allocationError: string | null;
  allocationSuccessMessage: string | null;
}

const initialState: AssessmentState = {
  stages: [],
  isLoading: false,
  error: null,

  isOverriding: false,
  overrideError: null,
  overrideSuccessMessage: null,

  isSimulating: false,
  simulateError: null,
  bulkSimulateSuccessMessage: null,
  completedStages: [],

  isAllocating: false,
  allocationError: null,
  allocationSuccessMessage: null,
};

// Helper function placed OUTSIDE the store to fix TypeScript Signal errors
// It maps the C# StageResult Enum to the frontend AssessmentStageStatus
function mapBackendResultToFrontendStatus(result: string | undefined): AssessmentStageStatus {
  if (!result) return 'Pending';
  
  // Based on StageResult C# Enum: None, Pending, Passed, NotPassed, Missed
  switch (result) {
    case 'Passed':
      return 'Passed';
    case 'NotPassed':
    case 'Missed':
      return 'NotPassed';
    case 'Pending':
    case 'None':
    default:
      return 'Pending';
  }
}

export const AssessmentStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed((store) => ({
    // Using computed() to properly return a Signal for each status
    // Mapping the C# SelectionStage Enum to our UI stepper items
    
    englishStatus: computed(() => {
      // Backend groups English and IQ into 'EnglishExamAndIq'
      const stage = store.stages().find(s => s.stageType === 'EnglishExamAndIq');
      return mapBackendResultToFrontendStatus(stage?.result);
    }),
    
    iqStatus: computed(() => {
      // Backend groups English and IQ into 'EnglishExamAndIq'
      const stage = store.stages().find(s => s.stageType === 'EnglishExamAndIq');
      return mapBackendResultToFrontendStatus(stage?.result);
    }),
    
    technicalStatus: computed(() => {
      // Maps to ProgrammingExam
      const stage = store.stages().find(s => s.stageType === 'ProgrammingExam');
      return mapBackendResultToFrontendStatus(stage?.result);
    }),
    
    interviewStatus: computed(() => {
      // Maps to SoftSkillsInterview
      const stage = store.stages().find(s => s.stageType === 'SoftSkillsInterview');
      return mapBackendResultToFrontendStatus(stage?.result);
    })
  })),
  withMethods((store, applicationService = inject(ApplicationService)) => ({
    
    // Method to fetch stages from backend
    loadStages: rxMethod<string>(
      pipe(
        tap(() => patchState(store, { isLoading: true, error: null })),
        switchMap((applicationId) =>
          applicationService.getStagesResults(applicationId).pipe(
            tapResponse({
              next: (response) => {
                if (response.isSuccess && response.data) {
                  patchState(store, { stages: response.data, isLoading: false });
                } else {
                  // Using fallback type casting to safely access 'message' if it exists, fixing the TS error
                  const errorMessage = (response as any).message || 'Failed to load stages';
                  patchState(store, { error: errorMessage, isLoading: false });
                }
              },
              error: (err: any) => {
                console.error('Error loading assessment stages', err);
                patchState(store, { error: err.message || 'Server error', isLoading: false });
              },
            })
          )
        )
      )
    ),
    
    // Method to clear state (useful when leaving the page)
    clearState: () => {
      patchState(store, initialState);
    }
  })),

  withMethods((store, assessmentService = inject(AssessmentService)) => ({
    
    /**
     * Submits the manual eligibility override request to the backend.
     */
    submitEligibilityOverride: rxMethod<{ applicationId: string; reason: string }>(
      pipe(
        // Set loading state and clear previous messages
        tap(() => patchState(store, { isOverriding: true, overrideError: null, overrideSuccessMessage: null })),
        switchMap(({ applicationId, reason }) =>
          assessmentService.overrideEligibility(applicationId, reason).pipe(
            tapResponse({
              next: (response) => {
                patchState(store, {
                  isOverriding: false,
                  overrideSuccessMessage: response.message || 'Override authorized successfully.'
                });
              },
              error: (err: any) => {
                patchState(store, {
                  isOverriding: false,
                  overrideError: err.error?.message || 'Failed to authorize override. Please try again.'
                });
              }
            })
          )
        )
      )
    ),

    /**
     * Helper method to reset the override state when opening/closing the modal.
     */
    resetOverrideState: () => {
      patchState(store, { isOverriding: false, overrideError: null, overrideSuccessMessage: null });
    },

    /**
     * Submits the bulk simulate stage request to the backend for an entire cycle.
     */
    submitBulkSimulateStage: rxMethod<{ cycleId: string; stage: SelectionStage }>(
      pipe(
        tap(() => patchState(store, { isSimulating: true, simulateError: null, bulkSimulateSuccessMessage: null })),
        switchMap(({ cycleId, stage }) =>
          assessmentService.bulkSimulateStage(cycleId, stage).pipe(
            tapResponse({
              next: (response: any) => {
                patchState(store, (state) => {
                  const newStages = state.completedStages.includes(stage) 
                    ? state.completedStages 
                    : [...state.completedStages, stage];

                  return {
                    isSimulating: false,
                    bulkSimulateSuccessMessage: response.message || 'Bulk simulation executed successfully.',
                    completedStages: newStages
                  };
                });
              },
              error: (err: any) => {
                // Use our new smart extractor!
                const errorMsg = extractErrorMessage(err, 'Failed to execute bulk simulation. Please check the Cycle ID.');
                
                patchState(store, {
                  isSimulating: false,
                  simulateError: errorMsg 
                });
              }
            })
          )
        )
      )
    ),

    /**
     * Helper method to reset the bulk simulation state.
     */
    resetBulkSimulateState: () => {
      patchState(store, { 
        isSimulating: false, 
        simulateError: null, 
        bulkSimulateSuccessMessage: null,
        completedStages: [] // <-- Clear the list on reset
      });
    },

    /**
     * Submits the request to run the allocation engine.
     */
    submitRunAllocation: rxMethod<string>(
      pipe(
        tap(() => patchState(store, { isAllocating: true, allocationError: null, allocationSuccessMessage: null })),
        switchMap((cycleId) =>
          assessmentService.runAllocation(cycleId).pipe(
            tapResponse({
              next: (response) => {
                patchState(store, {
                  isAllocating: false,
                  allocationSuccessMessage: response.message || 'Allocation engine executed successfully.'
                });
              },
              error: (err: any) => {
                // Use our new smart extractor here too!
                const errorMsg = extractErrorMessage(err, 'Failed to run allocation engine.');

                patchState(store, {
                  isAllocating: false,
                  allocationError: errorMsg
                });
              }
            })
          )
        )
      )
    ),
    
  }))
);