import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FormField } from './form-field';

describe('FormField', () => {
  let component: FormField;
  let fixture: ComponentFixture<FormField>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormField],
    }).compileComponents();

    fixture = TestBed.createComponent(FormField);
    fixture.componentRef.setInput('label', 'Email');
    fixture.componentRef.setInput('forId', 'email');
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
