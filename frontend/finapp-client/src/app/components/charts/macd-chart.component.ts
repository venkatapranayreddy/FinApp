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
  HistogramData,
  Time,
  ColorType,
  LineSeries,
  HistogramSeries
} from 'lightweight-charts';
import { MacdValue } from '../../models/market-data.model';

@Component({
  selector: 'app-macd-chart',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="chart-wrapper">
      <div class="chart-header">
        <h3>{{ title }}</h3>
        <div class="macd-values" *ngIf="currentMacd">
          <span class="macd-item">MACD: <strong>{{ currentMacd.value.toFixed(4) }}</strong></span>
          <span class="macd-item">Signal: <strong>{{ currentMacd.signal.toFixed(4) }}</strong></span>
          <span class="macd-item" [ngClass]="currentMacd.histogram >= 0 ? 'positive' : 'negative'">
            Hist: <strong>{{ currentMacd.histogram.toFixed(4) }}</strong>
          </span>
        </div>
      </div>
      <div #chartContainer class="chart-container"></div>
      <div class="chart-legend">
        <span class="legend-item macd-line">MACD Line</span>
        <span class="legend-item signal-line">Signal Line</span>
        <span class="legend-item histogram">Histogram</span>
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
      flex-wrap: wrap;
      gap: 8px;
    }
    .chart-header h3 {
      margin: 0;
      font-size: 16px;
      font-weight: 500;
      color: #333;
    }
    .macd-values {
      display: flex;
      gap: 16px;
      font-size: 12px;
    }
    .macd-item {
      color: #666;
    }
    .macd-item strong {
      color: #333;
    }
    .macd-item.positive strong {
      color: #26a69a;
    }
    .macd-item.negative strong {
      color: #ef5350;
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
      width: 12px;
      height: 12px;
      margin-right: 6px;
      border-radius: 2px;
    }
    .legend-item.macd-line::before { background: #2196f3; }
    .legend-item.signal-line::before { background: #ff9800; }
    .legend-item.histogram::before {
      background: linear-gradient(to right, #26a69a 50%, #ef5350 50%);
    }
  `]
})
export class MacdChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('chartContainer') chartContainer!: ElementRef;

  @Input() title: string = 'MACD (12, 26, 9)';
  @Input() macdData: MacdValue[] = [];
  @Input() height: number = 200;

  currentMacd: MacdValue | null = null;

  private chart: IChartApi | null = null;
  private macdSeries: ISeriesApi<'Line'> | null = null;
  private signalSeries: ISeriesApi<'Line'> | null = null;
  private histogramSeries: ISeriesApi<'Histogram'> | null = null;

  ngAfterViewInit(): void {
    this.initChart();
    this.updateChart();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (this.chart && changes['macdData']) {
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
      rightPriceScale: {
        borderColor: '#ddd'
      },
      timeScale: {
        borderColor: '#ddd',
        timeVisible: true
      }
    });

    // Histogram series (add first so it's behind lines)
    this.histogramSeries = this.chart.addSeries(HistogramSeries, {
      color: '#26a69a',
      priceLineVisible: false
    });

    // MACD line series
    this.macdSeries = this.chart.addSeries(LineSeries, {
      color: '#2196f3',
      lineWidth: 2,
      priceLineVisible: false
    });

    // Signal line series
    this.signalSeries = this.chart.addSeries(LineSeries, {
      color: '#ff9800',
      lineWidth: 2,
      priceLineVisible: false
    });

    // Add zero line
    if (this.macdSeries) {
      this.macdSeries.createPriceLine({
        price: 0,
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
    if (!this.chart) return;

    if (this.macdData.length > 0) {
      const sortedData = [...this.macdData].sort((a, b) => a.timestamp - b.timestamp);

      // MACD line data
      if (this.macdSeries) {
        const macdLineData: LineData[] = sortedData.map(item => ({
          time: (item.timestamp / 1000) as Time,
          value: item.value
        }));
        this.macdSeries.setData(macdLineData);
      }

      // Signal line data
      if (this.signalSeries) {
        const signalLineData: LineData[] = sortedData.map(item => ({
          time: (item.timestamp / 1000) as Time,
          value: item.signal
        }));
        this.signalSeries.setData(signalLineData);
      }

      // Histogram data with colors
      if (this.histogramSeries) {
        const histogramData: HistogramData[] = sortedData.map(item => ({
          time: (item.timestamp / 1000) as Time,
          value: item.histogram,
          color: item.histogram >= 0 ? '#26a69a' : '#ef5350'
        }));
        this.histogramSeries.setData(histogramData);
      }

      // Get most recent MACD values
      const recentData = [...this.macdData].sort((a, b) => b.timestamp - a.timestamp);
      this.currentMacd = recentData[0] ?? null;
    } else {
      this.macdSeries?.setData([]);
      this.signalSeries?.setData([]);
      this.histogramSeries?.setData([]);
      this.currentMacd = null;
    }

    this.chart.timeScale().fitContent();
  }
}
