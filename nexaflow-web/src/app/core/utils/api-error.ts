import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from '../models/auth.models';

/** Turns an HTTP error (our ProblemDetails shape) into a readable message. */
export function readApiError(err: unknown): string {
  if (err instanceof HttpErrorResponse) {
    const problem = err.error as ProblemDetails | undefined;
    if (problem?.errors) {
      return Object.values(problem.errors).flat().join(' ');
    }
    if (problem?.title) return problem.title;
    if (err.status === 0) return 'Cannot reach the server. Is the API running?';
  }
  return 'Something went wrong. Please try again.';
}
