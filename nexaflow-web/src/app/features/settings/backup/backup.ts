import { Component, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-backup',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatCardModule, MatIconModule, MatSnackBarModule],
  templateUrl: './backup.html',
  styles: [`
    .backup-container { padding: 24px; max-width: 800px; margin: 0 auto; }
    mat-card { margin-bottom: 24px; }
    .action-row { margin-top: 16px; display: flex; gap: 16px; }
  `]
})
export class BackupComponent {
  private readonly http = inject(HttpClient);
  private readonly snackBar = inject(MatSnackBar);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/backup`;

  isCreating = false;
  isRestoring = false;

  createBackup() {
    this.isCreating = true;
    this.http.post<{ fileUrl: string, message: string }>(`${this.baseUrl}/create`, {}).subscribe({
      next: (res) => {
        this.isCreating = false;
        this.snackBar.open(res.message, 'Close', { duration: 3000 });
        
        // Auto trigger download
        if (res.fileUrl) {
          window.open(`${environment.apiBaseUrl}${res.fileUrl}`, '_blank');
        }
      },
      error: () => {
        this.isCreating = false;
        this.snackBar.open('Failed to create backup', 'Close', { duration: 3000 });
      }
    });
  }

  onFileSelected(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) {
      this.restoreBackup(file);
    }
  }

  restoreBackup(file: File) {
    this.isRestoring = true;
    const formData = new FormData();
    formData.append('file', file);

    this.http.post<{ message: string }>(`${this.baseUrl}/restore`, formData).subscribe({
      next: (res) => {
        this.isRestoring = false;
        this.snackBar.open(res.message, 'Close', { duration: 3000 });
      },
      error: () => {
        this.isRestoring = false;
        this.snackBar.open('Failed to restore backup.', 'Close', { duration: 3000 });
      }
    });
  }
}
