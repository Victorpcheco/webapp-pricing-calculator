import { Injectable } from '@angular/core';

/**
 * ============================================================
 * STORE EM MEMÓRIA — DADOS MOCADOS DAS TELAS INTERNAS
 * ============================================================
 * Substitui o `localStorage` usado nos mockups da SIMULAÇÃO.
 * As telas se integram entre si exatamente como no protótipo:
 *
 *   Meus Custos  → valor da hora  → Meus Produtos (custo do trabalho)
 *   Meus Insumos → composição     → Meus Produtos (custo dos materiais)
 *   Meus Produtos→ custo unitário → Calcular Preços
 *   Calcular Preços → simulações  → Meus Resultados
 *
 * Como é um singleton (`providedIn: 'root'`), as alterações
 * sobrevivem à navegação entre rotas, mas se perdem no reload —
 * o backend ainda não existe.
 *
 * ENDPOINTS PREVISTOS (todos com Authorization: Bearer <token>):
 *   GET/POST/PUT/DELETE /api/insumos
 *   GET/POST/PUT/DELETE /api/produtos
 *   GET/POST/PUT/DELETE /api/custos
 *   GET/POST/PUT/DELETE /api/precificacoes
 *   GET/POST/PUT/DELETE /api/colaboradores
 * ============================================================
 */

export type SupplyType = 'Ingrediente' | 'Embalagem';
export type SupplyUnit = 'kg' | 'g' | 'L' | 'ml' | 'un';

export interface Supply {
  id: string;
  name: string;
  type: SupplyType;
  quantity: number;
  unit: SupplyUnit;
  price: number;
  code?: string;
}

export interface CompositionEntry {
  itemId: string;
  amount: number;
}

export interface Product {
  id: string;
  code?: string;
  name: string;
  yieldAmount: number;
  yieldName: string;
  productionTime: number;
  composition: CompositionEntry[];
  materials: number;
  labor: number;
  total: number;
  unitCost: number;
  updatedAt: string;
}

export interface CostResult {
  salary: number;
  hours: number;
  energy: number;
  energyPercent: number;
  gas: number;
  gasPercent: number;
  hasMei: boolean;
  das: number;
  depreciationRate: number;
  energyReal: number;
  gasReal: number;
  depreciation: number;
  monthly: number;
  hour: number;
}

export interface CostHistoryItem extends CostResult {
  id: string;
  description: string;
  createdAt: string;
}

export type EmployeeContractType = 'CLT' | 'Freelancer';
export type EmployeeStatus = 'Ativo' | 'Inativo';
export type FreelancerFrequency = 'Mensal' | 'Por hora' | 'Por serviço';

export interface Employee {
  id: string;
  code?: string;
  name: string;
  role: string;
  contractType: EmployeeContractType;
  status: EmployeeStatus;
  admissionDate: string;
  /** Salário bruto mensal (CLT) ou valor combinado (Freelancer). */
  baseValue: number;
  /** Só se aplica a Freelancer — CLT é sempre mensal. */
  freelancerFrequency?: FreelancerFrequency;
  phone?: string;
}

/** Encargos trabalhistas provisionados para um colaborador CLT. */
export interface CltCharges {
  fgts: number;
  decimoTerceiro: number;
  ferias: number;
  umTercoFerias: number;
  total: number;
}

export interface PricingSimulation {
  id: string;
  recipeId: string;
  recipeName: string;
  cost: number;
  margin: number;
  suggested: number;
  salePrice: number;
  quantity: number;
  profit: number;
  realMargin: number;
  revenue: number;
  totalProfit: number;
  createdAt: string;
}

/** Fatores de conversão para a unidade base (g / ml / un). */
const UNIT_FACTOR: Record<SupplyUnit, number> = { kg: 1000, g: 1, L: 1000, ml: 1, un: 1 };
const BASE_UNIT: Record<SupplyUnit, string> = { kg: 'g', g: 'g', L: 'ml', ml: 'ml', un: 'un' };

function isoDaysAgo(days: number): string {
  return new Date(Date.now() - days * 864e5).toISOString();
}

