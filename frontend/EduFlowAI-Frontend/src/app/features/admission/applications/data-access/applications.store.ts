import { computed, inject } from '@angular/core';
import { signalStore, withState, withMethods, patchState, withComputed } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { tapResponse } from '@ngrx/operators';
import { ApplicationService } from './application.service';
import { extractErrorMessage } from '../../../../shared/utils/error.utils';
import { ApplicationDashboardSummary } from '../models/application-dashboard-summary.model';
import { 
  ApplicationDetailsDto, ApplicationRequestDto, UpdateApplicationPreferencesDto,
  ActiveAdmissionCycleDto, ApplicationListDto, PaginatedResult, QueryParameters,
  EligibilityDetailsDto, SimulatedStageDto, EvaluateEligibilityRequestDto,
  DocumentReviewResultDto, EnrollmentChecklistDto
} from '../models/application.model';

// 1. Define the state shape tailored for the Applicant Dashboard
type ApplicationState = {
  summaryData: ApplicationDashboardSummary | null;
  applicationDetails: ApplicationDetailsDto | null;
  activeCycles: ActiveAdmissionCycleDto[];

  applicationsList: PaginatedResult<ApplicationListDto> | null;
  listQueryParams: QueryParameters;
  isWithdrawing: boolean; // Loading state specific to withdrawal action

  eligibilityDetails: EligibilityDetailsDto | null,
  stagesResults: SimulatedStageDto[],
  isStatusDetailsLoading: boolean, // Useful for showing spinners on the micro-view

  enrollmentChecklist: EnrollmentChecklistDto | null;
  isLoadingChecklist: boolean;

  isLoading: boolean;
  error: string | null;
};
  
// 2. Initial state
const initialState: ApplicationState = {
  summaryData: null,
  applicationDetails: null,
  activeCycles: [],

  applicationsList: null,
  listQueryParams: { page: 1, pageSize: 10 },
  isWithdrawing: false,

  eligibilityDetails: null as EligibilityDetailsDto | null,
  stagesResults: [] as SimulatedStageDto[],
  isStatusDetailsLoading: false, // Useful for showing spinners on the micro-views
  
  enrollmentChecklist: null,
  isLoadingChecklist: false,

  isLoading: false,
  error: null,
};
  

