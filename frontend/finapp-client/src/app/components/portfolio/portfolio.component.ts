import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, debounceTime, distinctUntilChanged } from 'rxjs/operators';

// Angular Material Imports
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatBadgeModule } from '@angular/material/badge';

// Services & Models
import { PortfolioService } from '../../services/portfolio.service';
import { RealtimeService, PriceUpdate } from '../../services/realtime.service';
import {
  AddTradeRequest,
  TradeRiskAnalysis,
  PortfolioSummary,
  PositionSizeResponse
} from '../../models/portfolio.model';

@Component({
  selector: 'app-portfolio',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatInputModule,
    MatFormFieldModule,
    MatTableModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatTooltipModule,
    MatSnackBarModule,
    MatDividerModule,
    MatChipsModule,
    MatBadgeModule
  ],
  templateUrl: './portfolio.component.html',
  styleUrl: './portfolio.component.scss'
})
export class PortfolioComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private tickerInput$ = new Subject<string>();

  // Portfolio Settings (user configurable)
  portfolioValue: number = 100000;

  // Form inputs
  ticker: string = '';
  shares: number = 0;
  stopLossPrice: number = 0;
  targetPrice: number = 0;
  notes: string = '';

  // Fetched data
  currentPrice: number | null = null;
  companyName: string = '';
  exchange: string = '';
  positionSizeCalc: PositionSizeResponse | null = null;

  // Loading states
  loadingPrice = false;
  loadingTrades = false;
  addingTrade = false;

  // Portfolio data
  portfolio: PortfolioSummary | null = null;

  // Real-time connection status
  connectionStatus: string = 'Disconnected';
  isRealtime: boolean = false;

  // Table columns
  tradeColumns = [
    'ticker', 'shares', 'entryPrice', 'currentPrice',
    'stopLoss', 'target', 'pnl', 'riskReward', 'status', 'actions'
  ];

  constructor(
    private portfolioService: PortfolioService,
    private realtimeService: RealtimeService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadTrades();
    this.setupRealtimeUpdates();

    // Debounce ticker input to fetch price (200ms for faster response)
    this.tickerInput$.pipe(
      debounceTime(200),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(ticker => {
      if (ticker && ticker.length >= 1) {
        this.fetchCurrentPrice(ticker);
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private setupRealtimeUpdates(): void {
    // Subscribe to connection state
    this.realtimeService.getConnectionState()
      .pipe(takeUntil(this.destroy$))
      .subscribe(state => {
        this.connectionStatus = state;
        this.isRealtime = state === 'Connected';
      });

    // Subscribe to price updates
    this.realtimeService.getPriceUpdates()
      .pipe(takeUntil(this.destroy$))
      .subscribe(update => {
        this.handlePriceUpdate(update);
      });
  }

  private handlePriceUpdate(update: PriceUpdate): void {
    // Update current price if it's the ticker being entered
    if (this.ticker && update.symbol === this.ticker.toUpperCase()) {
      this.currentPrice = update.price;
      this.calculatePositionSize();
    }

    // Update trade prices in portfolio
    if (this.portfolio?.trades) {
      const trade = this.portfolio.trades.find(t => t.ticker === update.symbol);
      if (trade) {
        const oldPrice = trade.currentPrice;
        trade.currentPrice = update.price;
        trade.currentValue = trade.shares * update.price;
        trade.unrealizedPnL = (update.price - trade.entryPrice) * trade.shares;
        trade.unrealizedPnLPercent = ((update.price - trade.entryPrice) / trade.entryPrice) * 100;

        // Update status color
        if (update.price <= trade.stopLossPrice) {
          trade.statusColor = 'red';
          trade.status = 'Stop Loss Hit';
        } else if (update.price >= trade.targetPrice) {
          trade.statusColor = 'green';
          trade.status = 'Target Reached';
        } else if (trade.unrealizedPnL < 0) {
          trade.statusColor = 'red';
          trade.status = 'In Loss';
        } else if (trade.unrealizedPnL > 0) {
          trade.statusColor = 'green';
          trade.status = 'In Profit';
        } else {
          trade.statusColor = 'neutral';
          trade.status = 'Open';
        }

        // Update portfolio totals
        this.updatePortfolioTotals();
      }
    }
  }

  private updatePortfolioTotals(): void {
    if (!this.portfolio?.trades) return;

    let totalPnL = 0;
    let tradesInProfit = 0;
    let tradesInLoss = 0;
    let totalRisk = 0;

    for (const trade of this.portfolio.trades) {
      totalPnL += trade.unrealizedPnL;
      totalRisk += trade.totalRiskAmount;
      if (trade.unrealizedPnL > 0) tradesInProfit++;
      else if (trade.unrealizedPnL < 0) tradesInLoss++;
    }

    this.portfolio.totalUnrealizedPnL = totalPnL;
    this.portfolio.tradesInProfit = tradesInProfit;
    this.portfolio.tradesInLoss = tradesInLoss;
    this.portfolio.currentTotalRisk = totalRisk;
    this.portfolio.currentTotalRiskPercent = (totalRisk / this.portfolio.totalPortfolioValue) * 100;
  }

  onTickerChange(): void {
    this.tickerInput$.next(this.ticker.toUpperCase());
    this.currentPrice = null;
    this.companyName = '';
    this.exchange = '';
    this.positionSizeCalc = null;
  }

  fetchCurrentPrice(ticker: string): void {
    this.loadingPrice = true;

    // Subscribe to real-time updates for this ticker
    this.realtimeService.subscribe(ticker);

    // Fetch from REST API (includes company name)
    this.portfolioService.getCurrentPrice(ticker)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.currentPrice = response.price;
          this.companyName = response.companyName || ticker;
          this.exchange = response.exchange || '';
          this.loadingPrice = false;
          this.calculatePositionSize();
        },
        error: (error) => {
          this.loadingPrice = false;
          // Check if we have real-time price
          const realtimePrice = this.realtimeService.getLatestPrice(ticker);
          if (realtimePrice) {
            this.currentPrice = realtimePrice;
            this.calculatePositionSize();
          } else {
            this.showError(`Failed to fetch price for ${ticker}`);
          }
        }
      });
  }

  calculatePositionSize(): void {
    if (!this.ticker || !this.stopLossPrice || !this.currentPrice) {
      return;
    }

    if (this.stopLossPrice >= this.currentPrice) {
      this.positionSizeCalc = null;
      return;
    }

    this.portfolioService.calculatePositionSize({
      ticker: this.ticker.toUpperCase(),
      stopLossPrice: this.stopLossPrice,
      portfolioValue: this.portfolioValue
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.positionSizeCalc = response;
          if (this.shares === 0) {
            this.shares = response.recommendedShares;
          }
        },
        error: () => {
          this.positionSizeCalc = null;
        }
      });
  }

  onStopLossChange(): void {
    this.calculatePositionSize();
  }

  addTrade(): void {
    if (!this.ticker || !this.shares || !this.stopLossPrice || !this.targetPrice) {
      this.showError('Please fill in all required fields');
      return;
    }

    this.addingTrade = true;
    const trade: AddTradeRequest = {
      ticker: this.ticker.toUpperCase(),
      shares: this.shares,
      stopLossPrice: this.stopLossPrice,
      targetPrice: this.targetPrice,
      notes: this.notes || undefined
    };

    this.portfolioService.addTrade(trade)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.addingTrade = false;
          this.showSuccess(`Added ${trade.ticker} to portfolio`);

          // Subscribe to real-time updates for this trade
          this.realtimeService.subscribe(trade.ticker);

          this.resetForm();
          this.loadTrades();
        },
        error: (error) => {
          this.addingTrade = false;
          this.showError(`Failed to add trade: ${error.error?.error || error.message}`);
        }
      });
  }

  loadTrades(): void {
    this.loadingTrades = true;
    this.portfolioService.getTrades()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.portfolio = response;
          this.loadingTrades = false;

          // Subscribe to real-time updates for all trades
          if (response.trades && response.trades.length > 0) {
            const symbols = response.trades.map(t => t.ticker);
            this.realtimeService.subscribeMultiple(symbols);
          }
        },
        error: (error) => {
          this.loadingTrades = false;
          this.showError('Failed to load trades');
        }
      });
  }

  deleteTrade(trade: TradeRiskAnalysis): void {
    if (confirm(`Delete trade for ${trade.ticker}?`)) {
      this.portfolioService.deleteTrade(trade.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.showSuccess(`Deleted ${trade.ticker} trade`);
            this.realtimeService.unsubscribe(trade.ticker);
            this.loadTrades();
          },
          error: () => {
            this.showError('Failed to delete trade');
          }
        });
    }
  }

  closeTrade(trade: TradeRiskAnalysis): void {
    const closePrice = prompt(`Enter closing price for ${trade.ticker}:`, trade.currentPrice.toString());
    if (closePrice) {
      this.portfolioService.closeTrade(trade.id, parseFloat(closePrice))
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.showSuccess(`Closed ${trade.ticker} trade`);
            this.loadTrades();
          },
          error: () => {
            this.showError('Failed to close trade');
          }
        });
    }
  }

  resetForm(): void {
    this.ticker = '';
    this.shares = 0;
    this.stopLossPrice = 0;
    this.targetPrice = 0;
    this.notes = '';
    this.currentPrice = null;
    this.companyName = '';
    this.exchange = '';
    this.positionSizeCalc = null;
  }

  // Helper methods
  formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(value);
  }

  formatPercent(value: number): string {
    return value.toFixed(2) + '%';
  }

  getStatusClass(trade: TradeRiskAnalysis): string {
    return trade.statusColor;
  }

  getRiskClass(riskPercent: number): string {
    if (riskPercent > 3) return 'risk-high';
    if (riskPercent > 2) return 'risk-medium';
    return 'risk-low';
  }

  getConnectionStatusClass(): string {
    switch (this.connectionStatus) {
      case 'Connected': return 'connected';
      case 'Reconnecting': return 'reconnecting';
      default: return 'disconnected';
    }
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
