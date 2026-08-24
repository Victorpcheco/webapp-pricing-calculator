import { CommonModule } from '@angular/common';
import { Component, ElementRef, HostListener, Input, forwardRef } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

const WEEKDAY_LABELS = ['D', 'S', 'T', 'Q', 'Q', 'S', 'S'];
const MONTH_NAMES = [
  'janeiro', 'fevereiro', 'março', 'abril', 'maio', 'junho',
  'julho', 'agosto', 'setembro', 'outubro', 'novembro', 'dezembro'
];

interface DayCell {
  date: Date;
  iso: string;
  day: number;
  inMonth: boolean;
  isToday: boolean;
  isSelected: boolean;
  isMarked: boolean;
}

@Component({
  selector: 'app-date-picker',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './app-date-picker.component.html',
  styleUrl: './app-date-picker.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => AppDatePickerComponent),
      multi: true
    }
  ]
})
export class AppDatePickerComponent implements ControlValueAccessor {
  @Input() placeholder = 'dd/mm/aaaa';
  @Input() ariaLabel: string | null = null;
  @Input() id: string | null = null;
  @Input() minHeight = 50;
  @Input() disabled = false;
  /** Exibe setas extras para andar semana a semana, além da navegação por mês. */
  @Input() weekNavigation = false;
  /** Datas (yyyy-MM-dd) marcadas com um indicador — dias com produtos/simulações cadastradas. */
  @Input() markedDates: string[] = [];

  readonly weekdayLabels = WEEKDAY_LABELS;

  open = false;
  value = '';
  viewDate = new Date();

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  constructor(private readonly elementRef: ElementRef<HTMLElement>) {}

  get displayValue(): string {
    if (!this.value) return '';
    const [y, m, d] = this.value.split('-').map(Number);
    return `${String(d).padStart(2, '0')}/${String(m).padStart(2, '0')}/${y}`;
  }

  get monthLabel(): string {
    return `${MONTH_NAMES[this.viewDate.getMonth()]} de ${this.viewDate.getFullYear()}`;
  }

  get days(): DayCell[] {
    const year = this.viewDate.getFullYear();
    const month = this.viewDate.getMonth();
    const startOffset = new Date(year, month, 1).getDay();
    const gridStart = new Date(year, month, 1 - startOffset);
    const marked = new Set(this.markedDates);
    const todayIso = this.toIso(new Date());

    return Array.from({ length: 42 }, (_, i) => {
      const date = new Date(gridStart.getFullYear(), gridStart.getMonth(), gridStart.getDate() + i);
      const iso = this.toIso(date);
      return {
        date,
        iso,
        day: date.getDate(),
        inMonth: date.getMonth() === month,
        isToday: iso === todayIso,
        isSelected: iso === this.value,
        isMarked: marked.has(iso)
      };
    });
  }

  private toIso(date: Date): string {
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }

  private parseIso(iso: string): Date {
    const [y, m, d] = iso.split('-').map(Number);
    return new Date(y, (m || 1) - 1, d || 1);
  }

  writeValue(value: string): void {
    this.value = value ?? '';
    this.viewDate = this.value ? this.parseIso(this.value) : new Date();
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
    if (isDisabled) this.close();
  }

  toggle(): void {
    if (this.disabled) return;
    this.open ? this.close() : this.openPanel();
  }

  openPanel(): void {
    if (this.disabled) return;
    this.viewDate = this.value ? this.parseIso(this.value) : new Date();
    this.open = true;
  }

  close(): void {
    this.open = false;
    this.onTouched();
  }

  selectDay(cell: DayCell): void {
    this.value = cell.iso;
    this.viewDate = cell.date;
    this.onChange(this.value);
    this.close();
  }

  goToday(): void {
    const today = new Date();
    this.value = this.toIso(today);
    this.viewDate = today;
    this.onChange(this.value);
    this.close();
  }

  clear(): void {
    this.value = '';
    this.onChange('');
    this.close();
  }

  prevMonth(): void {
    this.viewDate = new Date(this.viewDate.getFullYear(), this.viewDate.getMonth() - 1, 1);
  }

  nextMonth(): void {
    this.viewDate = new Date(this.viewDate.getFullYear(), this.viewDate.getMonth() + 1, 1);
  }

  prevWeek(): void {
    this.viewDate = new Date(this.viewDate.getFullYear(), this.viewDate.getMonth(), this.viewDate.getDate() - 7);
  }

  nextWeek(): void {
    this.viewDate = new Date(this.viewDate.getFullYear(), this.viewDate.getMonth(), this.viewDate.getDate() + 7);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.open && !this.elementRef.nativeElement.contains(event.target as Node)) {
      this.close();
    }
  }

  @HostListener('keydown.escape')
  onEscape(): void {
    if (this.open) this.close();
  }
}
