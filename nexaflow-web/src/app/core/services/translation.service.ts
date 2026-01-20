import { Injectable, signal, effect } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Injectable({
  providedIn: 'root'
})
export class TranslationService {
  currentLanguage = signal<string>('en');

  constructor(private translate: TranslateService) {
    const savedLang = localStorage.getItem('language') || 'en';
    // this.translate.setDefaultLang('en');
    this.setLanguage(savedLang);

    // Watch the signal and update HTML dir attribute (RTL/LTR)
    effect(() => {
      const lang = this.currentLanguage();
      const dir = lang === 'ar' ? 'rtl' : 'ltr';
      document.documentElement.dir = dir;
      document.documentElement.lang = lang;
      
      // Also set the theme class on body to ensure Material updates correctly
      if (lang === 'ar') {
        document.body.classList.add('rtl-theme');
      } else {
        document.body.classList.remove('rtl-theme');
      }
    });
  }

  setLanguage(lang: string) {
    this.translate.use(lang);
    this.currentLanguage.set(lang);
    localStorage.setItem('language', lang);
  }

  toggleLanguage() {
    const nextLang = this.currentLanguage() === 'en' ? 'ar' : 'en';
    this.setLanguage(nextLang);
  }
}