@Injectable({
  providedIn: 'root'
})
export class MockStoreService {
  /** Insumos — mesmos `demoItems` dos mockups. */
  supplies: Supply[] = [
    { id: 'farinha', name: 'Farinha de trigo', type: 'Ingrediente', quantity: 5, unit: 'kg', price: 24.9 },
    { id: 'leite', name: 'Leite integral', type: 'Ingrediente', quantity: 1, unit: 'L', price: 5.79 },
    { id: 'chocolate', name: 'Chocolate em pó', type: 'Ingrediente', quantity: 2, unit: 'kg', price: 49.9 },
    { id: 'ovos', name: 'Ovos', type: 'Ingrediente', quantity: 12, unit: 'un', price: 10.8 },
    { id: 'caixa', name: 'Caixa para bolo', type: 'Embalagem', quantity: 50, unit: 'un', price: 72.5 },
    { id: 'pote', name: 'Pote com tampa', type: 'Embalagem', quantity: 100, unit: 'un', price: 85.0 }
  ];

  /** Composição de demonstração usada na tela de produtos. */
  readonly demoComposition: CompositionEntry[] = [
    { itemId: 'farinha', amount: 500 },
    { itemId: 'leite', amount: 250 },
    { itemId: 'chocolate', amount: 150 },
    { itemId: 'ovos', amount: 3 },
    { itemId: 'caixa', amount: 1 }
  ];

  products: Product[] = [
    {
      id: 'recipe_demo',
      code: 'PROD-01',
      name: 'Bolo de chocolate',
      yieldAmount: 10,
      yieldName: 'fatia',
      productionTime: 60,
      composition: [
        { itemId: 'farinha', amount: 500 },
        { itemId: 'leite', amount: 250 },
        { itemId: 'chocolate', amount: 150 },
        { itemId: 'ovos', amount: 3 },
        { itemId: 'caixa', amount: 1 }
      ],
      materials: 7.88,
      labor: 19.94,
      total: 27.82,
      unitCost: 2.782,
      updatedAt: isoDaysAgo(0)
    },
    {
      id: 'demo-brigadeiro',
      code: 'PROD-02',
      name: 'Brigadeiro tradicional',
      yieldAmount: 25,
      yieldName: 'unidade',
      productionTime: 45,
      composition: [
        { itemId: 'leite', amount: 395 },
        { itemId: 'chocolate', amount: 80 }
      ],
      materials: 6.55,
      labor: 14.95,
      total: 21.5,
      unitCost: 0.86,
      updatedAt: isoDaysAgo(2)
    },
    {
      id: 'demo-bolo-pote',
      code: 'PROD-03',
      name: 'Bolo no pote',
      yieldAmount: 12,
      yieldName: 'pote',
      productionTime: 90,
      composition: [
        { itemId: 'farinha', amount: 400 },
        { itemId: 'chocolate', amount: 200 },
        { itemId: 'pote', amount: 12 }
      ],
      materials: 22.29,
      labor: 29.91,
      total: 52.2,
      unitCost: 4.35,
      updatedAt: isoDaysAgo(5)
    }
  ];

  /** Colaboradores — mesmos `demoEmployees` dos mockups. */
  employees: Employee[] = [
    {
      id: 'colab_demo_1',
      code: 'COL-01',
      name: 'Juliana Ferreira',
      role: 'Confeiteira',
      contractType: 'CLT',
      status: 'Ativo',
      admissionDate: isoDaysAgo(240),
      baseValue: 1900,
      phone: '(11) 98888-1234'
    },
    {
      id: 'colab_demo_2',
      code: 'COL-02',
      name: 'Rafael Souza',
      role: 'Designer de embalagens',
      contractType: 'Freelancer',
      status: 'Ativo',
      admissionDate: isoDaysAgo(60),
      baseValue: 45,
      freelancerFrequency: 'Por hora'
    }
  ];

  /** Percentuais legais aproximados usados na provisão mensal do custo CLT. */
  private readonly cltRates = {
    fgts: 0.08,
    decimoTerceiro: 1 / 12,
    ferias: 1 / 12,
    umTercoFerias: 1 / 36
  };

