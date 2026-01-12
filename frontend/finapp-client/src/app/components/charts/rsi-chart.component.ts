import {
  Component,
  Input,
  OnChanges,
  OnDestroy,
  ElementRef,
  ViewChild,
  AfterViewInit,
  SimpleChanges
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  createChart,
  IChartApi,
  ISeriesApi,
  LineData,
  Time,
  ColorType,
  LineSeries
} from 'lightweight-charts';
import { IndicatorValue } from '../../models/market-data.model';

@Component({
  selector: 'app-rsi-chart',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="chart-wrapper">
      <div class="chart-header">
        <h3>{{ title }}</h3>
        <div class="rsi-value" *ngIf="currentRsi !== null" [ngClass]="getRsiClass()">
          RSI: {{ currentRsi.toFixed(2) }}
          <span class="rsi-label">{{ getRsiLabel() }}</span>
        </div>
      </div>
      <div #chartContainer class="chart-container"></div>
      <div class="chart-legend">
        <span class="legend-item overbought">Overbought (>70)</span>
        <span class="legend-item oversold">Oversold (<30)</span>
      </div>
    </div>
  `,
  styles: [`
    .chart-wrapper {
      background: white;
      border-radius: 8px;
      padding: 16px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }
    .chart-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 12px;
    }
    .chart-header h3 {
      margin: 0;
      font-size: 16px;
      font-weight: 500;
      color: #333;
    }
    .rsi-value {
      font-size: 14px;
      font-weight: 600;
      padding: 4px 12px;
      border-radius: 4px;
    }
    .rsi-value.overbought {
      background: #ffebee;
      color: #c62828;
    }
    .rsi-value.oversold {
      background: #e8f5e9;
      color: #2e7d32;
    }
    .rsi-value.neutral {
      background: #f5f5f5;
      color: #666;
    }
    .rsi-label {
      font-size: 11px;
      font-weight: 400;
      margin-left: 4px;
    }
    .chart-container {
      width: 100%;
      height: 200px;
    }
    .chart-legend {
      display: flex;
      gap: 16px;
      margin-top: 12px;
      padding-top: 12px;
      border-top: 1px solid #eee;
    }
    .legend-item {
      display: flex;
      align-items: center;
      font-size: 12px;
      color: #666;
    }
    .legend-item::before {
      content: '';
      display: inline-block;
      width: 20px;
      height: 2px;
      margin-right: 6px;
    }
    .legend-item.overbought::before { background: #ef5350; }
    .legend-item.oversold::before { background: #26a69a; }
  `]
})
export class RsiChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('chartContainer') chartContainer!: ElementRef;

  @Input() title: string = 'RSI (14)';
  @Input() rsiData: IndicatorValue[] = [];
  @Input() height: number = 200;
  @Input() overboughtLevel: number = 70;
  @Input() oversoldLevel: number = 30;

  currentRsi: number | null = null;

  private chart: IChartApi | null = null;
  private rsiSeries: ISeriesApi<'Line'> | null = null;

  ngAfterViewInit(): void {
    this.initChart();
    this.updateChart();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (this.chart && changes['rsiData']) {
      this.updateChart();
    }
  }

  ngOnDestroy(): void {
    if (this.chart) {
      this.chart.remove();
      this.chart = null;
    }
  }

  getRsiClass(): string {
    if (this.currentRsi === null) return 'neutral';
    if (this.currentRsi >= this.overboughtLevel) return 'overbought';
    if (this.currentRsi <= this.oversoldLevel) return 'oversold';
    return 'neutral';
  }

  getRsiLabel(): string {
    if (this.currentRsi === null) return '';
    if (this.currentRsi >= this.overboughtLevel) return '(Overbought)';
    if (this.currentRsi <= this.oversoldLevel) return '(Oversold)';
    return '(Neutral)';
  }

  private initChart(): void {
    if (!this.chartContainer?.nativeElement) return;

    this.chart = createChart(this.chartContainer.nativeElement, {
      width: this.chartContainer.nativeElement.clientWidth,
      height: this.height,
      layout: {
        background: { type: ColorType.Solid, color: '#ffffff' },
        textColor: '#333'
      },
      grid: {
        vertLines: { color: '#f0f0f0' },
        horzLines: { color: '#f0f0f0' }
      },
      rightPriceScale: {
        borderColor: '#ddd',
        scaleMargins: {
          top: 0.1,
          bottom: 0.1
        }
      },
      timeScale: {
        borderColor: '#ddd',
        timeVisible: true
      }
    });

    // RSI line series
    this.rsiSeries = this.chart.addSeries(LineSeries, {
      color: '#7c4dff',
      lineWidth: 2,
      priceLineVisible: false
    });

    // Add overbought line
    if (this.rsiSeries) {
      this.rsiSeries.createPriceLine({
        price: this.overboughtLevel,
        color: '#ef5350',
        lineWidth: 1,
        lineStyle: 2,
        axisLabelVisible: true,
        title: 'OB'
      });

      // Add oversold line
      this.rsiSeries.createPriceLine({
        price: this.oversoldLevel,
        color: '#26a69a',
        lineWidth: 1,
        lineStyle: 2,
        axisLabelVisible: true,
        title: 'OS'
      });

      // Add middle line (50)
      this.rsiSeries.createPriceLine({
        price: 50,
        color: '#999',
        lineWidth: 1,
        lineStyle: 2,
        axisLabelVisible: false
      });
    }

    // Handle resize
    const resizeObserver = new ResizeObserver(entries => {
      if (this.chart && entries[0]) {
        this.chart.applyOptions({
          width: entries[0].contentRect.width
        });
      }
    });
    resizeObserver.observe(this.chartContainer.nativeElement);
  }

  private updateChart(): void {
    if (!this.chart || !this.rsiSeries) return;

    if (this.rsiData.length > 0) {
      const lineData: LineData[] = this.rsiData
        .map(item => ({
          time: (item.timestamp / 1000) as Time,
          value: item.value
        }))
        .sort((a, b) => (a.time as number) - (b.time as number));

      this.rsiSeries.setData(lineData);

      // Get most recent RSI value
      const sortedData = [...this.rsiData].sort((a, b) => b.timestamp - a.timestamp);
      this.currentRsi = sortedData[0]?.value ?? null;
    } else {
      this.rsiSeries.setData([]);
      this.currentRsi = null;
    }

    this.chart.timeScale().fitContent();
  }
}
