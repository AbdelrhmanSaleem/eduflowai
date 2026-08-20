import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ApplicationStatusPage } from './application-status-page';

describe('ApplicationStatusPage', () => {
  let component: ApplicationStatusPage;
  let fixture: ComponentFixture<ApplicationStatusPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ApplicationStatusPage],
    }).compileComponents();

    fixture = TestBed.createComponent(ApplicationStatusPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
