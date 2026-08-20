import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ApplicationCreatePage } from './application-create-page';

describe('ApplicationCreatePage', () => {
  let component: ApplicationCreatePage;
  let fixture: ComponentFixture<ApplicationCreatePage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ApplicationCreatePage],
    }).compileComponents();

    fixture = TestBed.createComponent(ApplicationCreatePage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
