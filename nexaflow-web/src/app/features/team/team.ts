import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TeamService, InvitationDto } from '../../core/services/team.service';
import { readApiError } from '../../core/utils/api-error';

@Component({
  selector: 'app-team',
  imports: [
    ReactiveFormsModule, DatePipe, MatCardModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatIconModule, MatListModule,
  ],
  templateUrl: './team.html',
  styleUrl: './team.scss',
})
export class Team {
  private readonly fb = inject(FormBuilder);
  private readonly team = inject(TeamService);
  private readonly snack = inject(MatSnackBar);

  readonly roles = ['CompanyAdmin', 'Manager', 'Employee'];
  readonly invitations = signal<InvitationDto[]>([]);
  readonly loading = signal(false);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    roleName: ['Employee', [Validators.required]],
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.team.getPending().subscribe({
      next: (items) => this.invitations.set(items),
      error: (err) => this.snack.open(readApiError(err), 'Dismiss', { duration: 4000 }),
    });
  }

  invite(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.team.invite(this.form.getRawValue()).subscribe({
      next: () => {
        this.snack.open('Invitation sent.', 'OK', { duration: 3000 });
        this.form.reset({ email: '', roleName: 'Employee' });
        this.loading.set(false);
        this.load();
      },
      error: (err) => {
        this.snack.open(readApiError(err), 'Dismiss', { duration: 4000 });
        this.loading.set(false);
      },
    });
  }

  revoke(inv: InvitationDto): void {
    this.team.revoke(inv.id).subscribe({
      next: () => this.load(),
      error: (err) => this.snack.open(readApiError(err), 'Dismiss', { duration: 4000 }),
    });
  }
}
