// Change 20260726
import type { RetryContext } from '@microsoft/signalr';
import { ExponentialBackoffRetryPolicy, NOTIFICATION_HUB_PATH } from './notification-hub.service';

function context(previousRetryCount: number): RetryContext {
  return { previousRetryCount, elapsedMilliseconds: 0, retryReason: new Error('dropped') };
}

describe('ExponentialBackoffRetryPolicy', () => {
  it('doubles the delay on each successive attempt', () => {
    // random() pinned to 0 so the returned delay is the un-jittered floor (half the ceiling).
    const policy = new ExponentialBackoffRetryPolicy(30_000, () => 0);

    expect(policy.nextRetryDelayInMilliseconds(context(0))).toBe(500);
    expect(policy.nextRetryDelayInMilliseconds(context(1))).toBe(1_000);
    expect(policy.nextRetryDelayInMilliseconds(context(2))).toBe(2_000);
    expect(policy.nextRetryDelayInMilliseconds(context(3))).toBe(4_000);
  });

  it('caps the delay at the ceiling and never gives up', () => {
    const policy = new ExponentialBackoffRetryPolicy(30_000, () => 1);

    expect(policy.nextRetryDelayInMilliseconds(context(20))).toBe(30_000);
    expect(policy.nextRetryDelayInMilliseconds(context(500))).toBe(30_000);
  });

  it('jitters within the upper half of the window', () => {
    const policy = new ExponentialBackoffRetryPolicy(30_000, () => 0.5);

    expect(policy.nextRetryDelayInMilliseconds(context(3))).toBe(6_000);
  });

  it('targets the hub path the API maps', () => {
    expect(NOTIFICATION_HUB_PATH).toBe('/hubs/notifications');
  });
});
