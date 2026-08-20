import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../../environments/environment';

import {
  AssistantMessageRequest,
  AssistantResponse,
} from '../models/assistant.models';

@Injectable({
  providedIn: 'root',
})
export class AssistantApiService {
  private readonly http = inject(HttpClient);

  private readonly assistantUrl =
    `${environment.apiUrl}/api/assistant/message`;

  sendMessage(
    request: AssistantMessageRequest,
  ): Observable<AssistantResponse> {
    return this.http.post<AssistantResponse>(
      this.assistantUrl,
      request,
    );
  }
}