import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  TickersResponse,
  SnapshotResponse,
  SnapshotsResponse,
  TopMoversResponse,
  TradesResponse,
  LastTradeResponse,
  QuotesResponse,
  LastQuoteResponse,
  IndicatorResponse,
  MacdResponse,
  AggregatesResponse,
  TickersRequest
} from '../models/market-data.model';

@Injectable({
  providedIn: 'root'
})
export class MarketDataService {
  private apiUrl = `${environment.apiUrl}/marketdata`;

  constructor(private http: HttpClient) {}

  // ============ Tickers ============

  getTickers(exchange: string = 'XNAS', limit: number = 10): Observable<TickersResponse> {
    const params = new HttpParams()
      .set('exchange', exchange)
      .set('limit', limit.toString());

    return this.http.get<TickersResponse>(`${this.apiUrl}/tickers`, { params });
  }

  // ============ Snapshots ============

  getSnapshot(ticker: string): Observable<SnapshotResponse> {
    return this.http.get<SnapshotResponse>(`${this.apiUrl}/snapshot/${ticker.toUpperCase()}`);
  }

  getSnapshots(tickers: string[]): Observable<SnapshotsResponse> {
    const request: TickersRequest = { tickers: tickers.map(t => t.toUpperCase()) };
    return this.http.post<SnapshotsResponse>(`${this.apiUrl}/snapshots`, request);
  }

  getTopMovers(direction: 'gainers' | 'losers' = 'gainers'): Observable<TopMoversResponse> {
    return this.http.get<TopMoversResponse>(`${this.apiUrl}/movers/${direction}`);
  }

  // ============ Trades ============

  getTrades(ticker: string, timestamp?: string, limit: number = 10): Observable<TradesResponse> {
    let params = new HttpParams().set('limit', limit.toString());

    if (timestamp) {
      params = params.set('timestamp', timestamp);
    }

    return this.http.get<TradesResponse>(`${this.apiUrl}/trades/${ticker.toUpperCase()}`, { params });
  }

  getLastTrade(ticker: string): Observable<LastTradeResponse> {
    return this.http.get<LastTradeResponse>(`${this.apiUrl}/trades/${ticker.toUpperCase()}/last`);
  }

  // ============ Quotes ============

  getQuotes(ticker: string, timestamp?: string, limit: number = 10): Observable<QuotesResponse> {
    let params = new HttpParams().set('limit', limit.toString());

    if (timestamp) {
      params = params.set('timestamp', timestamp);
    }

    return this.http.get<QuotesResponse>(`${this.apiUrl}/quotes/${ticker.toUpperCase()}`, { params });
  }

  getLastQuote(ticker: string): Observable<LastQuoteResponse> {
    return this.http.get<LastQuoteResponse>(`${this.apiUrl}/quotes/${ticker.toUpperCase()}/last`);
  }

  // ============ Technical Indicators ============

  getSma(
    ticker: string,
    window: number = 20,
    timespan: string = 'day',
    limit: number = 10
  ): Observable<IndicatorResponse> {
    const params = new HttpParams()
      .set('window', window.toString())
      .set('timespan', timespan)
      .set('limit', limit.toString());

    return this.http.get<IndicatorResponse>(
      `${this.apiUrl}/indicators/sma/${ticker.toUpperCase()}`,
      { params }
    );
  }

  getEma(
    ticker: string,
    window: number = 20,
    timespan: string = 'day',
    limit: number = 10
  ): Observable<IndicatorResponse> {
    const params = new HttpParams()
      .set('window', window.toString())
      .set('timespan', timespan)
      .set('limit', limit.toString());

    return this.http.get<IndicatorResponse>(
      `${this.apiUrl}/indicators/ema/${ticker.toUpperCase()}`,
      { params }
    );
  }

  getMacd(
    ticker: string,
    timespan: string = 'day',
    limit: number = 10
  ): Observable<MacdResponse> {
    const params = new HttpParams()
      .set('timespan', timespan)
      .set('limit', limit.toString());

    return this.http.get<MacdResponse>(
      `${this.apiUrl}/indicators/macd/${ticker.toUpperCase()}`,
      { params }
    );
  }

  getRsi(
    ticker: string,
    window: number = 14,
    timespan: string = 'day',
    limit: number = 10
  ): Observable<IndicatorResponse> {
    const params = new HttpParams()
      .set('window', window.toString())
      .set('timespan', timespan)
      .set('limit', limit.toString());

    return this.http.get<IndicatorResponse>(
      `${this.apiUrl}/indicators/rsi/${ticker.toUpperCase()}`,
      { params }
    );
  }

  // ============ Aggregates ============

  getAggregates(ticker: string, from: Date, to: Date): Observable<AggregatesResponse> {
    const params = new HttpParams()
      .set('from', from.toISOString().split('T')[0])
      .set('to', to.toISOString().split('T')[0]);

    return this.http.get<AggregatesResponse>(
      `${this.apiUrl}/aggregates/${ticker.toUpperCase()}`,
      { params }
    );
  }
}
