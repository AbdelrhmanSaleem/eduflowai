import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FinalRankingPage } from './final-ranking-page';

describe('FinalRankingPage', () => {
  let component: FinalRankingPage;
  let fixture: ComponentFixture<FinalRankingPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FinalRankingPage],
    }).compileComponents();

    fixture = TestBed.createComponent(FinalRankingPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
