import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TrackComparisonPage } from './track-comparison-page';

describe('TrackComparisonPage', () => {
  let component: TrackComparisonPage;
  let fixture: ComponentFixture<TrackComparisonPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TrackComparisonPage],
    }).compileComponents();

    fixture = TestBed.createComponent(TrackComparisonPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
