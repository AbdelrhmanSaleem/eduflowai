import { computed, inject } from '@angular/core';
import { tapResponse } from '@ngrx/operators';
import {
  patchState,
  signalStore,
  withComputed,
  withMethods,
  withState,
} from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';

import { AuthSessionStore } from '../../../../core/auth/auth-session.store';
import { ApiError } from '../../../../core/errors/api-problem';
import { LocaleStore } from '../../../../core/i18n/locale.store';
import {
  ApplicantProfile,
  PROFILE_FIELD_NAMES,
  ProfileFieldName,
  UpdateApplicantProfileRequest,
} from '../models/profile.model';
import { ProfileApi } from './profile.api';

type ProfileState = {
  profile: ApplicantProfile | null;
  isLoading: boolean;
  isSaving: boolean;
  hasLoaded: boolean;
  loadError: string | null;
  saveError: string | null;
  validationErrors: Partial<Record<ProfileFieldName, string[]>>;
  savedAt: number | null;
};

const initialState: ProfileState = {
  profile: null,
  isLoading: false,
  isSaving: false,
  hasLoaded: false,
  loadError: null,
  saveError: null,
  validationErrors: {},
  savedAt: null,
};

const knownFields = new Map(
  PROFILE_FIELD_NAMES.map((field) => [field.toLowerCase(), field]),
);

function normalizeErrorKey(key: string): ProfileFieldName | null {
  const leaf = key
    .replace(/^\$\./, '')
    .replace(/^\$/, '')
    .split('.')
    .at(-1)
    ?.replace(/[^a-zA-Z0-9]/g, '')
    .toLowerCase();

  return leaf ? (knownFields.get(leaf) ?? null) : null;
}

function parseApiError(error: unknown): {
  message: string;
  fields: Partial<Record<ProfileFieldName, string[]>>;
} {
  const problem = error as Partial<ApiError>;
  const fields: Partial<Record<ProfileFieldName, string[]>> = {};
  const generalMessages: string[] = [];

  if (problem.errors && typeof problem.errors === 'object') {
    for (const [key, messages] of Object.entries(problem.errors)) {
      const field = normalizeErrorKey(key);

      if (field && Array.isArray(messages)) {
        fields[field] = messages.map(String);
      } else if (Array.isArray(messages)) {
        generalMessages.push(...messages.map(String));
      }
    }
  }

  return {
    message:
      generalMessages[0] ||
      problem.detail ||
      problem.title ||
      (error instanceof Error ? error.message : null) ||
      'We could not save your profile. Please try again.',
    fields,
  };
}

export const ProfileStore = signalStore(
  withState(initialState),
  withComputed(({ profile }) => ({
    isComplete: computed(() => profile()?.isComplete ?? false),
    isLocked: computed(() => profile()?.isProfileLocked ?? false),
  })),
  withMethods(
    (
      store,
      api = inject(ProfileApi),
      session = inject(AuthSessionStore),
      locale = inject(LocaleStore),
    ) => ({
      load: rxMethod<void>(
        pipe(
          tap(() =>
            patchState(store, {
              isLoading: true,
              loadError: null,
              saveError: null,
            }),
          ),
          switchMap(() =>
            api.getProfile().pipe(
              tapResponse({
                next: (profile) => {
                  session.markProfileComplete(profile.isComplete);
                  locale.setLocale(profile.preferredLanguage);
                  patchState(store, {
                    profile,
                    hasLoaded: true,
                    loadError: null,
                  });
                },
                error: (error: unknown) => {
                  const parsed = parseApiError(error);
                  patchState(store, {
                    hasLoaded: true,
                    loadError: parsed.message,
                  });
                },
                finalize: () => patchState(store, { isLoading: false }),
              }),
            ),
          ),
        ),
      ),

      save: rxMethod<UpdateApplicantProfileRequest>(
        pipe(
          tap(() =>
            patchState(store, {
              isSaving: true,
              saveError: null,
              validationErrors: {},
              savedAt: null,
            }),
          ),
          switchMap((request) =>
            api.updateProfile(request).pipe(
              tapResponse({
                next: (profile) => {
                  session.markProfileComplete(profile.isComplete);
                  locale.setLocale(profile.preferredLanguage);
                  patchState(store, {
                    profile,
                    saveError: null,
                    validationErrors: {},
                    savedAt: Date.now(),
                  });
                },
                error: (error: unknown) => {
                  const parsed = parseApiError(error);
                  patchState(store, {
                    saveError: parsed.message,
                    validationErrors: parsed.fields,
                  });
                },
                finalize: () => patchState(store, { isSaving: false }),
              }),
            ),
          ),
        ),
      ),

      clearFieldError(field: ProfileFieldName): void {
        const current = store.validationErrors();

        const next = { ...current };
        delete next[field];
        patchState(store, {
          validationErrors: next,
          saveError: null,
          savedAt: null,
        });
      },

      clearSaveFeedback(): void {
        patchState(store, {
          saveError: null,
          validationErrors: {},
          savedAt: null,
        });
      },
    }),
  ),
);
