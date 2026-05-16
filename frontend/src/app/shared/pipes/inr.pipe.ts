import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'inr', standalone: true, pure: true })
export class InrPipe implements PipeTransform {
  /**
   * Indian-locale formatter. compact=true shortens to L / Cr.
   * Examples: 500000 -> "5.0L", 12500000 -> "1.25 Cr"
   */
  transform(value: number | null | undefined, compact: boolean = false): string {
    if (value === null || value === undefined || isNaN(value)) return '—';
    if (compact) {
      if (value >= 10_000_000) return (value / 10_000_000).toFixed(2) + ' Cr';
      if (value >= 100_000) return (value / 100_000).toFixed(1) + 'L';
      if (value >= 1000) return (value / 1000).toFixed(0) + 'K';
    }
    return '₹' + value.toLocaleString('en-IN', { maximumFractionDigits: 0 });
  }
}
