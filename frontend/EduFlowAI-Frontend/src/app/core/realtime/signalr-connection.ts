import { inject, Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

import { AuthSessionStore } from '../auth/auth-session.store';

@Injectable({ providedIn: 'root' })
export class SignalrConnection {
  private readonly session = inject(AuthSessionStore);

  create(url: string): HubConnection {
    return new HubConnectionBuilder()
      .withUrl(url, {
        accessTokenFactory: () => this.session.accessToken() ?? '',
      })
      .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
      .configureLogging(LogLevel.None)
      .build();
  }
}
