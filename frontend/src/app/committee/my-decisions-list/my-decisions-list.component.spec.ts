import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommitteeApiService } from '../committee-api.service';
import { MyCommitteeDecision } from '../committee.model';
import { MyDecisionsListComponent } from './my-decisions-list.component';

describe('MyDecisionsListComponent', () => {
  let fixture: ComponentFixture<MyDecisionsListComponent>;
  let committeeApi: jasmine.SpyObj<CommitteeApiService>;

  function setup(decisions: MyCommitteeDecision[]): void {
    committeeApi = jasmine.createSpyObj('CommitteeApiService', ['getMine', 'getAttachment']); // Change 20260726
    committeeApi.getMine.and.returnValue(Promise.resolve(decisions));

    TestBed.configureTestingModule({
      imports: [MyDecisionsListComponent],
      providers: [{ provide: CommitteeApiService, useValue: committeeApi }],
    });
    fixture = TestBed.createComponent(MyDecisionsListComponent);
  }

  it('renders one row per decision with score', async () => {
    setup([
      { id: 'decision-1', ideaId: 'idea-1', ideaCode: 'IDEA-0001', ideaTitleEn: 'One', totalScore: 8, decidedAt: '2026-01-01' },
    ]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('IDEA-0001');
    expect(fixture.nativeElement.textContent).toContain('8');
  });

  // Change 20260726
  describe('decision attachments', () => {
    const withAttachments: MyCommitteeDecision[] = [
      {
        id: 'decision-1', ideaId: 'idea-1', ideaCode: 'IDEA-0001', ideaTitleEn: 'One', totalScore: 8, decidedAt: '2026-01-01',
        attachments: [
          { id: 'att-1', fileName: 'minutes.pdf', contentType: 'application/pdf', sizeBytes: 100, uploadedAt: '2026-01-01' },
          { id: 'att-2', fileName: 'chart.png', contentType: 'image/png', sizeBytes: 200, uploadedAt: '2026-01-01' },
        ],
      },
    ];

    async function render(decisions: MyCommitteeDecision[]): Promise<void> {
      setup(decisions);
      fixture.detectChanges();
      await fixture.componentInstance.ngOnInit();
      fixture.detectChanges();
    }

    it('renders a clickable button per attachment', async () => {
      await render(withAttachments);

      const links = Array.from(fixture.nativeElement.querySelectorAll('td button')) as HTMLButtonElement[];
      expect(links.map((b) => b.textContent!.trim())).toEqual(['minutes.pdf', 'chart.png']);
    });

    it('shows a placeholder when a decision has no attachments', async () => {
      await render([
        { id: 'decision-1', ideaId: 'idea-1', ideaCode: 'IDEA-0001', ideaTitleEn: 'One', totalScore: 8, decidedAt: '2026-01-01', attachments: [] },
      ]);

      expect(fixture.nativeElement.querySelectorAll('td button').length).toBe(0);
      expect(fixture.nativeElement.textContent).toContain('—');
    });

    it('fetches the attachment through the API when its name is clicked', async () => {
      await render(withAttachments);
      committeeApi.getAttachment.and.returnValue(Promise.resolve(new Blob(['x'], { type: 'application/pdf' })));
      spyOn(window, 'open').and.returnValue(null);

      await fixture.componentInstance.openAttachment('decision-1', withAttachments[0].attachments![0]);

      expect(committeeApi.getAttachment).toHaveBeenCalledWith('decision-1', 'att-1');
      expect(fixture.componentInstance.attachmentError()).toBeNull();
    });

    it('surfaces a download failure without hiding the table', async () => {
      await render(withAttachments);
      committeeApi.getAttachment.and.returnValue(Promise.reject(new Error('boom')));
      spyOn(window, 'open').and.returnValue(null);

      await fixture.componentInstance.openAttachment('decision-1', withAttachments[0].attachments![0]);
      fixture.detectChanges();

      expect(fixture.componentInstance.attachmentError()).not.toBeNull();
      expect(fixture.componentInstance.error()).toBeNull();
      expect(fixture.nativeElement.querySelector('table')).toBeTruthy();
    });
  });

  it('shows an empty-state message when there are no decisions', async () => {
    setup([]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain("haven't submitted");
  });

  it('shows the error state and retries the fetch when "Try again" is clicked', async () => {
    setup([]);
    committeeApi.getMine.and.returnValue(Promise.reject(new Error('boom')));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).not.toBeNull();
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).toBeTruthy();

    const decisions: MyCommitteeDecision[] = [
      { id: 'decision-1', ideaId: 'idea-1', ideaCode: 'IDEA-0001', ideaTitleEn: 'One', totalScore: 8, decidedAt: '2026-01-01' },
    ];
    committeeApi.getMine.and.returnValue(Promise.resolve(decisions));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).toBeNull();
    expect(fixture.componentInstance.decisions().length).toBe(1);
  });
});
