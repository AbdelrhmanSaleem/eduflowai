import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../../environments/environment'; // Import from environments
import { Observable } from 'rxjs';
import { 
    ApplicationDetailsDto, ApplicationRequestDto, Result, 
    UpdateApplicationPreferencesDto, ActiveAdmissionCycleDto,
    PaginatedResult, QueryParameters, ApplicationListDto,
    ApplicationStatusDto, EligibilityDetailsDto, SimulatedStageDto,
    EvaluateEligibilityRequestDto, EligibilityResult, DocumentReviewResultDto,
    EnrollmentChecklistDto
  } from '../models/application.model';

// providedIn: 'root' makes this service a singleton (one instance for the whole app)
@Injectable({
    providedIn: 'root'
})

export class ApplicationService {
    private readonly http = inject(HttpClient);
    // Mock API URL - Replace this with the production backend URL later
    private readonly applicationsUrl = `${environment.apiUrl}/api/modules/admission/applications`;
    private readonly cyclesUrl = `${environment.apiUrl}/api/modules/admission/cycles`;

    /**
         * Fetches the active admission cycles available for new applications.
         * Maps to GET: api/modules/admission/cycles/active
         */
    getActiveCycles(): Observable<Result<ActiveAdmissionCycleDto[]>> {
        return this.http.get<Result<ActiveAdmissionCycleDto[]>>(`${this.cyclesUrl}/active`);
    }

    /**
     * Fetch the new dashboard summary endpoint
     */
    getDashboardSummary(applicationId: string) {
        // Assuming the backend Result<T> wraps the actual data in a property (e.g., value or data)
        return this.http.get<any>(`${this.applicationsUrl}/${applicationId}/dashboard-summary`);
    }

    /**
     * Creates a new draft application.
     * Maps to POST: api/modules/admission/applications/draft
     * @param request request The application request data containing cycle and preferences
     * @returns Observable of the operation result
     */
    createDraft(request: ApplicationRequestDto): Observable<Result<ApplicationDetailsDto>> {
        return this.http.post<Result<ApplicationDetailsDto>>(`${this.applicationsUrl}/draft`, request);
    }

    /**
     * Retrieves full application details for editing.
     * Maps to GET: api/modules/admission/applications/{applicationId}
     * @param applicationId applicationId The unique identifier of the application
     * @returns Observable of the operation result containing application details
     */
    getApplicationDetails(applicationId: string): Observable<Result<ApplicationDetailsDto>> {
        return this.http.get<Result<ApplicationDetailsDto>>(`${this.applicationsUrl}/${applicationId}`);
    }

    /**
     * Updates application preferences.
     * Maps to PUT: api/modules/admission/applications/{applicationId}/preferences
     * @param applicationId applicationId The unique identifier of the application
     * @param request request The updated preferences data
     * @returns Observable of the operation result
     */
    updatePreferences(applicationId: string, request: UpdateApplicationPreferencesDto): Observable<Result<ApplicationDetailsDto>> {
        return this.http.put<Result<ApplicationDetailsDto>>(`${this.applicationsUrl}/${applicationId}/preferences`, request);
    }

    /**
     * Get list of applications of current user.
     * @param queryParams 
     * @returns 
     */
    getMyApplications(queryParams: QueryParameters): Observable<Result<PaginatedResult<ApplicationListDto>>> {
        let params = new HttpParams()

        // Dynamically append parameters only if they possess a valid value
        // The backend expects 'Page' and 'PageSize', mapping to the QueryParameters class
        if(queryParams.page !== undefined && queryParams.page !== null){
            params = params.set('page', queryParams.page.toString());
        }

        if(queryParams.pageSize !== undefined && queryParams.pageSize !== null){
            params = params.set('pageSize', queryParams.pageSize.toString());
        }

        if(queryParams.search){
            params = params.set('search', queryParams.search);
        }

        if(queryParams.status){
            params = params.set('status', queryParams.status);
        }

        if (queryParams.type) {
            params = params.set('type', queryParams.type);
        }

        // Execute the GET request with the constructed parameters
        // The return type tightly couples Result -> PaginatedResult -> ApplicationListDto
        return this.http.get<Result<PaginatedResult<ApplicationListDto>>>(`${this.applicationsUrl}/my-applications`, { params });
    }

    /**
     * Retrieves the enrollment checklist for a specific application.
     * Maps to GET: api/modules/admission/applications/{applicationId}/enrollment-checklist
     */
    getEnrollmentChecklist(applicationId: string): Observable<Result<EnrollmentChecklistDto>> {
        return this.http.get<Result<EnrollmentChecklistDto>>(`${this.applicationsUrl}/${applicationId}/enrollment-checklist`);
    }

    withdrawApplication(applicationId: string): Observable<Result<ApplicationStatusDto>> {
        return this.http.post<Result<ApplicationStatusDto>>(`${this.applicationsUrl}/${applicationId}/withdraw`, {});
    }

    /**
     * Fetches the current application status directly.
     * Maps to GET: api/modules/admission/applications/{applicationId}/status
     */
    getApplicationStatus(applicationId: string): Observable<Result<ApplicationStatusDto>> {
        return this.http.get<Result<ApplicationStatusDto>>(`${this.applicationsUrl}/${applicationId}/status`);
    }

    /**
     * Retrieves eligibility details including failure reasons.
     * Maps to GET: api/modules/admission/applications/{applicationId}/eligibility
     */
    getEligibilityDetails(applicationId: string): Observable<Result<EligibilityDetailsDto>> {
        return this.http.get<Result<EligibilityDetailsDto>>(`${this.applicationsUrl}/${applicationId}/eligibility`);
    }

    /**
     * Retrieves the assessment stages results for the applicant.
     * Maps to GET: api/modules/admission/applications/{applicationId}/stages
     */
    getStagesResults(applicationId: string): Observable<Result<SimulatedStageDto[]>> {
        return this.http.get<Result<SimulatedStageDto[]>>(`${this.applicationsUrl}/${applicationId}/stages`);
    }

    /**
     * Evaluates applicant eligibility criteria.
     * Maps to POST: api/modules/admission/applications/evaluate
     */
    evaluateApplicant(request: EvaluateEligibilityRequestDto): Observable<Result<EligibilityResult>> {
        return this.http.post<Result<EligibilityResult>>(`${this.applicationsUrl}/evaluate`, request);
    }

    /**
     * Submits a document review process result.
     * Maps to POST: api/modules/admission/applications/{applicationId}/process-document-review
     */
    processDocumentReview(applicationId: string, reviewResult: DocumentReviewResultDto): Observable<Result<ApplicationStatusDto>> {
        return this.http.post<Result<ApplicationStatusDto>>(`${this.applicationsUrl}/${applicationId}/process-document-review`, reviewResult);
    }

    /**
     * Submits the application for final review and triggers automatic eligibility evaluation.
     * Maps to POST: api/modules/admission/applications/{applicationId}/submit
     */
    submitApplication(applicationId: string): Observable<Result<ApplicationDetailsDto>> {
        return this.http.post<Result<ApplicationDetailsDto>>(`${this.applicationsUrl}/${applicationId}/submit`, {});
    }
}

