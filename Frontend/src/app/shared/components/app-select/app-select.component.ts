import { CommonModule } from '@angular/common';
import { Component, ElementRef, HostListener, Input, forwardRef } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export interface AppSelectOption {
  value: string;
  label: string;
}

@Component({
  selector: 'app-select',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './app-select.component.html',
  styleUrl: './app-select.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => AppSelectComponent),
      multi: true
    }
  ]
})
export class AppSelectComponent implements ControlValueAccessor {
  @Input() options: AppSelectOption[] = [];
  @Input() placeholder = 'Selecione';
  @Input() ariaLabel: string | null = null;
  @Input() id: string | null = null;
  @Input() minHeight = 48;
  @Input() disabled = false;

  open = false;
  value = '';
  activeIndex = -1;

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  constructor(private readonly elementRef: ElementRef<HTMLElement>) {}

  get selectedLabel(): string {
    return this.options.find(option => option.value === this.value)?.label ?? this.placeholder;
  }

  writeValue(value: string): void {
    this.value = value ?? '';
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
    this.open ? this.close() : this.openMenu();
  }

  openMenu(): void {
    if (this.disabled || !this.options.length) return;
    this.open = true;
    this.activeIndex = Math.max(0, this.options.findIndex(option => option.value === this.value));
  }

  close(): void {
    this.open = false;
    this.onTouched();
  }

  selectOption(option: AppSelectOption): void {
    this.value = option.value;
    this.onChange(this.value);
    this.close();
  }

  moveActive(delta: number): void {
    if (!this.open) {
      this.openMenu();
      return;
    }
    const last = this.options.length - 1;
    this.activeIndex = Math.min(last, Math.max(0, this.activeIndex + delta));
  }

  confirmActive(): void {
    if (!this.open) {
      this.openMenu();
      return;
    }
    const option = this.options[this.activeIndex];
    if (option) this.selectOption(option);
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
