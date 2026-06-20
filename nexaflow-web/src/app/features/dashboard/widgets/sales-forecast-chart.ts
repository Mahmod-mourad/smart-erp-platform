import { Component, ElementRef, OnDestroy, effect, input, viewChild } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { Chart } from 'chart.js/auto';
import { SalesForecastResult } from '../../../shared/models/prediction.models';

/** Renders the historical sales series and the ML forecast (with its confidence band). */
@Component({
  selector: 'app-sales-forecast-chart',
  imports: [MatCardModule],
  template: `
    <mat-card class="forecast">
      <mat-card-header>
        <mat-card-title>Sales Forecast</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        @if (forecast()?.isSuccessful) {
          <div class="forecast__canvas"><canvas #chartCanvas></canvas></div>
        } @else {
          <p class="forecast__empty">
            {{ forecast()?.errorMessage ?? 'Not enough sales history yet to forecast.' }}
          </p>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: [
    `
      .forecast__canvas {
        position: relative;
        height: 280px;
      }
      .forecast__empty {
        color: rgba(0, 0, 0, 0.55);
        padding: 24px 4px;
      }
    `,
  ],
})
export class SalesForecastChart implements OnDestroy {
  readonly forecast = input<SalesForecastResult | null>();
  private readonly canvas = viewChild<ElementRef<HTMLCanvasElement>>('chartCanvas');
  private chart?: Chart;

  constructor() {
    effect(() => {
      const data = this.forecast();
      const canvas = this.canvas();
      if (data?.isSuccessful && canvas) {
        this.render(data, canvas.nativeElement);
      }
    });
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  private render(data: SalesForecastResult, canvas: HTMLCanvasElement): void {
    this.chart?.destroy();

    const histLen = data.historicalData.length;
    const labels = [
      ...data.historicalData.map((_, i) => `M-${histLen - i}`),
      ...data.predictions.map((p) => p.month.slice(0, 7)),
    ];
    const pad = (count: number) => new Array(count).fill(null) as (number | null)[];

    const historical = [...data.historicalData, ...pad(data.predictions.length)];
    // Join the two series so the forecast line starts at the last historical point.
    const forecast = [
      ...pad(histLen - 1),
      ...(histLen ? [data.historicalData[histLen - 1]] : []),
      ...data.predictions.map((p) => p.predictedValue),
    ];
    const upper = [...pad(histLen), ...data.predictions.map((p) => p.upperBound)];
    const lower = [...pad(histLen), ...data.predictions.map((p) => p.lowerBound)];

    this.chart = new Chart(canvas, {
      type: 'line',
      data: {
        labels,
        datasets: [
          {
            label: 'Historical',
            data: historical,
            borderColor: '#2563eb',
            borderWidth: 2,
            tension: 0.3,
          },
          {
            label: 'Forecast',
            data: forecast,
            borderColor: '#10b981',
            borderDash: [6, 4],
            borderWidth: 2,
            tension: 0.3,
          },
          {
            label: 'Upper',
            data: upper,
            borderColor: 'rgba(16,185,129,0.25)',
            borderWidth: 1,
            pointRadius: 0,
          },
          {
            label: 'Lower',
            data: lower,
            borderColor: 'rgba(16,185,129,0.25)',
            backgroundColor: 'rgba(16,185,129,0.10)',
            borderWidth: 1,
            pointRadius: 0,
            fill: '-1',
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { position: 'bottom' } },
        scales: {
          y: {
            beginAtZero: false,
            ticks: { callback: (v) => `${(Number(v) / 1000).toFixed(0)}K` },
          },
        },
      },
    });
  }
}
