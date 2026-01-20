import { Injectable, signal, effect, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export enum ThemeMode {
  Light = 0,
  Dark = 1,
  Auto = 2
}

export interface UserPreferences {
  themeMode: ThemeMode;
  primaryColor: string | null;
  secondaryColor: string | null;
  language: string;
}

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private http = inject(HttpClient);
  
  public currentTheme = signal<ThemeMode>(ThemeMode.Light);
  public primaryColor = signal<string | null>(null);

  constructor() {
    this.loadPreferences();

    // Effect to apply theme class
    effect(() => {
      const mode = this.currentTheme();
      let isDark = false;
      
      if (mode === ThemeMode.Auto) {
        isDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
      } else {
        isDark = mode === ThemeMode.Dark;
      }

      if (isDark) {
        document.body.classList.add('dark-theme');
      } else {
        document.body.classList.remove('dark-theme');
      }
    });

    // Effect to apply custom colors
    effect(() => {
      const pColor = this.primaryColor();
      if (pColor) {
        document.documentElement.style.setProperty('--mat-sys-primary', pColor);
      } else {
        document.documentElement.style.removeProperty('--mat-sys-primary');
      }
    });

    // Listen for system theme changes if set to auto
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
      if (this.currentTheme() === ThemeMode.Auto) {
        if (e.matches) {
          document.body.classList.add('dark-theme');
        } else {
          document.body.classList.remove('dark-theme');
        }
      }
    });
  }

  loadPreferences() {
    this.http.get<UserPreferences>(`${environment.apiBaseUrl}/api/preferences`).subscribe(res => {
      this.currentTheme.set(res.themeMode);
      this.primaryColor.set(res.primaryColor);
    });
  }

  setTheme(mode: ThemeMode) {
    this.currentTheme.set(mode);
    this.savePreferences();
  }

  setPrimaryColor(color: string | null) {
    this.primaryColor.set(color);
    this.savePreferences();
  }

  private savePreferences() {
    const prefs: UserPreferences = {
      themeMode: this.currentTheme(),
      primaryColor: this.primaryColor(),
      secondaryColor: null,
      language: localStorage.getItem('language') || 'en'
    };
    this.http.put(`${environment.apiBaseUrl}/api/preferences`, prefs).subscribe();
  }
}
