import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslatePipe } from '@ngx-translate/core';
import { BranchService, Branch } from '../../../core/services/branch.service';

import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-branch-list',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatTableModule, MatCheckboxModule, TranslatePipe
  ],
  templateUrl: './branch-list.html',
  styleUrl: './branch-list.scss'
})
export class BranchList implements OnInit {
  private readonly fb = inject(FormBuilder);
  public readonly branchService = inject(BranchService);
  private readonly snack = inject(MatSnackBar);

  displayedColumns: string[] = ['name', 'city', 'phone', 'isHeadquarters', 'actions'];

  form = this.fb.nonNullable.group({
    id: [''],
    name: ['', [Validators.required]],
    city: [''],
    address: [''],
    phone: [''],
    isHeadquarters: [false]
  });

  isEditing = false;

  ngOnInit() {
    this.branchService.loadBranches();
  }

  save() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const val = this.form.getRawValue();
    const dto = {
      name: val.name,
      city: val.city,
      address: val.address,
      phone: val.phone,
      isHeadquarters: val.isHeadquarters
    };

    if (this.isEditing && val.id) {
      this.branchService.updateBranch(val.id, dto).subscribe({
        next: () => {
          this.snack.open('Branch updated', 'OK', { duration: 3000 });
          this.reset();
        },
        error: () => this.snack.open('Error updating branch', 'OK', { duration: 3000 })
      });
    } else {
      this.branchService.createBranch(dto).subscribe({
        next: () => {
          this.snack.open('Branch created', 'OK', { duration: 3000 });
          this.reset();
        },
        error: () => this.snack.open('Error creating branch', 'OK', { duration: 3000 })
      });
    }
  }

  edit(b: Branch) {
    this.isEditing = true;
    this.form.patchValue({
      id: b.id,
      name: b.name,
      city: b.city || '',
      address: b.address || '',
      phone: b.phone || '',
      isHeadquarters: b.isHeadquarters
    });
  }

  delete(b: Branch) {
    if (confirm('Are you sure you want to delete this branch?')) {
      this.branchService.deleteBranch(b.id).subscribe({
        next: () => this.snack.open('Branch deleted', 'OK', { duration: 3000 }),
        error: () => this.snack.open('Error deleting branch', 'OK', { duration: 3000 })
      });
    }
  }

  reset() {
    this.isEditing = false;
    this.form.reset({ id: '', name: '', city: '', address: '', phone: '', isHeadquarters: false });
  }
}