// 3. Create the SignalStore
export const ApplicationsStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  
  // 4. Computed properties for derived UI states
  withComputed(({ summaryData, applicationDetails, applicationsList }) => ({
    // Check if documents are under verification (Milestone 1 & 2)
    isVerificationPending: computed(() => summaryData()?.currentStatus === 'UnderDocumentVerification'),
    
    // Business Rule: Can edit preferences only if status is Draft or DocumentsRequired (Milestone 3)
    canEdit: computed(() => {
      const details = applicationDetails();
      if (!details) return false;
      return details.status === 'Draft' || details.status === 'DocumentsRequired';
    }),
    
    // Check if the application deadline has passed (Milestone 3)
    isExpired: computed(() => {
      const details = applicationDetails();
      return details?.status === 'Expired';
    }),

    // Check if preferences are already filled (Milestone 3)
    hasPreferences: computed(() => {
      const details = applicationDetails();
      return details ? details.preferences.length > 0 : false;
    }),

    // Computed properties for Pagination UI
    hasApplications: computed(() => {
      const list = applicationsList();
      return list !== null && list.data.length > 0;
    }),
    paginationStats: computed(() => {
        const list = applicationsList();
        if (!list || list.totalCount === 0) return null;
        
        const start = (list.currentPage - 1) * list.pageSize + 1;
        const end = Math.min(list.currentPage * list.pageSize, list.totalCount);
        return { start, end, total: list.totalCount };
    }),
    hasNextPage: computed(() => {
        const list = applicationsList();
        return list ? list.currentPage < list.totalPages : false;
    }),
    hasPrevPage: computed(() => {
        const list = applicationsList();
        return list ? list.currentPage > 1 : false;
    })
  })),
  
  // 5. Async methods to communicate with the backend API
  withMethods((store, api = inject(ApplicationService)) => ({
    // Method to load active cycles from the backend
    loadActiveCycles: rxMethod<void>(
      pipe(
        tap(() => patchState(store, { isLoading: true, error: null })),
        switchMap(() => api.getActiveCycles().pipe(
          tapResponse({
            next: (response: any) => {
              const dataPayload = response.data || response.value || response;
              patchState(store, { activeCycles: dataPayload, isLoading: false });
            },
            error: (err: any) => patchState(store, { error: err.error?.message || 'Failed to load active cycles' }),
            finalize: () => patchState(store, { isLoading: false }),
          })
        ))
      )
    ),

    // Method to fetch the dashboard summary
    loadDashboardSummary: rxMethod<string>(
      pipe(
        tap(() => patchState(store, { isLoading: true, error: null })),
        switchMap((applicationId) => api.getDashboardSummary(applicationId).pipe(
          tapResponse({
            next: (response: any) => {
              const dataPayload = response.data || response.value || response;
              patchState(store, { summaryData: dataPayload, isLoading: false });
            },
            error: (err: any) => patchState(store, { error: err.error?.message || 'Failed to load dashboard summary' }),
            finalize: () => patchState(store, { isLoading: false }),
          })
        ))
      )
    ),

    // --- Milestone 3 Methods ---
    // Method to load full application details for editing
    loadApplicationDetails: rxMethod<string>(
      pipe(
        tap(() => patchState(store, { isLoading: true, error: null })),
        switchMap((applicationId) => api.getApplicationDetails(applicationId).pipe(
          tapResponse({
            next: (response: any) => patchState(store, { 
              applicationDetails: response.data, 
              isLoading: false 
            }),
            error: (err: any) => patchState(store, { 
              error: err.error?.message || 'Failed to load application details', 
              isLoading: false 
            })
          })
        ))
      )
    ),

    // Method to create a new draft application
    createDraft: rxMethod<ApplicationRequestDto>(
      pipe(
        tap(() => patchState(store, { isLoading: true, error: null })),
        switchMap((request) => api.createDraft(request).pipe(
          tapResponse({
            next: (response: any) => patchState(store, { 
              applicationDetails: response.data, 
              isLoading: false 
            }),
            error: (err: any) => {
              // Use our smart extractor!
              const errorMsg = extractErrorMessage(err, 'Failed to create application draft. Please try again.');

              patchState(store, { 
                error: errorMsg, 
                isLoading: false 
              });
            }
          })
        ))
      )
    ),

    // Method to update primary and backup preferences
    updatePreferences: rxMethod<{ applicationId: string; request: UpdateApplicationPreferencesDto }>(
      pipe(
        tap(() => patchState(store, { isLoading: true, error: null })),
        switchMap(({ applicationId, request }) => api.updatePreferences(applicationId, request).pipe(
          tapResponse({
            next: (response: any) => patchState(store, { 
              applicationDetails: response.data, 
              isLoading: false 
            }),
            error: (err: any) => patchState(store, { 
              error: err.error?.message || 'Failed to update preferences', 
              isLoading: false 
            })
          })
        ))
      )
    ),

    // Method to load paginated applications list
    loadMyApplications: rxMethod<QueryParameters>(
      pipe(
        tap((params) => patchState(store, { isLoading: true, error: null, listQueryParams: params })),
        switchMap((params) => api.getMyApplications(params).pipe(
          tapResponse({
            next: (response) => patchState(store, { 
              applicationsList: response.data, 
              isLoading: false 
            }),
            error: (err: any) => patchState(store, { 
              error: err.error?.message || 'Failed to load applications', 
              isLoading: false 
            })
          })
        ))
      )
    ),
  })),

  // 6. Dependent Async methods
  withMethods((store, api = inject(ApplicationService)) => ({
    // Method to withdraw an application and refresh the list
    withdrawApplication: rxMethod<string>(
      pipe(
        tap(() => patchState(store, { isWithdrawing: true, error: null })),
        switchMap((applicationId) => api.withdrawApplication(applicationId).pipe(
          tapResponse({
            next: () => {
              patchState(store, { isWithdrawing: false });
              // Refresh the list after successful withdrawal using current params
              store.loadMyApplications(store.listQueryParams());
            },
            error: (err: any) => patchState(store, { 
              error: err.error?.message || 'Failed to withdraw application', 
              isWithdrawing: false 
            })
          })
        ))
      )
    ),

    /**
     * Loads eligibility details for the given application ID
     */
    loadEligibilityDetails: rxMethod<string>(
      pipe(
        tap(() => patchState(store, { isStatusDetailsLoading: true })),
        switchMap((applicationId) => 
          api.getEligibilityDetails(applicationId).pipe(
            tapResponse({
              next: (response) => {
                if (response.isSuccess && response.data) {
                    patchState(store, { 
                        eligibilityDetails: response.data, 
                        isStatusDetailsLoading: false 
                    });
                }
              },
              error: (error) => {
                console.error('Error loading eligibility details:', error);
                patchState(store, { isStatusDetailsLoading: false });
              }
            })
          )
        )
      )
    ),

    /**
     * Loads simulated assessment stages for the given application ID
     */
    loadStagesResults: rxMethod<string>(
      pipe(
        tap(() => patchState(store, { isStatusDetailsLoading: true })),
        switchMap((applicationId) => 
          api.getStagesResults(applicationId).pipe(
            tapResponse({
              next: (response) => {
                if (response.isSuccess && response.data) {
                  patchState(store, { 
                      stagesResults: response.data, 
                      isStatusDetailsLoading: false 
                  });
                }
              },
              error: (error) => {
                console.error('Error loading stages results:', error);
                patchState(store, { isStatusDetailsLoading: false });
              }
            })
          )
        )
      )
    ),

    /**
     * Evaluates the applicant eligibility criteria
     */
    evaluateApplicant: rxMethod<EvaluateEligibilityRequestDto>(
      pipe(
        tap(() => patchState(store, { isLoading: true })), // Reusing your existing loading flag
        switchMap((request) => 
          api.evaluateApplicant(request).pipe(
            tapResponse({
              next: (response) => {
                patchState(store, { isLoading: false });
                // Optionally trigger a reload of the status or eligibility details here
              },
              error: (error) => {
                console.error('Evaluation failed:', error);
                patchState(store, { isLoading: false });
              }
            })
          )
        )
      )
    ),

    /**
     * Processes document review results
     */
    processDocumentReview: rxMethod<{applicationId: string, review: DocumentReviewResultDto}>(
      pipe(
        tap(() => patchState(store, { isLoading: true })),
        switchMap(({applicationId, review}) => 
          api.processDocumentReview(applicationId, review).pipe(
            tapResponse({
              next: (response) => {
                patchState(store, { isLoading: false });
                // Optionally update the local status state if needed
              },
              error: (error) => {
                console.error('Document review processing failed:', error);
                patchState(store, { isLoading: false });
              }
            })
          )
        )
      )
    ),

    /**
     * Method to submit the application and evaluate eligibility.
     */
    submitApplication: rxMethod<string>(
      pipe(
        tap(() => patchState(store, { isLoading: true, error: null })),
        switchMap((applicationId) => api.submitApplication(applicationId).pipe(
          tapResponse({
            next: (response: any) => patchState(store, { 
              // Update the details with the new status and eligibility result
              applicationDetails: response.data, 
              isLoading: false 
            }),
            error: (err: any) => patchState(store, { 
              error: err.error?.message || 'Failed to submit application', 
              isLoading: false 
            })
          })
        ))
      )
    ),

    /**
     * Loads the enrollment checklist details for the given application ID
     */
    loadEnrollmentChecklist: rxMethod<string>(
      pipe(
        tap(() => patchState(store, { isLoadingChecklist: true, error: null })),
        switchMap((applicationId) => 
          api.getEnrollmentChecklist(applicationId).pipe(
            tapResponse({
              next: (response) => {
                if (response.isSuccess && response.data) {
                  patchState(store, { 
                    enrollmentChecklist: response.data, 
                    isLoadingChecklist: false 
                  });
                }
              },
              error: (error: any) => {
                console.error('Error loading enrollment checklist:', error);
                patchState(store, { 
                  error: error.error?.message || 'Failed to load enrollment checklist',
                  isLoadingChecklist: false 
                });
              }
            })
          )
        )
      )
    ),
  }))
  
);