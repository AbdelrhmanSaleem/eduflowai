import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SimulateStagePage } from './simulate-stage-page';

describe('SimulateStagePage', () => {
  let component: SimulateStagePage;
  let fixture: ComponentFixture<SimulateStagePage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SimulateStagePage],
    }).compileComponents();

    fixture = TestBed.createComponent(SimulateStagePage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
