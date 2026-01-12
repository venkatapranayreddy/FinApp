import { Routes } from '@angular/router';
import { MarketDashboardComponent } from './components/market-dashboard/market-dashboard.component';
import { StockPerformanceComponent } from './components/stock-performance/stock-performance.component';
import { PortfolioComponent } from './components/portfolio/portfolio.component';
import { OptionsScannerComponent } from './components/options-scanner/options-scanner.component';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: MarketDashboardComponent },
  { path: 'performance', component: StockPerformanceComponent },
  { path: 'portfolio', component: PortfolioComponent },
  { path: 'options-scanner', component: OptionsScannerComponent }
];
