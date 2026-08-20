import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { BranchOfferingDto, TrackDto } from './../models/track.model';
import { Result } from './../models/application.model'

@Injectable({
  providedIn: 'root'
})
export class TrackService {
  private readonly http = inject(HttpClient);
  private readonly tracksUrl = `${environment.apiUrl}/api/tracks`;

  /**
   * Fetches the public tracks, optionally filtered by admission cycle.
   * Maps to GET: api/tracks?cycleId={cycleId}
   */
  getTracks(cycleId?: string): Observable<Result<TrackDto[]>> {
    let params = new HttpParams();
    if (cycleId) {
      params = params.set('cycleId', cycleId);
    }
    
    return this.http.get<Result<TrackDto[]>>(this.tracksUrl, { params });
  }
}