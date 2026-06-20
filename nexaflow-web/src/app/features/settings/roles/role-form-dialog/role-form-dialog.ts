import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { TranslatePipe } from '@ngx-translate/core';
import { RoleDto } from '../../../../shared/models/role.models';
import { RoleService } from '../../../../core/services/role.service';

@Component({
  selector: 'app-role-form-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatButtonModule, MatInputModule, MatCheckboxModule, TranslatePipe],
  templateUrl: './role-form-dialog.html',
  styles: [`
    .permissions-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 10px;
      margin-top: 15px;
    }
  `]
})
export class RoleFormDialog {
  private readonly fb = inject(FormBuilder);
  private readonly ref = inject(MatDialogRef<RoleFormDialog>);
  protected readonly data = inject<RoleDto | null>(MAT_DIALOG_DATA);
  public readonly roleService = inject(RoleService);

  form = this.fb.group({
    name: [this.data?.name || '', [Validators.required]],
    description: [this.data?.description || '']
  });

  selectedPermissions = new Set<string>(this.data?.permissions || []);

  togglePermission(perm: string, checked: boolean) {
    if (checked) {
      this.selectedPermissions.add(perm);
    } else {
      this.selectedPermissions.delete(perm);
    }
  }

  save() {
    if (this.form.valid) {
      this.ref.close({
        ...this.form.value,
        permissions: Array.from(this.selectedPermissions)
      });
    }
  }
}
