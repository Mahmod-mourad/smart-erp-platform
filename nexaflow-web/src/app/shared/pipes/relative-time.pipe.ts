import { Pipe, PipeTransform } from '@angular/core';

const MINUTE = 60;
const HOUR = 60 * MINUTE;
const DAY = 24 * HOUR;
const WEEK = 7 * DAY;
const MONTH = 30 * DAY;
const YEAR = 365 * DAY;

/**
 * Formats a date as a human-friendly relative string ("2 hours ago", "yesterday", "in 3 days").
 * Used in the activity timeline and anywhere a precise timestamp is less useful than recency.
 * Built on Intl.RelativeTimeFormat so it is locale-aware with no extra dependency.
 */
@Pipe({ name: 'relativeTime' })
export class RelativeTimePipe implements PipeTransform {
  private readonly rtf = new Intl.RelativeTimeFormat(undefined, { numeric: 'auto' });

  transform(value: Date | string | number | null | undefined): string {
    if (value === null || value === undefined) return '';

    const date = value instanceof Date ? value : new Date(value);
    const ms = date.getTime();
    if (Number.isNaN(ms)) return '';

    const seconds = Math.round((ms - Date.now()) / 1000);
    const abs = Math.abs(seconds);

    if (abs < MINUTE) return 'just now';
    if (abs < HOUR) return this.rtf.format(Math.round(seconds / MINUTE), 'minute');
    if (abs < DAY) return this.rtf.format(Math.round(seconds / HOUR), 'hour');
    if (abs < WEEK) return this.rtf.format(Math.round(seconds / DAY), 'day');
    if (abs < MONTH) return this.rtf.format(Math.round(seconds / WEEK), 'week');
    if (abs < YEAR) return this.rtf.format(Math.round(seconds / MONTH), 'month');
    return this.rtf.format(Math.round(seconds / YEAR), 'year');
  }
}
