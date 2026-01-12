import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterOutlet } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    RouterOutlet,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatTabsModule
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  navLinks = [
    { path: '/dashboard', label: 'Market Dashboard', icon: 'dashboard' },
    { path: '/performance', label: 'Stock Performance', icon: 'trending_up' },
    { path: '/portfolio', label: 'Portfolio', icon: 'account_balance_wallet' },
    { path: '/options-scanner', label: 'Options Scanner', icon: 'radar' }
  ];
}
