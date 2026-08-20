import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { DirectionalityService } from './core/i18n/directionality.service';
import { AssistantPage } from './features/ai/assistant/pages/assistant-page/assistant-page';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    AssistantPage,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly directionality = inject(DirectionalityService);
}