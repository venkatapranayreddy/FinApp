import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  AddTradeRequest,
  TradeRiskAnalysis,
  PortfolioSummary,
  PositionSizeRequest,
  PositionSizeResponse,
  PriceResponse
} from '../models/portfolio.model';

@Injectable({
  providedIn: 'root'
})
export class PortfolioService {
  private apiUrl = `${environment.apiUrl}/portfolio`;

  constructor(private http: HttpClient) {}

  // Get current price for a ticker
  getCurrentPrice(ticker: string): Observable<PriceResponse> {
    return this.http.get<PriceResponse>(`${this.apiUrl}/price/${ticker.toUpperCase()}`);
  }

  // Calculate position size based on risk
  calculatePositionSize(request: PositionSizeRequest): Observable<PositionSizeResponse> {
    return this.http.post<PositionSizeResponse>(`${this.apiUrl}/calculate-position`, request);
  }

  // Add a new trade
  addTrade(trade: AddTradeRequest): Observable<TradeRiskAnalysis> {
    return this.http.post<TradeRiskAnalysis>(`${this.apiUrl}/trades`, trade);
  }

  // Get all trades with portfolio summary
  getTrades(): Observable<PortfolioSummary> {
    return this.http.get<PortfolioSummary>(`${this.apiUrl}/trades`);
  }

  // Get a single trade
  getTrade(id: string): Observable<TradeRiskAnalysis> {
    return this.http.get<TradeRiskAnalysis>(`${this.apiUrl}/trades/${id}`);
  }

  // Close a trade
  closeTrade(id: string, closePrice: number): Observable<TradeRiskAnalysis> {
    return this.http.put<TradeRiskAnalysis>(`${this.apiUrl}/trades/${id}/close?closePrice=${closePrice}`, {});
  }

  // Delete a trade
  deleteTrade(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/trades/${id}`);
  }

  // Update portfolio settings
  updatePortfolioSettings(portfolioValue: number): Observable<{ message: string; portfolioValue: number }> {
    return this.http.put<{ message: string; portfolioValue: number }>(`${this.apiUrl}/settings`, { portfolioValue });
  }
}
