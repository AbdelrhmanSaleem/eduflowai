import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';
import { Observable } from 'rxjs';
import { Result, SelectionStage, SimulatedStageDto } from '../../applications/models/application.model';

// Provided at the root level so it can be injected into our SignalStore
@Injectable({
    providedIn: 'root'
})
export class AssessmentService {
    private readonly http = inject(HttpClient);
    
    // The base URL for the admin applications controller
    private readonly adminUrl = `${environment.apiUrl}/api/modules/admission/admin/applications`;

    /**
     * Overrides the eligibility failure for a specific application.
     * Maps to POST: api/modules/admission/admin/applications/{applicationId}/eligibility-override
     * 
     * @param applicationId The unique ID of the application
     * @param overrideReason The mandatory justification for bypassing the rules
     * @returns An observable containing the operation result
     */
    overrideEligibility(applicationId: string, overrideReason: string): Observable<Result<string>> {
        // Since the backend expects [FromBody] string, we pass the string wrapped in quotes 
        // so it is serialized as a valid JSON string payload.
        return this.http.post<Result<string>>(
            `${this.adminUrl}/${applicationId}/eligibility-override`, 
            `"${overrideReason}"`,
            { headers: { 'Content-Type': 'application/json' } }
        );
    }

    /**
     * Simulates an assessment stage for a given application.
     * Maps to POST: api/modules/admission/admin/applications/{applicationId}/simulate-stage
     * 
     * @param applicationId The unique ID of the application
     * @param stage The stage to simulate
     * @returns An observable containing the simulated stage result
     */
    simulateStage(applicationId: string, stage: SelectionStage): Observable<Result<SimulatedStageDto>> {
        return this.http.post<Result<SimulatedStageDto>>(
            `${this.adminUrl}/${applicationId}/simulate-stage?stage=${stage}`, 
            {} // Empty body since the stage is passed as a query parameter
        );
    }

    /**
     * Bulk simulates an assessment stage for all eligible applications in a cycle.
     * Maps to POST: api/modules/admission/admin/applications/cycles/{cycleId}/bulk-simulate-stage
     * 
     * @param cycleId The unique ID of the admission cycle
     * @param stage The assessment stage to simulate for all eligible applicants
     * @returns An observable containing the operation result message (e.g., how many were processed)
     */
    bulkSimulateStage(cycleId: string, stage: SelectionStage): Observable<Result<string>> {
        return this.http.post<Result<string>>(
            `${this.adminUrl}/cycles/${cycleId}/bulk-simulate-stage?stage=${stage}`, 
            {} // Empty body since the stage is passed as a query parameter
        );
    }

    /**
     * Executes the allocation engine for a specific cycle.
     * Maps to POST: api/cycles/{cycleId}/run-allocation
     * 
     * @param cycleId The unique ID of the admission cycle
     * @returns An observable containing the allocation results message
     */
    runAllocation(cycleId: string): Observable<Result<string>> {
        // Notice the different base URL for this specific controller
        return this.http.post<Result<string>>(
            `${environment.apiUrl}/api/cycles/${cycleId}/run-allocation`, 
            {}
        );
    }
}