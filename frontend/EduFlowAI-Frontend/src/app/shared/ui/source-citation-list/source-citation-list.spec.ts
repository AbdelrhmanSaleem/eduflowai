import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SourceCitationList } from './source-citation-list';

describe('SourceCitationList', () => {
  let component: SourceCitationList;
  let fixture: ComponentFixture<SourceCitationList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SourceCitationList],
    }).compileComponents();

    fixture = TestBed.createComponent(SourceCitationList);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
