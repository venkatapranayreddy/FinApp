import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { StockCardComponent } from '../stock-card/stock-card.component';
import { StockPerformance } from '../../models/stock.model';

@Component({
  selector: 'app-stock-list',
  standalone: true,
  imports: [
    CommonModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    StockCardComponent
  ],
  templateUrl: './stock-list.component.html',
  styleUrl: './stock-list.component.scss'
})
export class StockListComponent {
  @Input() stocks: StockPerformance[] = [];
  @Input() totalCount: number = 0;
  @Input() pageSize: number = 50;
  @Input() currentPage: number = 0;
  @Input() loading: boolean = false;

  @Output() pageChange = new EventEmitter<PageEvent>();

  onPageChange(event: PageEvent): void {
    this.pageChange.emit(event);
  }
}
