import { Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';

export interface PriceUpdate {
  symbol: string;
  price: number;
  timestamp: number;
}

export interface TradeUpdate {
  symbol: string;
  price: number;
  volume: number;
  timestamp: number;
}

@Injectable({
  providedIn: 'root'
})
export class RealtimeService implements OnDestroy {
  private hubConnection: signalR.HubConnection | null = null;
  private connectionState$ = new BehaviorSubject<string>('Disconnected');
  private priceUpdates$ = new Subject<PriceUpdate>();
  private tradeUpdates$ = new Subject<TradeUpdate>();
  private prices: Map<string, number> = new Map();
  private subscribedSymbols: Set<string> = new Set();
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;

  constructor() {
    this.initConnection();
  }

  ngOnDestroy(): void {
    this.disconnect();
  }

  private initConnection(): void {
    const hubUrl = `${environment.apiUrl.replace('/api', '')}/hubs/marketdata`;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Handle connection events
    this.hubConnection.onreconnecting((error) => {
      console.log('SignalR reconnecting...', error);
      this.connectionState$.next('Reconnecting');
    });

    this.hubConnection.onreconnected((connectionId) => {
      console.log('SignalR reconnected:', connectionId);
      this.connectionState$.next('Connected');
      this.resubscribeAll();
    });

    this.hubConnection.onclose((error) => {
      console.log('SignalR connection closed:', error);
      this.connectionState$.next('Disconnected');
      this.attemptReconnect();
    });

    // Handle incoming messages
    this.hubConnection.on('ReceivePriceUpdate', (data: PriceUpdate) => {
      this.prices.set(data.symbol, data.price);
      this.priceUpdates$.next(data);
    });

    this.hubConnection.on('ReceiveTrade', (data: TradeUpdate) => {
      this.prices.set(data.symbol, data.price);
      this.tradeUpdates$.next(data);
    });

    this.hubConnection.on('SubscriptionConfirmed', (symbol: string) => {
      console.log(`Subscription confirmed for ${symbol}`);
      this.subscribedSymbols.add(symbol);
    });

    this.hubConnection.on('UnsubscriptionConfirmed', (symbol: string) => {
      console.log(`Unsubscription confirmed for ${symbol}`);
      this.subscribedSymbols.delete(symbol);
    });

    this.hubConnection.on('Pong', (timestamp: number) => {
      console.log('Pong received:', timestamp);
    });

    // Start connection
    this.connect();
  }

  async connect(): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      await this.hubConnection?.start();
      console.log('SignalR connected');
      this.connectionState$.next('Connected');
      this.reconnectAttempts = 0;
    } catch (error) {
      console.error('SignalR connection error:', error);
      this.connectionState$.next('Error');
      this.attemptReconnect();
    }
  }

  private attemptReconnect(): void {
    if (this.reconnectAttempts < this.maxReconnectAttempts) {
      this.reconnectAttempts++;
      const delay = Math.min(1000 * Math.pow(2, this.reconnectAttempts), 30000);
      console.log(`Attempting reconnect in ${delay}ms (attempt ${this.reconnectAttempts})`);
      setTimeout(() => this.connect(), delay);
    }
  }

  private async resubscribeAll(): Promise<void> {
    for (const symbol of this.subscribedSymbols) {
      await this.subscribe(symbol);
    }
  }

  async subscribe(symbol: string): Promise<void> {
    const upperSymbol = symbol.toUpperCase();

    if (this.hubConnection?.state !== signalR.HubConnectionState.Connected) {
      await this.connect();
    }

    try {
      await this.hubConnection?.invoke('SubscribeToSymbol', upperSymbol);
      this.subscribedSymbols.add(upperSymbol);
    } catch (error) {
      console.error(`Failed to subscribe to ${upperSymbol}:`, error);
    }
  }

  async unsubscribe(symbol: string): Promise<void> {
    const upperSymbol = symbol.toUpperCase();

    try {
      await this.hubConnection?.invoke('UnsubscribeFromSymbol', upperSymbol);
      this.subscribedSymbols.delete(upperSymbol);
    } catch (error) {
      console.error(`Failed to unsubscribe from ${upperSymbol}:`, error);
    }
  }

  async subscribeMultiple(symbols: string[]): Promise<void> {
    for (const symbol of symbols) {
      await this.subscribe(symbol);
    }
  }

  async getCurrentPrice(symbol: string): Promise<void> {
    try {
      await this.hubConnection?.invoke('GetCurrentPrice', symbol.toUpperCase());
    } catch (error) {
      console.error(`Failed to get price for ${symbol}:`, error);
    }
  }

  getLatestPrice(symbol: string): number | null {
    return this.prices.get(symbol.toUpperCase()) ?? null;
  }

  // Observables for components to subscribe to
  getPriceUpdates(): Observable<PriceUpdate> {
    return this.priceUpdates$.asObservable();
  }

  getTradeUpdates(): Observable<TradeUpdate> {
    return this.tradeUpdates$.asObservable();
  }

  getConnectionState(): Observable<string> {
    return this.connectionState$.asObservable();
  }

  isConnected(): boolean {
    return this.hubConnection?.state === signalR.HubConnectionState.Connected;
  }

  getSubscribedSymbols(): string[] {
    return Array.from(this.subscribedSymbols);
  }

  disconnect(): void {
    this.hubConnection?.stop();
    this.connectionState$.next('Disconnected');
  }

  // Utility method to format price updates for specific symbol
  getPriceUpdatesForSymbol(symbol: string): Observable<PriceUpdate> {
    const upperSymbol = symbol.toUpperCase();
    return new Observable(subscriber => {
      const subscription = this.priceUpdates$.subscribe(update => {
        if (update.symbol === upperSymbol) {
          subscriber.next(update);
        }
      });
      return () => subscription.unsubscribe();
    });
  }
}
