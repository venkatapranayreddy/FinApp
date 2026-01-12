import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, forkJoin } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

// Angular Material Imports
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

// Chart Components
import { PriceChartComponent } from '../charts/price-chart.component';
import { RsiChartComponent } from '../charts/rsi-chart.component';
import { MacdChartComponent } from '../charts/macd-chart.component';

// Services & Models
import { MarketDataService } from '../../services/market-data.service';
import {
  IndicatorValue,
  MacdValue,
  AggregateBar
} from '../../models/market-data.model';

@Component({
  selector: 'app-market-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatTabsModule,
    MatTableModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatIconModule,
    MatTooltipModule,
    MatSnackBarModule,
    PriceChartComponent,
    RsiChartComponent,
    MacdChartComponent
  ],
  templateUrl: './market-dashboard.component.html',
  styleUrl: './market-dashboard.component.scss'
})
export class MarketDashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  // Search & Selection
  selectedTicker: string = 'AAPL';
  searchTicker: string = '';

  // Loading States
  loadingIndicators = false;
  loadingAggregates = false;

  // Data
  smaData: IndicatorValue[] = [];
  emaData: IndicatorValue[] = [];
  rsiData: IndicatorValue[] = [];
  macdData: MacdValue[] = [];
  aggregates: AggregateBar[] = [];

  // Indicator Settings
  smaWindow = 20;
  emaWindow = 20;
  rsiWindow = 14;
  indicatorLimit = 30;

  // Table Columns
  aggregateColumns = ['date', 'open', 'high', 'low', 'close', 'volume'];

  constructor(
    private marketData: MarketDataService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadAllData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  searchStock(): void {
    if (this.searchTicker.trim()) {
      this.selectedTicker = this.searchTicker.toUpperCase().trim();
      this.searchTicker = '';
      this.loadAllData();
    }
  }

  // ============ Indicators Methods ============

  loadIndicators(): void {
    this.loadingIndicators = true;

    forkJoin({
      sma: this.marketData.getSma(this.selectedTicker, this.smaWindow, 'day', this.indicatorLimit),
      ema: this.marketData.getEma(this.selectedTicker, this.emaWindow, 'day', this.indicatorLimit),
      rsi: this.marketData.getRsi(this.selectedTicker, this.rsiWindow, 'day', this.indicatorLimit),
      macd: this.marketData.getMacd(this.selectedTicker, 'day', this.indicatorLimit)
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.smaData = response.sma.data || [];
          this.emaData = response.ema.data || [];
          this.rsiData = response.rsi.data || [];
          this.macdData = response.macd.data || [];
          this.loadingIndicators = false;
          this.showSuccess(`Loaded indicators for ${this.selectedTicker}`);
        },
        error: (error) => {
          this.loadingIndicators = false;
          this.showError(`Failed to load indicators: ${error.message}`);
        }
      });
  }

  // ============ Aggregates Methods ============

  loadAggregates(): void {
    this.loadingAggregates = true;

    // Get last 30 days of data (excluding today to avoid future date issues)
    const to = new Date();
    to.setDate(to.getDate() - 1); // Yesterday
    const from = new Date();
    from.setDate(from.getDate() - 31);

    this.marketData.getAggregates(this.selectedTicker, from, to)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.aggregates = response.data || [];
          this.loadingAggregates = false;
          if (this.aggregates.length > 0) {
            this.showSuccess(`Loaded ${this.aggregates.length} days of price data`);
          }
        },
        error: (error) => {
          this.loadingAggregates = false;
          this.showError(`Failed to load aggregates: ${error.message}`);
        }
      });
  }

  // ============ Load All Data ============

  loadAllData(): void {
    this.loadIndicators();
    this.loadAggregates();
  }

  // ============ Helper Methods ============

  formatDate(timestamp: number): string {
    const ms = timestamp > 1e12 ? timestamp / 1e6 : timestamp;
    return new Date(ms).toLocaleDateString();
  }

  formatPrice(price: number): string {
    return price?.toFixed(2) || '—';
  }

  formatVolume(volume: number): string {
    if (volume >= 1e9) return (volume / 1e9).toFixed(2) + 'B';
    if (volume >= 1e6) return (volume / 1e6).toFixed(2) + 'M';
    if (volume >= 1e3) return (volume / 1e3).toFixed(2) + 'K';
    return volume?.toString() || '—';
  }

  private showSuccess(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      panelClass: ['success-snackbar']
    });
  }

  private showError(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      panelClass: ['error-snackbar']
    });
  }
}