  /** Provisão mensal de FGTS, 13º e férias (+1/3) sobre o salário bruto. */
  cltCharges(baseValue: number): CltCharges {
    const base = Math.max(0, Number(baseValue) || 0);
    const fgts = base * this.cltRates.fgts;
    const decimoTerceiro = base * this.cltRates.decimoTerceiro;
    const ferias = base * this.cltRates.ferias;
    const umTercoFerias = base * this.cltRates.umTercoFerias;
    return { fgts, decimoTerceiro, ferias, umTercoFerias, total: fgts + decimoTerceiro + ferias + umTercoFerias };
  }

  /** Custo mensal total: CLT soma os encargos, Freelancer é só o valor combinado. */
  employeeMonthlyCost(employee: Employee): number {
    if (employee.contractType === 'CLT') {
      return employee.baseValue + this.cltCharges(employee.baseValue).total;
    }
    return employee.freelancerFrequency === 'Mensal' ? employee.baseValue : 0;
  }

  /** Configuração de custo ativa — origem do valor da hora. */
  costSettings: CostResult = {
    salary: 3000,
    hours: 176,
    energy: 450,
    energyPercent: 30,
    gas: 180,
    gasPercent: 70,
    hasMei: true,
    das: 80.9,
    depreciationRate: 5,
    energyReal: 135,
    gasReal: 126,
    depreciation: 167.09,
    monthly: 3508.99,
    hour: 19.94
  };

  costHistory: CostHistoryItem[] = [
    {
      id: 'cost_demo',
      description: 'Configuração atual',
      createdAt: isoDaysAgo(0),
      ...this.costSettings
    }
  ];

  simulations: PricingSimulation[] = [
    {
      id: 'pricing-demo-1',
      recipeId: 'recipe_demo',
      recipeName: 'Bolo de chocolate',
      cost: 2.782,
      margin: 40,
      suggested: 3.9,
      salePrice: 3.9,
      quantity: 30,
      profit: 1.118,
      realMargin: 28.67,
      revenue: 117,
      totalProfit: 33.54,
      createdAt: isoDaysAgo(0)
    },
    {
      id: 'pricing-demo-2',
      recipeId: 'demo-brigadeiro',
      recipeName: 'Brigadeiro tradicional',
      cost: 0.86,
      margin: 100,
      suggested: 1.72,
      salePrice: 2.0,
      quantity: 100,
      profit: 1.14,
      realMargin: 57,
      revenue: 200,
      totalProfit: 114,
      createdAt: isoDaysAgo(1)
    },
    {
      id: 'pricing-demo-3',
      recipeId: 'demo-bolo-pote',
      recipeName: 'Bolo no pote',
      cost: 4.35,
      margin: 50,
      suggested: 6.53,
      salePrice: 6.5,
      quantity: 24,
      profit: 2.15,
      realMargin: 33.08,
      revenue: 156,
      totalProfit: 51.6,
      createdAt: isoDaysAgo(4)
    }
  ];

  /** Valor da hora vindo de Meus Custos (18,50 é o fallback do mockup). */
  get hourlyRate(): number {
    const value = Number(this.costSettings?.hour);
    return Number.isFinite(value) && value > 0 ? value : 18.5;
  }

  unitFactor(unit: SupplyUnit): number {
    return UNIT_FACTOR[unit] ?? 1;
  }

  baseUnit(unit: SupplyUnit): string {
    return BASE_UNIT[unit] ?? unit;
  }

  /** Custo do insumo por unidade base (g, ml ou un). */
  supplyUnitCost(supply: Supply): number {
    const baseQuantity = Number(supply.quantity || 0) * this.unitFactor(supply.unit);
    return baseQuantity > 0 ? Number(supply.price || 0) / baseQuantity : 0;
  }

  findSupply(id: string): Supply | undefined {
    return this.supplies.find(item => item.id === id);
  }

  findProduct(id: string): Product | undefined {
    return this.products.find(item => String(item.id) === String(id));
  }

  /** Custo de uma linha da composição de um produto. */
  compositionCost(entry: CompositionEntry): number {
    const supply = this.findSupply(entry.itemId);
    return supply ? Number(entry.amount || 0) * this.supplyUnitCost(supply) : 0;
  }
}
