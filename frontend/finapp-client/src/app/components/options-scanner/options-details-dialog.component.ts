import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';

import { OptionsActivityService } from '../../services/options-activity.service';
import {
  OptionsActivityDetails,
  TopOptionContract,
  DirectionalBias
} from '../../models/options-activity.model';

@Component({
  selector: 'app-options-details-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    MatCardModule,
    MatTooltipModule
  ],
  template: `
    <div class="details-dialog">
      <div mat-dialog-title class="dialog-header">
        <div class="title-content" *ngIf="details">
          <div class="symbol-info">
            <span class="symbol">{{ details.symbol }}</span>
            <mat-chip [ngClass]="getBiasClass(details.summary.bias)">
              <mat-icon>{{ getBiasIcon(details.summary.bias) }}</mat-icon>
              {{ details.summary.biasLabel }}
            </mat-chip>
          </div>
          <span class="company-name">{{ details.companyName }}</span>
        </div>
        <button mat-icon-button (click)="close()">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <mat-dialog-content>
        <div *ngIf="loading" class="loading">
          <mat-spinner diameter="40"></mat-spinner>
          <span>Loading details...</span>
        </div>

        <div *ngIf="!loading && details" class="content">
          <!-- Summary Stats -->
          <div class="summary-grid">
            <div class="summary-item">
              <span class="label">Stock Price</span>
              <span class="value">{{ formatCurrency(details.summary.lastPrice) }}</span>
              <span class="sub" [ngClass]="details.summary.priceChangePercent >= 0 ? 'positive' : 'negative'">
                {{ details.summary.priceChangePercent >= 0 ? '+' : '' }}{{ formatPercent(details.summary.priceChangePercent) }}
              </span>
            </div>
            <div class="summary-item highlight">
              <span class="label">Volume vs Avg</span>
              <span class="value large">{{ formatPercent(details.summary.volumeChangePercent, 0) }}</span>
              <span class="sub">of 30-day avg</span>
            </div>
            <div class="summary-item">
              <span class="label">Options Volume</span>
              <span class="value">{{ formatNumber(details.summary.totalOptionsVolume) }}</span>
              <span class="sub">Avg: {{ formatNumber(details.summary.avgOptionsVolume30Day) }}</span>
            </div>
            <div class="summary-item">
              <span class="label">Put/Call Ratio</span>
              <span class="value">{{ details.summary.putCallRatio.toFixed(2) }}</span>
            </div>
            <div class="summary-item">
              <span class="label">Implied Volatility</span>
              <span class="value" [ngClass]="getIVClass(details.summary.impliedVolatility)">
                {{ formatPercent(details.summary.impliedVolatility, 0) }}
              </span>
            </div>
            <div class="summary-item">
              <span class="label">Open Interest</span>
              <span class="value">{{ formatNumber(details.summary.openInterest) }}</span>
            </div>
          </div>

          <!-- AI Explanation -->
          <mat-card class="explanation-card">
            <mat-card-header>
              <mat-icon mat-card-avatar>psychology</mat-icon>
              <mat-card-title>AI Analysis</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <div class="explanation-text" [innerHTML]="formatExplanation(details.aiExplanation)"></div>
            </mat-card-content>
          </mat-card>

          <!-- Potential Catalysts -->
          <div class="catalysts" *ngIf="details.potentialCatalysts.length > 0">
            <h4>Potential Catalysts</h4>
            <div class="catalyst-chips">
              <mat-chip *ngFor="let catalyst of details.potentialCatalysts">
                <mat-icon>event</mat-icon>
                {{ catalyst }}
              </mat-chip>
            </div>
          </div>

          <mat-divider></mat-divider>

          <!-- Top Contracts Table -->
          <h4>Most Active Contracts</h4>
          <div class="contracts-table">
            <table mat-table [dataSource]="details.topContracts">
              <ng-container matColumnDef="optionType">
                <th mat-header-cell *matHeaderCellDef>Type</th>
                <td mat-cell *matCellDef="let row">
                  <mat-chip [ngClass]="row.optionType === 'Call' ? 'call-chip' : 'put-chip'">
                    {{ row.optionType }}
                  </mat-chip>
                </td>
              </ng-container>

              <ng-container matColumnDef="strikePrice">
                <th mat-header-cell *matHeaderCellDef>Strike</th>
                <td mat-cell *matCellDef="let row">{{ formatCurrency(row.strikePrice) }}</td>
              </ng-container>

              <ng-container matColumnDef="expirationDate">
                <th mat-header-cell *matHeaderCellDef>Expiry</th>
                <td mat-cell *matCellDef="let row">{{ formatDate(row.expirationDate) }}</td>
              </ng-container>

              <ng-container matColumnDef="volume">
                <th mat-header-cell *matHeaderCellDef>Volume</th>
                <td mat-cell *matCellDef="let row">
                  <span [class.unusual]="row.isUnusual">{{ formatNumber(row.volume) }}</span>
                </td>
              </ng-container>

              <ng-container matColumnDef="openInterest">
                <th mat-header-cell *matHeaderCellDef>OI</th>
                <td mat-cell *matCellDef="let row">{{ formatNumber(row.openInterest) }}</td>
              </ng-container>

              <ng-container matColumnDef="lastPrice">
                <th mat-header-cell *matHeaderCellDef>Price</th>
                <td mat-cell *matCellDef="let row">{{ formatCurrency(row.lastPrice) }}</td>
              </ng-container>

              <ng-container matColumnDef="impliedVolatility">
                <th mat-header-cell *matHeaderCellDef>IV</th>
                <td mat-cell *matCellDef="let row">{{ formatPercent(row.impliedVolatility, 0) }}</td>
              </ng-container>

              <ng-container matColumnDef="activityType">
                <th mat-header-cell *matHeaderCellDef>Activity</th>
                <td mat-cell *matCellDef="let row">
                  <span class="activity-badge" [ngClass]="row.activityType.toLowerCase()">
                    {{ row.activityType }}
                  </span>
                </td>
              </ng-container>

              <tr mat-header-row *matHeaderRowDef="contractColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: contractColumns" [class.unusual-row]="row.isUnusual"></tr>
            </table>
          </div>

          <div class="generated-info">
            Generated at {{ details.generatedAt | date:'medium' }}
          </div>
        </div>

        <div *ngIf="!loading && error" class="error">
          <mat-icon>error</mat-icon>
          <span>{{ error }}</span>
        </div>
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button mat-button (click)="close()">Close</button>
      </mat-dialog-actions>
    </div>
  `,
  styles: [`
    .details-dialog {
      min-width: 300px;
    }

    .dialog-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      padding-bottom: 16px;
      border-bottom: 1px solid #eee;
    }

    .title-content {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .symbol-info {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .symbol {
      font-size: 24px;
      font-weight: 600;
      color: #1976d2;
    }

    .company-name {
      font-size: 14px;
      color: #666;
    }

    .loading, .error {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 16px;
      padding: 40px;
      color: #666;
    }

    .error {
      color: #f44336;
    }

    .content {
      padding-top: 16px;
    }

    .summary-grid {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 16px;
      margin-bottom: 24px;
    }

    @media (max-width: 600px) {
      .summary-grid {
        grid-template-columns: repeat(2, 1fr);
      }
    }

    .summary-item {
      background: #f9f9f9;
      padding: 16px;
      border-radius: 8px;
      text-align: center;

      &.highlight {
        background: linear-gradient(135deg, #e3f2fd 0%, #bbdefb 100%);
        border: 1px solid #1976d2;
      }

      .label {
        display: block;
        font-size: 11px;
        color: #666;
        text-transform: uppercase;
        margin-bottom: 4px;
      }

      .value {
        display: block;
        font-size: 20px;
        font-weight: 600;
        color: #333;

        &.large {
          font-size: 28px;
          color: #1976d2;
        }
      }

      .sub {
        display: block;
        font-size: 11px;
        color: #999;
        margin-top: 2px;
      }
    }

    .positive { color: #4caf50 !important; }
    .negative { color: #f44336 !important; }

    .iv-high { color: #ff9800; }
    .iv-extreme { color: #f44336; }

    .explanation-card {
      margin-bottom: 24px;

      mat-card-header {
        mat-icon {
          color: #1976d2;
        }
      }

      .explanation-text {
        font-size: 14px;
        line-height: 1.6;
        color: #333;

        :deep(strong) {
          color: #1976d2;
        }

        :deep(p) {
          margin: 0 0 12px 0;
        }
      }
    }

    .catalysts {
      margin-bottom: 24px;

      h4 {
        margin: 0 0 12px 0;
        font-size: 14px;
        color: #333;
      }

      .catalyst-chips {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;

        mat-chip {
          font-size: 12px;

          mat-icon {
            font-size: 14px;
            width: 14px;
            height: 14px;
            margin-right: 4px;
          }
        }
      }
    }

    mat-divider {
      margin: 24px 0;
    }

    h4 {
      margin: 0 0 16px 0;
      font-size: 14px;
      font-weight: 500;
      color: #333;
    }

    .contracts-table {
      overflow-x: auto;
      margin-bottom: 16px;

      table {
        width: 100%;

        th {
          font-size: 11px;
          text-transform: uppercase;
          background: #f5f5f5;
        }

        td, th {
          padding: 10px 12px;
        }
      }

      .unusual {
        font-weight: 600;
        color: #1976d2;
      }

      .unusual-row {
        background: rgba(25, 118, 210, 0.05);
      }
    }

    .call-chip {
      background: rgba(76, 175, 80, 0.15) !important;
      color: #2e7d32 !important;
    }

    .put-chip {
      background: rgba(244, 67, 54, 0.15) !important;
      color: #c62828 !important;
    }

    .activity-badge {
      font-size: 11px;
      padding: 2px 8px;
      border-radius: 4px;

      &.opening {
        background: rgba(76, 175, 80, 0.1);
        color: #2e7d32;
      }

      &.closing {
        background: rgba(244, 67, 54, 0.1);
        color: #c62828;
      }

      &.unknown {
        background: #f5f5f5;
        color: #666;
      }
    }

    mat-chip.bullish {
      background-color: rgba(76, 175, 80, 0.15) !important;
      color: #2e7d32 !important;
    }

    mat-chip.bearish {
      background-color: rgba(244, 67, 54, 0.15) !important;
      color: #c62828 !important;
    }

    mat-chip.neutral {
      background-color: rgba(158, 158, 158, 0.15) !important;
      color: #616161 !important;
    }

    .generated-info {
      font-size: 11px;
      color: #999;
      text-align: right;
      margin-top: 16px;
    }
  `]
})
export class OptionsDetailsDialogComponent implements OnInit {
  details: OptionsActivityDetails | null = null;
  loading = true;
  error = '';

  contractColumns = [
    'optionType', 'strikePrice', 'expirationDate', 'volume',
    'openInterest', 'lastPrice', 'impliedVolatility', 'activityType'
  ];

  constructor(
    private dialogRef: MatDialogRef<OptionsDetailsDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { symbol: string },
    private optionsService: OptionsActivityService
  ) {}

  ngOnInit(): void {
    this.loadDetails();
  }

  loadDetails(): void {
    this.loading = true;
    this.error = '';

    this.optionsService.getActivityDetails(this.data.symbol).subscribe({
      next: (details) => {
        this.details = details;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load options details';
        this.loading = false;
        console.error('Error loading details:', err);
      }
    });
  }

  close(): void {
    this.dialogRef.close();
  }

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

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric'
    });
  }

  formatExplanation(text: string): string {
    // Convert markdown-like formatting to HTML
    return text
      .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
      .replace(/\n\n/g, '</p><p>')
      .replace(/\n- /g, '</p><p>• ')
      .replace(/^/, '<p>')
      .replace(/$/, '</p>');
  }

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

  getIVClass(iv: number): string {
    if (iv >= 80) return 'iv-extreme';
    if (iv >= 50) return 'iv-high';
    return '';
  }
}
