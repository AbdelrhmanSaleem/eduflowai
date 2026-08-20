import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FieldComparison } from './field-comparison';

describe('FieldComparison', () => {
  let component: FieldComparison;
  let fixture: ComponentFixture<FieldComparison>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FieldComparison],
    }).compileComponents();

    fixture = TestBed.createComponent(FieldComparison);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
