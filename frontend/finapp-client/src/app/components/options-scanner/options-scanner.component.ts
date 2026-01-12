import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

// Angular Material
import { MatCardModule } from '@angular/material/card';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatSortModule, MatSort, Sort } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatBadgeModule } from '@angular/material/badge';

// Services & Models
import { OptionsActivityService } from '../../services/options-activity.service';
import {
  UnusualOptionsActivity,
  OptionsActivityResponse,
  OptionsActivityFilter,
  OptionsActivityStats,
  DirectionalBias,
  MarketCapCategory
} from '../../models/options-activity.model';

// Details Dialog
import { OptionsDetailsDialogComponent } from './options-details-dialog.component';

@Component({
  selector: 'app-options-scanner',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatSelectModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatDialogModule,
    MatSlideToggleModule,
    MatBadgeModule
  ],
  templateUrl: './options-scanner.component.html',
  styleUrl: './options-scanner.component.scss'
})
export class OptionsScannerComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  // Table data
  dataSource = new MatTableDataSource<UnusualOptionsActivity>([]);
  displayedColumns = [
    'symbol', 'lastPrice', 'priceChangePercent', 'totalOptionsVolume',
    'volumeChangePercent', 'callVolume', 'putVolume', 'putCallRatio',
    'impliedVolatility', 'bias', 'actions'
  ];

  // Pagination
  totalCount = 0;
  pageSize = 25;
  pageIndex = 0;

  // Filters
  biasFilter: DirectionalBias | '' = '';
  capFilter: MarketCapCategory | '' = '';
  highIVOnly = false;
  minVolume = 1000;
  sortBy = 'volumeChangePercent';
  sortDesc = true;

  // Stats
  stats: OptionsActivityStats | null = null;

  // Loading state
  loading = false;
  lastUpdated: Date | null = null;

  // Auto refresh
  autoRefresh = false;
  refreshInterval: any;

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  constructor(
    private optionsService: OptionsActivityService,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }

  loadData(): void {
    this.loading = true;

    const filter: OptionsActivityFilter = {
      bias: this.biasFilter || undefined,
      capCategory: this.capFilter || undefined,
      highIVOnly: this.highIVOnly || undefined,
      minVolume: this.minVolume,
      sortBy: this.sortBy,
      sortDesc: this.sortDesc,
      page: this.pageIndex + 1,
      pageSize: this.pageSize
    };

    this.optionsService.getUnusualActivity(filter)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: OptionsActivityResponse) => {
          this.dataSource.data = response.data;
          this.totalCount = response.totalCount;
          this.stats = response.stats;
          this.lastUpdated = new Date(response.lastUpdated);
          this.loading = false;
        },
        error: (error) => {
          console.error('Error loading options data:', error);
          this.loading = false;
        }
      });
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadData();
  }

  onSortChange(sort: Sort): void {
    this.sortBy = sort.active;
    this.sortDesc = sort.direction === 'desc';
    this.loadData();
  }

  onFilterChange(): void {
    this.pageIndex = 0;
    this.loadData();
  }

  clearFilters(): void {
    this.biasFilter = '';
    this.capFilter = '';
    this.highIVOnly = false;
    this.minVolume = 1000;
    this.pageIndex = 0;
    this.loadData();
  }

  toggleAutoRefresh(): void {
    this.autoRefresh = !this.autoRefresh;
    if (this.autoRefresh) {
      this.refreshInterval = setInterval(() => this.loadData(), 60000); // Refresh every minute
    } else if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }

  openDetails(activity: UnusualOptionsActivity): void {
    this.dialog.open(OptionsDetailsDialogComponent, {
      width: '900px',
      maxHeight: '90vh',
      data: { symbol: activity.symbol }
    });
  }

  // Formatting helpers
  formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2
    }).format(value);
  }

  formatNumber(value: number): string {
    if (value >= 1000000) {
      return (value / 1000000).toFixed(1) + 'M';
    }
    if (value >= 1000) {
      return (value / 1000).toFixed(1) + 'K';
    }
    return value.toLocaleString();
  }

  formatPercent(value: number, decimals: number = 1): string {
    return value.toFixed(decimals) + '%';
  }

  formatRatio(value: number): string {
    return value.toFixed(2);
  }

  // Styling helpers
  getBiasClass(bias: DirectionalBias): string {
    switch (bias) {
      case 'Bullish': return 'bullish';
      case 'Bearish': return 'bearish';
      default: return 'neutral';
    }
  }

  getBiasIcon(bias: DirectionalBias): string {
    switch (bias) {
      case 'Bullish': return 'trending_up';
      case 'Bearish': return 'trending_down';
      default: return 'trending_flat';
    }
  }

  getPriceChangeClass(change: number): string {
    if (change > 0) return 'positive';
    if (change < 0) return 'negative';
    return '';
  }

  getVolumeChangeClass(percent: number): string {
    if (percent >= 500) return 'extreme';
    if (percent >= 300) return 'high';
    if (percent >= 150) return 'moderate';
    return '';
  }

  getIVClass(iv: number): string {
    if (iv >= 80) return 'iv-extreme';
    if (iv >= 50) return 'iv-high';
    return '';
  }

  getRowClass(activity: UnusualOptionsActivity): string {
    const classes = [];
    if (activity.bias === 'Bullish') classes.push('row-bullish');
    if (activity.bias === 'Bearish') classes.push('row-bearish');
    if (activity.volumeChangePercent >= 500) classes.push('row-extreme');
    return classes.join(' ');
  }
}
