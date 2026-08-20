import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RecommendationQuestionnairePage } from './recommendation-questionnaire-page';

describe('RecommendationQuestionnairePage', () => {
  let component: RecommendationQuestionnairePage;
  let fixture: ComponentFixture<RecommendationQuestionnairePage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RecommendationQuestionnairePage],
    }).compileComponents();

    fixture = TestBed.createComponent(RecommendationQuestionnairePage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
