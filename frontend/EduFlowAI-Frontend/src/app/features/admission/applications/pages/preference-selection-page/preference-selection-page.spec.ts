import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PreferenceSelectionPage } from './preference-selection-page';

describe('PreferenceSelectionPage', () => {
  let component: PreferenceSelectionPage;
  let fixture: ComponentFixture<PreferenceSelectionPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PreferenceSelectionPage],
    }).compileComponents();

    fixture = TestBed.createComponent(PreferenceSelectionPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
