import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { PageEvent } from '@angular/material/paginator';
import { StockFiltersComponent } from '../stock-filters/stock-filters.component';
import { StockListComponent } from '../stock-list/stock-list.component';
import { StockService } from '../../services/stock.service';
import { FilterRequest, StockPerformance } from '../../models/stock.model';

@Component({
  selector: 'app-stock-performance',
  standalone: true,
  imports: [
    CommonModule,
    MatSnackBarModule,
    StockFiltersComponent,
    StockListComponent
  ],
  templateUrl: './stock-performance.component.html',
  styleUrl: './stock-performance.component.scss'
})
export class StockPerformanceComponent {
  stocks: StockPerformance[] = [];
  totalCount: number = 0;
  pageSize: number = 50;
  currentPage: number = 0;
  loading: boolean = false;
  currentFilter: FilterRequest | null = null;

  constructor(
    private stockService: StockService,
    private snackBar: MatSnackBar
  ) {}

  onFilterChange(filter: FilterRequest): void {
    this.currentFilter = filter;
    this.currentPage = 0;
    this.loadStocks();
  }

  onPageChange(event: PageEvent): void {
    this.currentPage = event.pageIndex;
    this.pageSize = event.pageSize;

    if (this.currentFilter) {
      this.currentFilter.page = event.pageIndex + 1;
      this.currentFilter.pageSize = event.pageSize;
      this.loadStocks();
    }
  }

  onSync(): void {
    this.loading = true;
    this.stockService.syncStocks().subscribe({
      next: (result) => {
        this.loading = false;
        this.snackBar.open(`Synced ${result.syncedCount} stocks successfully!`, 'Close', {
          duration: 3000
        });
      },
      error: (err) => {
        this.loading = false;
        this.snackBar.open('Failed to sync stocks. Please try again.', 'Close', {
          duration: 3000
        });
        console.error('Sync error:', err);
      }
    });
  }

  private loadStocks(): void {
    if (!this.currentFilter) return;

    this.loading = true;
    this.stockService.getStockPerformance(this.currentFilter).subscribe({
      next: (response) => {
        this.stocks = response.data;
        this.totalCount = response.totalCount;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.snackBar.open('Failed to load stock data. Please try again.', 'Close', {
          duration: 3000
        });
        console.error('Load error:', err);
      }
    });
  }
}
