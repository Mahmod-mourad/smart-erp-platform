import {
  HttpContext,
  HttpContextToken,
  HttpErrorResponse,
  HttpInterceptorFn,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../services/notification.service';
import { readApiError } from '../utils/api-error';

/**
 * Per-request opt-out. Requests that render their own error UI (auth screens, the team page,
 * the leave-request form) set this token so the global interceptor stays silent and we never
 * show two messages for one failure.
 */
export const SKIP_ERROR_NOTIFICATION = new HttpContextToken<boolean>(() => false);

/** Convenience for callers: `this.http.post(url, body, { context: skipErrorNotification() })`. */
export function skipErrorNotification(context: HttpContext = new HttpContext()): HttpContext {
  return context.set(SKIP_ERROR_NOTIFICATION, true);
}

/**
 * Global safety net for HTTP failures. Surfaces a toast for any error that no one else owns,
 * then re-throws so component-level handlers still run. Deliberately stays out of two lanes:
 *   - 401 is owned by {@link authInterceptor}, which transparently refreshes the token.
 *   - Requests that set {@link SKIP_ERROR_NOTIFICATION} handle their own messaging.
 * Register this as the OUTER interceptor (before authInterceptor) so it observes the final
 * outcome of a request after any 401 refresh-and-retry, not the intermediate 401.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notifications = inject(NotificationService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const handledElsewhere = req.context.get(SKIP_ERROR_NOTIFICATION) || err.status === 401;
      if (!handledElsewhere) {
        notifications.error(readApiError(err));
      }
      return throwError(() => err);
    }),
  );
};
