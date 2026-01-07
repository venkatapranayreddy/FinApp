import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { FilterRequest } from '../../models/stock.model';

@Component({
  selector: 'app-stock-filters',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonToggleModule
  ],
  templateUrl: './stock-filters.component.html',
  styleUrl: './stock-filters.component.scss'
})
export class StockFiltersComponent {
  @Output() filterChange = new EventEmitter<FilterRequest>();
  @Output() syncRequest = new EventEmitter<void>();

  startDate: Date | null = null;
  endDate: Date | null = null;
  exchange: string = '';
  performanceFilter: string = 'all';

  applyFilters(): void {
    if (!this.startDate || !this.endDate) {
      return;
    }

    const filter: FilterRequest = {
      startDate: this.formatDate(this.startDate),
      endDate: this.formatDate(this.endDate),
      exchange: this.exchange || undefined,
      profitOnly: this.performanceFilter === 'profit' ? true : undefined,
      lossOnly: this.performanceFilter === 'loss' ? true : undefined,
      page: 1,
      pageSize: 50
    };

    this.filterChange.emit(filter);
  }

  onSync(): void {
    this.syncRequest.emit();
  }

  private formatDate(date: Date): string {
    return date.toISOString().split('T')[0];
  }
}
