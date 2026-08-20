import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { Subject } from 'rxjs';

import { NotificationRealtimeService } from '../../data-access/notification-realtime.service';
import { NotificationItem } from '../../models/notification.model';
import { NotificationCenterPage } from './notification-center-page';

describe('NotificationCenterPage', () => {
  const originalMatchMedia = window.matchMedia;
  let component: NotificationCenterPage;
  let fixture: ComponentFixture<NotificationCenterPage>;
  let http: HttpTestingController;
  let realtimeNotifications: Subject<NotificationItem>;

  beforeEach(async () => {
    realtimeNotifications = new Subject<NotificationItem>();
    await TestBed.configureTestingModule({
      imports: [NotificationCenterPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: NotificationRealtimeService,
          useValue: { notificationCreated$: realtimeNotifications.asObservable() },
        },
      ],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    Object.defineProperty(window, 'matchMedia', {
      configurable: true,
      value: originalMatchMedia,
    });
  });

  it('should create', () => {
    createComponent([]);
    expect(component).toBeTruthy();
  });

  it('marks the initially opened unread notification as read', () => {
    createComponent([
      {
        id: 'notification-1',
        message: 'Upload the requested document.',
        type: 1,
        isRead: false,
        createdAt: '2026-08-01T10:00:00Z',
      },
    ]);

    const request = http.expectOne('/api/communication/notifications/notification-1/read');
    expect(request.request.method).toBe('PATCH');
    request.flush(null, { status: 204, statusText: 'No Content' });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.unread-dot')).toBeNull();
    expect(fixture.nativeElement.querySelector('.status-pill')?.textContent).toContain('Read');
  });

  it('marks all loaded notifications as read from the toolbar action', () => {
    createComponent([
      {
        id: 'notification-1',
        message: 'Your first notification.',
        type: 1,
        isRead: true,
        createdAt: '2026-08-01T10:00:00Z',
      },
      {
        id: 'notification-2',
        message: 'Your second notification.',
        type: 5,
        isRead: false,
        createdAt: '2026-08-01T09:00:00Z',
      },
    ]);

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('.mark-all-button');
    button.click();

    const request = http.expectOne('/api/communication/notifications/read-all');
    expect(request.request.method).toBe('PATCH');
    request.flush({ updatedCount: 1 });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('.unread-dot')).toHaveLength(0);
  });

  it('switches from the inbox to detail and back in compact view', () => {
    useCompactViewport();
    createComponent([
      {
        id: 'notification-1',
        message: 'Open this notification.',
        type: 5,
        isRead: false,
        createdAt: '2026-08-01T10:00:00Z',
      },
    ]);

    http.expectNone('/api/communication/notifications/notification-1/read');

    const notification: HTMLButtonElement =
      fixture.nativeElement.querySelector('.notification-item');
    notification.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.notification-list-panel').style.display).toBe(
      'none',
    );
    expect(fixture.nativeElement.querySelector('.notification-detail').style.display).toBe('');

    http
      .expectOne('/api/communication/notifications/notification-1/read')
      .flush(null, { status: 204, statusText: 'No Content' });

    const backButton: HTMLButtonElement = fixture.nativeElement.querySelector('.back-to-inbox');
    backButton.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.notification-list-panel').style.display).toBe('');
    expect(fixture.nativeElement.querySelector('.notification-detail').style.display).toBe('none');
  });

  it('adds a matching real-time notification without marking it as read', () => {
    createComponent([
      {
        id: 'notification-1',
        message: 'Existing notification.',
        type: 1,
        isRead: true,
        createdAt: '2026-08-01T10:00:00Z',
      },
    ]);

    realtimeNotifications.next({
      id: 'notification-2',
      message: 'A replacement document is required.',
      type: 'DocumentReplacementRequested',
      isRead: false,
      createdAt: '2026-08-04T10:00:00Z',
    });
    fixture.detectChanges();

    const items = fixture.nativeElement.querySelectorAll('.notification-item');
    expect(items).toHaveLength(2);
    expect(items[0].textContent).toContain('A replacement document is required.');
    expect(items[0].querySelector('.unread-dot')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.notification-count').textContent).toContain('2');
    http.expectNone('/api/communication/notifications/notification-2/read');
  });

  function createComponent(data: object[]): void {
    fixture = TestBed.createComponent(NotificationCenterPage);
    component = fixture.componentInstance;
    fixture.detectChanges();

    http
      .expectOne(
        (request) =>
          request.url === '/api/communication/notifications' &&
          request.params.get('Page') === '1' &&
          request.params.get('PageSize') === '10',
      )
      .flush({
        data,
        filters: null,
        currentPage: 1,
        totalPages: data.length > 0 ? 1 : 0,
        pageSize: 10,
        totalCount: data.length,
      });

    fixture.detectChanges();
  }

  function useCompactViewport(): void {
    Object.defineProperty(window, 'matchMedia', {
      configurable: true,
      value: () =>
        ({
          matches: true,
          media: '(max-width: 55rem)',
          onchange: null,
          addListener: () => undefined,
          removeListener: () => undefined,
          addEventListener: () => undefined,
          removeEventListener: () => undefined,
          dispatchEvent: () => true,
        }) as MediaQueryList,
    });
  }
});
