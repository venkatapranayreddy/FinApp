import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { FilterRequest, StockPerformanceResponse, StocksResponse } from '../models/stock.model';

@Injectable({
  providedIn: 'root'
})
export class StockService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getStocks(page: number = 1, pageSize: number = 50, exchange?: string): Observable<StocksResponse> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (exchange) {
      params = params.set('exchange', exchange);
    }

    return this.http.get<StocksResponse>(`${this.apiUrl}/stocks`, { params });
  }

  syncStocks(): Observable<{ message: string; syncedCount: number }> {
    return this.http.get<{ message: string; syncedCount: number }>(`${this.apiUrl}/stocks/sync`);
  }

  getStockPerformance(filter: FilterRequest): Observable<StockPerformanceResponse> {
    return this.http.post<StockPerformanceResponse>(`${this.apiUrl}/stocks/performance`, filter);
  }
}
