import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FeedbackCountBadgeComponent } from './feedback-count-badge.component';

describe('FeedbackCountBadgeComponent', () => {
  let fixture: ComponentFixture<FeedbackCountBadgeComponent>;

  async function setup(count: number): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [FeedbackCountBadgeComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(FeedbackCountBadgeComponent);
    fixture.componentRef.setInput('count', count);
    fixture.detectChanges();
  }

  it('renders the count and label when count is greater than 0', async () => {
    await setup(3);
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('3');
    expect(text).toContain('feedback');
  });

  it('does not render when count is 0', async () => {
    await setup(0);
    expect((fixture.nativeElement as HTMLElement).textContent?.trim()).toBe('');
  });
});
