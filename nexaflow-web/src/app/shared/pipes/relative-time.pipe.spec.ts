import { RelativeTimePipe } from './relative-time.pipe';

describe('RelativeTimePipe', () => {
  const pipe = new RelativeTimePipe();
  const now = Date.now();
  const ago = (seconds: number) => new Date(now - seconds * 1000);

  it('returns an empty string for null/undefined/invalid input', () => {
    expect(pipe.transform(null)).toBe('');
    expect(pipe.transform(undefined)).toBe('');
    expect(pipe.transform('not-a-date')).toBe('');
  });

  it('treats sub-minute differences as "just now"', () => {
    expect(pipe.transform(ago(10))).toBe('just now');
  });

  it('formats minutes, hours and days in the past', () => {
    expect(pipe.transform(ago(5 * 60))).toContain('minute');
    expect(pipe.transform(ago(2 * 3600))).toContain('hour');
    expect(pipe.transform(ago(3 * 86400))).toContain('day');
  });

  it('accepts ISO strings as well as Date objects', () => {
    expect(pipe.transform(ago(2 * 3600).toISOString())).toContain('hour');
  });

  it('handles future dates', () => {
    expect(pipe.transform(new Date(now + 3 * 86400 * 1000))).toContain('in');
  });
});
