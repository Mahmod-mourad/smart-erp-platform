import { Injectable, inject } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';

/**
 * Single entry point for transient user notifications (toasts). Wrapping MatSnackBar here keeps
 * styling/duration consistent across the app and lets non-component code (e.g. the error
 * interceptor) raise toasts without re-implementing the snackbar plumbing every time.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  error(message: string): void {
    this.show(message, 'Dismiss', { duration: 5000, panelClass: 'nf-snack-error' });
  }

  success(message: string): void {
    this.show(message, 'OK', { duration: 3000, panelClass: 'nf-snack-success' });
  }

  info(message: string): void {
    this.show(message, 'OK', { duration: 4000 });
  }

  private show(message: string, action: string, config: MatSnackBarConfig): void {
    this.snackBar.open(message, action, {
      horizontalPosition: 'right',
      verticalPosition: 'bottom',
      ...config,
    });
  }
}
