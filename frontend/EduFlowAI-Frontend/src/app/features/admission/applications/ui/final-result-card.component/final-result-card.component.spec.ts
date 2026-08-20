import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FinalResultCardComponent } from './final-result-card.component';

describe('FinalResultCardComponent', () => {
  let component: FinalResultCardComponent;
  let fixture: ComponentFixture<FinalResultCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FinalResultCardComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(FinalResultCardComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
