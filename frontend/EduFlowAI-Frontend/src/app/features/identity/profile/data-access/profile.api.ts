import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { RuntimeConfig } from '../../../../core/config/runtime-config';
import {
  ApplicantProfile,
  UpdateApplicantProfileRequest,
} from '../models/profile.model';

@Injectable({ providedIn: 'root' })
export class ProfileApi {
  private readonly http = inject(HttpClient);
  private readonly profileUrl = `${inject(RuntimeConfig).apiBaseUrl}/profile`;

  getProfile(): Observable<ApplicantProfile> {
    return this.http.get<ApplicantProfile>(this.profileUrl);
  }

  updateProfile(
    request: UpdateApplicantProfileRequest,
  ): Observable<ApplicantProfile> {
    return this.http.put<ApplicantProfile>(this.profileUrl, request);
  }
}
