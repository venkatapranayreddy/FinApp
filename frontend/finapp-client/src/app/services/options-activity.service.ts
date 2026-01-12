import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  OptionsActivityResponse,
  OptionsActivityDetails,
  UnusualOptionsActivity,
  OptionsActivityFilter,
  OptionsActivityStats
} from '../models/options-activity.model';

@Injectable({
  providedIn: 'root'
})
export class OptionsActivityService {
  private baseUrl = `${environment.apiUrl}/optionsactivity`;

  constructor(private http: HttpClient) {}

  /**
   * Get unusual options activity with filtering and pagination
   */
  getUnusualActivity(filter: OptionsActivityFilter = {}): Observable<OptionsActivityResponse> {
    let params = new HttpParams();

    if (filter.bias) {
      params = params.set('bias', filter.bias);
    }
    if (filter.highIVOnly !== undefined) {
      params = params.set('highIV', filter.highIVOnly.toString());
    }
    if (filter.capCategory) {
      params = params.set('capCategory', filter.capCategory);
    }
    if (filter.minVolume !== undefined) {
      params = params.set('minVolume', filter.minVolume.toString());
    }
    if (filter.minVolumeChange !== undefined) {
      params = params.set('minVolumeChange', filter.minVolumeChange.toString());
    }
    if (filter.sortBy) {
      params = params.set('sortBy', filter.sortBy);
    }
    if (filter.sortDesc !== undefined) {
      params = params.set('sortDesc', filter.sortDesc.toString());
    }
    if (filter.page !== undefined) {
      params = params.set('page', filter.page.toString());
    }
    if (filter.pageSize !== undefined) {
      params = params.set('pageSize', filter.pageSize.toString());
    }

    return this.http.get<OptionsActivityResponse>(`${this.baseUrl}/scanner`, { params });
  }

  /**
   * Get top movers with highest unusual activity
   */
  getTopMovers(count: number = 10): Observable<UnusualOptionsActivity[]> {
    return this.http.get<UnusualOptionsActivity[]>(`${this.baseUrl}/top-movers`, {
      params: { count: count.toString() }
    });
  }

  /**
   * Get detailed options activity for a specific symbol
   */
  getActivityDetails(symbol: string): Observable<OptionsActivityDetails> {
    return this.http.get<OptionsActivityDetails>(`${this.baseUrl}/details/${symbol}`);
  }

  /**
   * Get summary statistics
   */
  getStats(): Observable<OptionsActivityStats> {
    return this.http.get<OptionsActivityStats>(`${this.baseUrl}/stats`);
  }
}
