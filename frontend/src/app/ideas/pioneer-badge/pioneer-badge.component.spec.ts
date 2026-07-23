import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PioneerBadgeComponent } from './pioneer-badge.component';

describe('PioneerBadgeComponent', () => {
  let fixture: ComponentFixture<PioneerBadgeComponent>;

  async function setup(stage: number): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [PioneerBadgeComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(PioneerBadgeComponent);
    fixture.componentRef.setInput('stage', stage);
    fixture.detectChanges();
  }

  it('renders the Pioneer badge when stage is 6', async () => {
    await setup(6);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Pioneer');
  });

  it('renders the Pioneer badge when stage is above 6', async () => {
    await setup(8);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Pioneer');
  });

  it('does not render when stage is below 6', async () => {
    await setup(5);
    expect((fixture.nativeElement as HTMLElement).textContent?.trim()).toBe('');
  });
});
