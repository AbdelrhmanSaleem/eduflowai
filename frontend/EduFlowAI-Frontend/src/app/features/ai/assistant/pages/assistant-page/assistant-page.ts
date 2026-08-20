import {
  Component,
  computed,
  ElementRef,
  inject,
  signal,
  ViewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';

import {
  TrackCatalogItem,
} from '../../../../admission/catalog/models/track-catalog.model';
import {
  TrackCatalogService,
} from '../../../../admission/catalog/data-access/track-catalog.service';

import {
  ChatMessage,
  RecommendedTrack,
  RecommendationQuestionKey,
  RecommendationQuestionnaireProgress,
} from '../../models/assistant.models';
import { AssistantApiService } from '../../services/assistant-api.service';
import { AssistantLauncherService } from '../../services/assistant-launcher.service';

@Component({
  selector: 'app-assistant-page',
  imports: [FormsModule],
  templateUrl: './assistant-page.html',
  styleUrl: './assistant-page.scss',
})
export class AssistantPage {
  @ViewChild('chatBody')
  private chatBody?: ElementRef<HTMLElement>;

  @ViewChild('messageInput')
  private messageInput?: ElementRef<HTMLTextAreaElement>;

  private readonly assistantApi = inject(AssistantApiService);

  private readonly assistantLauncher =
    inject(AssistantLauncherService);

  private readonly trackCatalogService =
    inject(TrackCatalogService);

  readonly messages = signal<ChatMessage[]>([]);
  readonly isSending = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly isOpen = this.assistantLauncher.isOpen;
  readonly isMaximized = signal(false);

  readonly activeRecommendationQuestionKey =
    signal<RecommendationQuestionKey | null>(null);

  readonly latestRecommendedTracks =
    signal<RecommendedTrack[]>([]);

  readonly selectedComparisonTracks =
    signal<RecommendedTrack[]>([]);

  readonly comparisonTracks =
    signal<TrackCatalogItem[]>([]);

  readonly isComparisonOpen = signal(false);
  readonly isLoadingComparison = signal(false);
  readonly comparisonError = signal<string | null>(null);

  readonly selectedTrackDetails =
    signal<TrackCatalogItem | null>(null);

  readonly selectedTrackRecommendation =
    signal<RecommendedTrack | null>(null);

  readonly isTrackDetailsOpen = signal(false);
  readonly isLoadingTrackDetails = signal(false);
  readonly trackDetailsError = signal<string | null>(null);

  readonly canCompare = computed(
    () => this.selectedComparisonTracks().length >= 2,
  );

  readonly canOfferComparison = computed(
    () => this.latestRecommendedTracks().length >= 2,
  );

  message = '';
  sessionId: string | null = null;

  recommendationProgress: RecommendationQuestionnaireProgress = {
    major: null,
    technicalCourses: null,
    skills: null,
    interests: null,
    preferredActivities: null,
    careerGoals: null,
    additionalContext: null,
    skippedFields: [],
  };

  sendMessage(): void {
    const normalizedMessage = this.message.trim();

    if (!normalizedMessage || this.isSending()) {
      return;
    }

    const currentQuestionKey =
      this.activeRecommendationQuestionKey();

    if (currentQuestionKey) {
      this.applyRecommendationAnswer(
        currentQuestionKey,
        normalizedMessage,
      );

      this.activeRecommendationQuestionKey.set(null);
    }

    this.message = '';

    this.sendAssistantRequest(
      normalizedMessage,
      false,
    );
  }

  handleComposerKeydown(event: KeyboardEvent): void {
    if (
      event.key !== 'Enter' ||
      event.shiftKey
    ) {
      return;
    }

    event.preventDefault();
    this.sendMessage();
  }

  autoResizeMessageInput(event: Event): void {
    const textarea = event.target as HTMLTextAreaElement;

    textarea.style.height = 'auto';
    textarea.style.height =
      `${Math.min(textarea.scrollHeight, 112)}px`;
  }


  recommendNow(): void {
    if (this.isSending()) {
      return;
    }

    this.activeRecommendationQuestionKey.set(null);

    this.sendAssistantRequest(
      'Recommend based on the information available.',
      true,
    );
  }

  toggleChat(): void {
    const wasOpen = this.isOpen();

    this.assistantLauncher.toggle();

    if (!wasOpen) {
      this.scheduleScrollToBottom();
    }
  }

  toggleMaximize(): void {
    this.isMaximized.update(value => !value);
    this.scheduleScrollToBottom();
  }

  closeChat(): void {
    this.assistantLauncher.close();
    this.isMaximized.set(false);

    this.closeComparison();
    this.closeTrackDetails();
  }

  isTrackSelected(trackId: string): boolean {
    return this.selectedComparisonTracks().some(
      track => track.trackId === trackId,
    );
  }

  toggleTrackForComparison(track: RecommendedTrack): void {
    if (!this.canOfferComparison()) {
      return;
    }

    const selected = this.selectedComparisonTracks();

    const alreadySelected = selected.some(
      item => item.trackId === track.trackId,
    );

    if (alreadySelected) {
      this.selectedComparisonTracks.set(
        selected.filter(
          item => item.trackId !== track.trackId,
        ),
      );

      return;
    }

    if (selected.length >= 3) {
      return;
    }

    this.selectedComparisonTracks.set([
      ...selected,
      track,
    ]);
  }

  openTrackDetails(track: RecommendedTrack): void {
    if (this.isLoadingTrackDetails()) {
      return;
    }

    this.isLoadingTrackDetails.set(true);
    this.trackDetailsError.set(null);
    this.selectedTrackDetails.set(null);
    this.selectedTrackRecommendation.set(track);

    this.trackCatalogService
      .getTrack(track.trackId)
      .pipe(
        finalize(() => {
          this.isLoadingTrackDetails.set(false);
        }),
      )
      .subscribe({
        next: trackDetails => {
          this.selectedTrackDetails.set(trackDetails);
          this.isTrackDetailsOpen.set(true);
        },
        error: () => {
          this.selectedTrackRecommendation.set(null);

          this.trackDetailsError.set(
            'Could not load track details. Please try again.',
          );
        },
      });
  }

  closeTrackDetails(): void {
    this.isTrackDetailsOpen.set(false);
    this.selectedTrackDetails.set(null);
    this.selectedTrackRecommendation.set(null);
    this.trackDetailsError.set(null);
  }

  openComparison(): void {
    const selected = this.selectedComparisonTracks();

    if (
      !this.canOfferComparison() ||
      selected.length < 2 ||
      this.isLoadingComparison()
    ) {
      return;
    }

    this.isLoadingComparison.set(true);
    this.comparisonError.set(null);
    this.comparisonTracks.set([]);

    forkJoin(
      selected.map(track =>
        this.trackCatalogService.getTrack(track.trackId),
      ),
    )
      .pipe(
        finalize(() => {
          this.isLoadingComparison.set(false);
        }),
      )
      .subscribe({
        next: tracks => {
          this.comparisonTracks.set([...tracks]);
          this.isComparisonOpen.set(true);
        },
        error: () => {
          this.comparisonError.set(
            'Could not load track details for comparison.',
          );
        },
      });
  }

  closeComparison(): void {
    this.isComparisonOpen.set(false);
    this.comparisonError.set(null);
  }

  recommendationReason(trackId: string): string {
    return (
      this.selectedComparisonTracks().find(
        track => track.trackId === trackId,
      )?.reason ?? ''
    );
  }

  private applyRecommendationAnswer(
    questionKey: RecommendationQuestionKey,
    answer: string,
  ): void {
    if (this.isSkippedAnswer(answer)) {
      this.clearRecommendationField(questionKey);
      this.addSkippedField(questionKey);
      return;
    }

    this.removeSkippedField(questionKey);

    if (questionKey === 'major') {
      this.recommendationProgress = {
        ...this.recommendationProgress,
        major: answer.trim(),
      };

      return;
    }

    const values = this.parseAnswerValues(answer);

    this.recommendationProgress = {
      ...this.recommendationProgress,
      [questionKey]: values,
    };
  }

  private isSkippedAnswer(answer: string): boolean {
    const normalizedAnswer = answer
      .trim()
      .toLowerCase()
      .replace(/[.!?]+$/g, '');

    const skippedAnswers = new Set([
      'none',
      'no',
      'skip',
      'not yet',
      'nothing',
      'i have none',
      "i don't have any",
      'i do not have any',
      "i don't know",
      'i do not know',
      'معنديش',
      'مش عندي',
      'لا يوجد',
      'لا أعرف',
      'تخطي',
    ]);

    return skippedAnswers.has(normalizedAnswer);
  }

  private parseAnswerValues(answer: string): string[] {
    return answer
      .split(/[,;\n]+/)
      .map(value => value.trim())
      .filter(Boolean);
  }

  private clearRecommendationField(
    questionKey: RecommendationQuestionKey,
  ): void {
    if (questionKey === 'major') {
      this.recommendationProgress = {
        ...this.recommendationProgress,
        major: null,
      };

      return;
    }

    this.recommendationProgress = {
      ...this.recommendationProgress,
      [questionKey]: null,
    };
  }

  private addSkippedField(
    questionKey: RecommendationQuestionKey,
  ): void {
    const skippedFields = new Set(
      this.recommendationProgress.skippedFields,
    );

    skippedFields.add(questionKey);

    this.recommendationProgress = {
      ...this.recommendationProgress,
      skippedFields: [...skippedFields],
    };
  }

  private removeSkippedField(
    questionKey: RecommendationQuestionKey,
  ): void {
    this.recommendationProgress = {
      ...this.recommendationProgress,
      skippedFields:
        this.recommendationProgress.skippedFields.filter(
          field => field !== questionKey,
        ),
    };
  }

  private scheduleScrollToBottom(): void {
    setTimeout(() => {
      const container = this.chatBody?.nativeElement;

      if (!container) {
        return;
      }

      container.scrollTo({
        top: container.scrollHeight,
        behavior: 'smooth',
      });
    });
  }

  private resetMessageInputHeight(): void {
    const textarea = this.messageInput?.nativeElement;

    if (textarea) {
      textarea.style.height = 'auto';
    }
  }

  // crypto.randomUUID is undefined outside a secure context, and the site is served over HTTP.
  private createMessageId(): string {
    if (
      typeof crypto !== 'undefined' &&
      typeof crypto.randomUUID === 'function'
    ) {
      return crypto.randomUUID();
    }

    return `${Date.now().toString(36)}-${Math.random()
      .toString(36)
      .slice(2, 12)}`;
  }

  private sendAssistantRequest(
    message: string,
    recommendWithAvailableData: boolean,
  ): void {
    this.messages.update(messages => [
      ...messages,
      {
        id: this.createMessageId(),
        role: 'user',
        content: message,
        timestamp: new Date().toISOString(),
      },
    ]);

    this.resetMessageInputHeight();
    this.isSending.set(true);
    this.scheduleScrollToBottom();
    this.errorMessage.set(null);

    this.assistantApi
      .sendMessage({
        message,
        sessionId: this.sessionId,
        language: 'en',
        recommendation: this.recommendationProgress,
        recommendWithAvailableData,
      })
      .pipe(
        finalize(() => this.isSending.set(false)),
      )
      .subscribe({
        next: response => {
          this.sessionId = response.sessionId;

          const recommendationResult =
            response.results.find(
              result =>
                result.intent === 'recommendation',
            );

          const collectingRecommendationResult =
            response.results.find(
              result =>
                result.intent === 'recommendation' &&
                result.metadata.state ===
                  'collecting_answers',
            );

          this.activeRecommendationQuestionKey.set(
            collectingRecommendationResult
              ?.metadata.questionKey ?? null,
          );

          if (
            recommendationResult &&
            Array.isArray(
              recommendationResult.metadata.recommendations,
            )
          ) {
            const recommendations =
              recommendationResult.metadata.recommendations as
                RecommendedTrack[];

            this.latestRecommendedTracks.set(
              recommendations,
            );

            if (recommendations.length < 2) {
              this.selectedComparisonTracks.set([]);
              this.comparisonTracks.set([]);
              this.closeComparison();
            } else {
              const recommendationIds = new Set(
                recommendations.map(track => track.trackId),
              );

              this.selectedComparisonTracks.update(
                selected =>
                  selected.filter(track =>
                    recommendationIds.has(track.trackId),
                  ),
              );
            }
          }

          this.messages.update(messages => [
            ...messages,
            {
              id: this.createMessageId(),
              role: 'assistant',
              response,
              timestamp: response.timestamp,
            },
          ]);
          this.scheduleScrollToBottom();
        },
        error: () => {
          this.activeRecommendationQuestionKey.set(null);

          this.errorMessage.set(
            'Could not send the message. Please try again.',
          );
          this.scheduleScrollToBottom();
        },
      });
  }
}