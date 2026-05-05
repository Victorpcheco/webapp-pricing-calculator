import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AppShellComponent } from '../../shared/app-shell/app-shell.component';

interface RecipeIngredient {
  name: string;
  amount: string;
  unitCost: number;
}

@Component({
  selector: 'app-product-create',
  imports: [CommonModule, FormsModule, RouterLink, AppShellComponent],
  templateUrl: './product-create.component.html',
  styleUrl: './product-create.component.scss'
})
export class ProductCreateComponent {
  readonly categories = ['Bolos', 'Docinhos', 'Brownies', 'Salgados', 'Sobremesas', 'Outros'];
  readonly salesUnits = ['Unidade', 'Caixa', 'Cento', 'Kg', 'Fatia'];

  name = 'Bolo no pote chocolate';
  category = 'Bolos';
  salesUnit = 'Unidade';
  portions = 24;
  packageCost = 0.75;
  operationalCost = 1.2;
  desiredMargin = 62;
  manualPrice = 14;
  description = 'Massa de chocolate, recheio cremoso e cobertura com granulado belga.';

  ingredients: RecipeIngredient[] = [
    { name: 'Chocolate meio amargo', amount: '650 g', unitCost: 27.9 },
    { name: 'Leite condensado', amount: '4 latas', unitCost: 27.2 },
    { name: 'Farinha de trigo', amount: '900 g', unitCost: 4.1 },
    { name: 'Granulado belga', amount: '300 g', unitCost: 9.4 }
  ];

  get recipeCost(): number {
    return this.ingredients.reduce((total, ingredient) => total + this.toNumber(ingredient.unitCost), 0);
  }

  get recipeCostPerPortion(): number {
    return this.divide(this.recipeCost, this.toNumber(this.portions));
  }

  get totalUnitCost(): number {
    return this.recipeCostPerPortion + this.toNumber(this.packageCost) + this.toNumber(this.operationalCost);
  }

  get suggestedPrice(): number {
    const margin = Math.min(Math.max(this.toNumber(this.desiredMargin), 1), 90) / 100;
    return this.divide(this.totalUnitCost, 1 - margin);
  }

  get profitPerUnit(): number {
    return Math.max(this.toNumber(this.manualPrice) - this.totalUnitCost, 0);
  }

  get currentMargin(): number {
    return this.divide(this.profitPerUnit, this.toNumber(this.manualPrice)) * 100;
  }

  addIngredient(): void {
    this.ingredients = [
      ...this.ingredients,
      { name: 'Novo ingrediente', amount: '0 g', unitCost: 0 }
    ];
  }

  removeIngredient(index: number): void {
    this.ingredients = this.ingredients.filter((_ingredient, itemIndex) => itemIndex !== index);
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL'
    }).format(this.toNumber(value));
  }

  formatPercent(value: number): string {
    return `${Math.round(this.toNumber(value))}%`;
  }

  trackByIndex(index: number): number {
    return index;
  }

  trackByValue(_index: number, value: string): string {
    return value;
  }

  private toNumber(value: number): number {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : 0;
  }

  private divide(value: number, divisor: number): number {
    return divisor > 0 ? value / divisor : 0;
  }
}
