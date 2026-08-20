import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { RuntimeConfig } from '../../../../core/config/runtime-config';
import { ApiResult, TrackCatalogItem } from '../models/track-catalog.model';

@Injectable({ providedIn: 'root' })
export class TrackCatalogService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(RuntimeConfig).apiBaseUrl}/tracks`;

  getTracks(): Observable<readonly TrackCatalogItem[]> {
    return this.http
      .get<ApiResult<TrackCatalogItem[]>>(this.baseUrl)
      .pipe(map((response) => this.unwrap(response).map((track) => this.normalizeTrack(track))));
  }

  getTrack(trackId: string): Observable<TrackCatalogItem> {
    return this.http
      .get<ApiResult<TrackCatalogItem>>(`${this.baseUrl}/${trackId}`)
      .pipe(map((response) => this.normalizeTrack(this.unwrap(response))));
  }

  /**
   * Keep the catalog usable during a backend-first rolling deployment. Older
   * API instances do not return Intake 47 metadata, so additive fields must
   * receive safe runtime defaults instead of reaching templates as undefined.
   */
  private normalizeTrack(track: TrackCatalogItem): TrackCatalogItem {
    const offerings = track.offerings ?? [];
    const locations =
      track.isOfficialIntake47 || (track.locations?.length ?? 0) > 0
        ? (track.locations ?? [])
        : offerings
            .filter(
              (offering, index, items) =>
                items.findIndex((item) => item.branchId === offering.branchId) === index,
            )
            .map((offering) => ({
              branchId: offering.branchId,
              branchName: offering.branchName,
              governorate: offering.governorate,
            }));

    return {
      ...track,
      officialTrackId: track.officialTrackId ?? null,
      officialTrackUrl: track.officialTrackUrl ?? null,
      isOfficialIntake47: track.isOfficialIntake47 ?? false,
      intake: track.intake ?? null,
      year: track.year ?? null,
      category: track.category ?? null,
      totalHours: track.totalHours ?? null,
      minimumGrade: track.minimumGrade ?? null,
      eligibilitySummary: track.eligibilitySummary ?? null,
      graduationYearLimitYears: track.graduationYearLimitYears ?? null,
      prerequisiteTopics: track.prerequisiteTopics ?? [],
      locations,
      offerings,
    };
  }

  private unwrap<T>(response: ApiResult<T>): T {
    if (!response.isSuccess) {
      throw new Error(response.message || 'The request was not successful.');
    }

    return response.data;
  }
}
