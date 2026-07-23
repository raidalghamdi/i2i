import { Component, computed, input } from '@angular/core';

type StepState = 'done' | 'current' | 'upcoming';

@Component({
  selector: 'app-post-program-stepper',
  templateUrl: './post-program-stepper.component.html',
})
export class PostProgramStepperComponent {
  readonly status = input.required<string>();

  private readonly order = ['in_pilot', 'in_measurement', 'in_scaling'];

  readonly steps = computed(() => {
    const current = this.order.indexOf(this.status());
    const labels = [
      $localize`:@@postProgramStepPilot:Pilot`,
      $localize`:@@postProgramStepMeasurement:Measurement`,
      $localize`:@@postProgramStepScaling:Scaling`,
    ];
    return this.order.map((key, i) => ({
      key,
      label: labels[i],
      state: (current > i ? 'done' : current === i ? 'current' : 'upcoming') as StepState,
    }));
  });
}
