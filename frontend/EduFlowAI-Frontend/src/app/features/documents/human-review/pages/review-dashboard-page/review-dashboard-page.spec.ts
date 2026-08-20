import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ReviewDashboardPage } from './review-dashboard-page';

describe('ReviewDashboardPage', () => {
  let component: ReviewDashboardPage;
  let fixture: ComponentFixture<ReviewDashboardPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReviewDashboardPage],
    }).compileComponents();

    fixture = TestBed.createComponent(ReviewDashboardPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
