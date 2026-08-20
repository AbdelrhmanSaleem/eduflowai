import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NotificationPreferencesPage } from './notification-preferences-page';

describe('NotificationPreferencesPage', () => {
  let component: NotificationPreferencesPage;
  let fixture: ComponentFixture<NotificationPreferencesPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NotificationPreferencesPage],
    }).compileComponents();

    fixture = TestBed.createComponent(NotificationPreferencesPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
