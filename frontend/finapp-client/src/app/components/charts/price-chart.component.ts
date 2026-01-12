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
  CandlestickData,
  LineData,
  Time,
  ColorType,
  CandlestickSeries,
  LineSeries
} from 'lightweight-charts';
import { AggregateBar, IndicatorValue } from '../../models/market-data.model';

@Component({
  selector: 'app-price-chart',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="chart-wrapper">
      <div class="chart-header">
        <h3>{{ title }}</h3>
        <span class="chart-subtitle" *ngIf="subtitle">{{ subtitle }}</span>
      </div>
      <div #chartContainer class="chart-container"></div>
      <div class="chart-legend" *ngIf="showLegend">
        <span class="legend-item candle">OHLC</span>
        <span class="legend-item sma" *ngIf="smaData.length">SMA ({{ smaWindow }})</span>
        <span class="legend-item ema" *ngIf="emaData.length">EMA ({{ emaWindow }})</span>
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
    .chart-subtitle {
      font-size: 12px;
      color: #666;
    }
    .chart-container {
      width: 100%;
      height: 400px;
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
      width: 12px;
      height: 12px;
      margin-right: 6px;
      border-radius: 2px;
    }
    .legend-item.candle::before { background: #26a69a; }
    .legend-item.sma::before { background: #2196f3; }
    .legend-item.ema::before { background: #ff9800; }
  `]
})
export class PriceChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('chartContainer') chartContainer!: ElementRef;

  @Input() title: string = 'Price Chart';
  @Input() subtitle: string = '';
  @Input() priceData: AggregateBar[] = [];
  @Input() smaData: IndicatorValue[] = [];
  @Input() emaData: IndicatorValue[] = [];
  @Input() smaWindow: number = 20;
  @Input() emaWindow: number = 20;
  @Input() showLegend: boolean = true;
  @Input() height: number = 400;

  private chart: IChartApi | null = null;
  private candleSeries: ISeriesApi<'Candlestick'> | null = null;
  private smaSeries: ISeriesApi<'Line'> | null = null;
  private emaSeries: ISeriesApi<'Line'> | null = null;

  ngAfterViewInit(): void {
    this.initChart();
    this.updateChart();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (this.chart && (changes['priceData'] || changes['smaData'] || changes['emaData'])) {
      this.updateChart();
    }
  }

  ngOnDestroy(): void {
    if (this.chart) {
      this.chart.remove();
      this.chart = null;
    }
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
      crosshair: {
        mode: 1
      },
      rightPriceScale: {
        borderColor: '#ddd'
      },
      timeScale: {
        borderColor: '#ddd',
        timeVisible: true,
        secondsVisible: false
      }
    });

    // Candlestick series
    this.candleSeries = this.chart.addSeries(CandlestickSeries, {
      upColor: '#26a69a',
      downColor: '#ef5350',
      borderUpColor: '#26a69a',
      borderDownColor: '#ef5350',
      wickUpColor: '#26a69a',
      wickDownColor: '#ef5350'
    });

    // SMA line series
    this.smaSeries = this.chart.addSeries(LineSeries, {
      color: '#2196f3',
      lineWidth: 2,
      crosshairMarkerVisible: true,
      priceLineVisible: false
    });

    // EMA line series
    this.emaSeries = this.chart.addSeries(LineSeries, {
      color: '#ff9800',
      lineWidth: 2,
      crosshairMarkerVisible: true,
      priceLineVisible: false
    });

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
    if (!this.chart) return;

    // Update candlestick data
    if (this.candleSeries && this.priceData.length > 0) {
      const candleData: CandlestickData[] = this.priceData
        .map(bar => ({
          time: (bar.t / 1000) as Time, // Convert ms to seconds
          open: bar.o,
          high: bar.h,
          low: bar.l,
          close: bar.c
        }))
        .sort((a, b) => (a.time as number) - (b.time as number));

      this.candleSeries.setData(candleData);
    }

    // Update SMA data
    if (this.smaSeries) {
      if (this.smaData.length > 0) {
        const smaLineData: LineData[] = this.smaData
          .map(item => ({
            time: (item.timestamp / 1000) as Time,
            value: item.value
          }))
          .sort((a, b) => (a.time as number) - (b.time as number));

        this.smaSeries.setData(smaLineData);
      } else {
        this.smaSeries.setData([]);
      }
    }

    // Update EMA data
    if (this.emaSeries) {
      if (this.emaData.length > 0) {
        const emaLineData: LineData[] = this.emaData
          .map(item => ({
            time: (item.timestamp / 1000) as Time,
            value: item.value
          }))
          .sort((a, b) => (a.time as number) - (b.time as number));

        this.emaSeries.setData(emaLineData);
      } else {
        this.emaSeries.setData([]);
      }
    }

    // Fit content
    this.chart.timeScale().fitContent();
  }
}
