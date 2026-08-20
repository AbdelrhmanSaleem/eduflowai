import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RecommendationResultsPage } from './recommendation-results-page';

describe('RecommendationResultsPage', () => {
  let component: RecommendationResultsPage;
  let fixture: ComponentFixture<RecommendationResultsPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RecommendationResultsPage],
    }).compileComponents();

    fixture = TestBed.createComponent(RecommendationResultsPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
