import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { TranslatePipe } from '@ngx-translate/core';
import { TranslationService } from '../../core/services/translation.service';
import { ThemeService, ThemeMode } from '../../core/services/theme.service';
import { AuthService } from '../../core/services/auth.service';
import { SignalRService } from '../../core/services/signalr.service';
import { ChatWidget } from '../../features/chatbot/chat-widget/chat-widget';

@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive,
    MatToolbarModule, MatSidenavModule, MatListModule,
    MatIconModule, MatButtonModule, MatMenuModule,
    ChatWidget, TranslatePipe
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell implements OnInit, OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly signalR = inject(SignalRService);
  public translation = inject(TranslationService);
  public themeService = inject(ThemeService);
  private readonly breakpointObserver = inject(BreakpointObserver);

  readonly user = this.auth.user;
  
  isMobile = false;

  isDarkMode() {
    const mode = this.themeService.currentTheme();
    if (mode === ThemeMode.Auto) {
      return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    }
    return mode === ThemeMode.Dark;
  }

  toggleTheme() {
    if (this.isDarkMode()) {
      this.themeService.setTheme(ThemeMode.Light);
    } else {
      this.themeService.setTheme(ThemeMode.Dark);
    }
  }

  ngOnInit(): void {
    // Responsive layout detection
    this.breakpointObserver.observe([Breakpoints.Handset]).subscribe(result => {
      this.isMobile = result.matches;
    });

    // Open the real-time channel for the authenticated session (automation notifications).
    this.signalR.connect();
  }

  ngOnDestroy(): void {
    this.signalR.disconnect();
  }

  logout(): void {
    this.signalR.disconnect();
    this.auth.logout();
  }
}
