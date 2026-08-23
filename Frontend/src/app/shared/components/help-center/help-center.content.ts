/**
 * ============================================================
 * CENTRAL DE AJUDA — conteúdo das seções
 *
 * Cada seção corresponde a uma tela do sistema e descreve os
 * campos que aparecem nela: o que significam, se são
 * obrigatórios e como preenchê-los.
 *
 * Para documentar um campo novo basta editar este arquivo —
 * o componente monta a navegação e a busca automaticamente.
 * ============================================================
 */

/** Etiqueta exibida ao lado do nome do campo. */
export type HelpFieldTag = 'Obrigatório' | 'Opcional' | 'Automático' | 'Ajustável';

export interface HelpField {
  /** Rótulo exatamente como aparece na tela. */
  label: string;
  tag: HelpFieldTag;
  /** Explicação de para que serve e como preencher. */
  description: string;
  /** Exemplo prático de preenchimento. */
  example?: string;
}

export interface HelpGroup {
  title: string;
  fields: HelpField[];
}

export interface HelpSection {
  /** Identificador usado para abrir a ajuda direto nesta seção. */
  id: string;
  /** Rota da tela — permite abrir a ajuda já na página atual. */
  route: string;
  icon: string;
  label: string;
  summary: string;
  groups: HelpGroup[];
  /** Dicas rápidas exibidas no rodapé da seção. */
  tips: string[];
}

export const HELP_SECTIONS: HelpSection[] = [
  /* ===================== VISÃO GERAL ===================== */
  {
    id: 'dashboard',
    route: '/dashboard',
    icon: '⌂',
    label: 'Visão geral',
    summary:
      'Painel inicial com o resumo do seu negócio. Nenhum campo é preenchido aqui: todos os números vêm das outras telas.',
    groups: [
      {
        title: 'Indicadores do topo',
        fields: [
          {
            label: 'Valor da sua hora',
            tag: 'Automático',
            description:
              'Quanto cada hora do seu trabalho precisa render para cobrir todos os custos do mês. Vem da última configuração salva em Meus Custos.',
            example: 'Se aparecer R$ 0,00, ainda falta salvar seus custos.'
          },
          {
            label: 'Insumos cadastrados',
            tag: 'Automático',
            description: 'Total de ingredientes e embalagens registrados na tela Meus Insumos.'
          },
          {
            label: 'Produtos cadastrados',
            tag: 'Automático',
            description: 'Quantos produtos já têm receita e composição montadas em Meus Produtos.'
          },
          {
            label: 'Simulações de lucro',
            tag: 'Automático',
            description: 'Quantidade de cenários de preço salvos na tela Calcular Preços.'
          }
        ]
      },
      {
        title: 'Gráfico e atividade',
        fields: [
          {
            label: 'Desempenho dos Produtos (Custo × Venda)',
            tag: 'Automático',
            description:
              'Compara, produto a produto, o custo de produção com o preço de venda simulado. Barras muito próximas indicam margem apertada.'
          },
          {
            label: 'Atividade Recente',
            tag: 'Automático',
            description: 'Lista dos últimos cadastros e cálculos feitos, do mais novo para o mais antigo.'
          }
        ]
      }
    ],
    tips: [
      'A ordem recomendada de uso é: Meus Custos → Meus Insumos → Meus Produtos → Calcular Preços → Meus Resultados.'
    ]
  },

  /* ===================== MEUS CUSTOS ===================== */
  {
    id: 'custos',
    route: '/meus-custos',
    icon: 'R$',
    label: 'Meus Custos',
    summary:
      'Aqui você informa quanto quer ganhar e quais são as despesas mensais do negócio. O sistema divide esse total pelas horas trabalhadas e descobre o valor da sua hora.',
    groups: [
      {
        title: '01 · Remuneração e rotina',
        fields: [
          {
            label: 'Quanto você quer ganhar por mês?',
            tag: 'Obrigatório',
            description:
              'O salário (pró-labore) que você deseja retirar do negócio todo mês. Não é o faturamento: é o quanto quer receber pelo seu trabalho.',
            example: 'R$ 3.000,00'
          },
          {
            label: 'Quantas horas você trabalha por mês?',
            tag: 'Obrigatório',
            description:
              'Some as horas realmente dedicadas ao negócio no mês. É esse número que divide o custo mensal para chegar ao valor da hora — quanto menos horas, mais cara fica a hora.',
            example: '8 horas por dia × 22 dias = 176 horas'
          }
        ]
      },
      {
        title: '02 · Custos do negócio',
        fields: [
          {
            label: 'Conta de energia mensal — Valor da conta',
            tag: 'Opcional',
            description: 'O valor total da conta de luz do mês, incluindo o consumo da casa.',
            example: 'R$ 450,00'
          },
          {
            label: 'Conta de energia mensal — Uso profissional (%)',
            tag: 'Opcional',
            description:
              'Quanto por cento dessa conta corresponde ao trabalho. Só essa fatia entra no cálculo; o restante é considerado gasto pessoal.',
            example: '30% de R$ 450,00 = R$ 135,00 no custo'
          },
          {
            label: 'Gasto mensal com gás — Valor gasto',
            tag: 'Opcional',
            description: 'Quanto você gasta de gás por mês, somando os botijões ou a conta do gás encanado.',
            example: 'R$ 180,00'
          },
          {
            label: 'Gasto mensal com gás — Uso profissional (%)',
            tag: 'Opcional',
            description: 'A parcela do gás usada na produção. Se o botijão for exclusivo do trabalho, informe 100%.',
            example: '70%'
          },
          {
            label: 'Você possui MEI e paga DAS?',
            tag: 'Opcional',
            description:
              'Chave liga/desliga. Quando ativada, libera o campo do DAS e soma o valor integral aos custos do mês. Desligada, o DAS é ignorado no cálculo.'
          },
          {
            label: 'Valor mensal do DAS',
            tag: 'Opcional',
            description:
              'A guia mensal do MEI. Entra 100% no custo, sem proporção, porque é uma despesa exclusiva do negócio. Só fica editável com a chave acima ativada.',
            example: 'R$ 80,90'
          }
        ]
      },
      {
        title: '03 · Reserva para equipamentos',
        fields: [
          {
            label: 'Percentual de depreciação',
            tag: 'Ajustável',
            description:
              'Uma reserva sobre o custo mensal para repor forno, batedeira, ferramentas e outros equipamentos que se desgastam. Comece com os 5% sugeridos e aumente se você usa muitos equipamentos.',
            example: '5% aplicado sobre a soma dos demais custos'
          }
        ]
      },
      {
        title: 'Painel de resultado (lado direito)',
        fields: [
          {
            label: 'Valor por hora',
            tag: 'Automático',
            description:
              'O número calculado em tempo real: (energia proporcional + gás proporcional + DAS + salário + depreciação) ÷ horas trabalhadas.'
          },
          {
            label: 'Composição mensal',
            tag: 'Automático',
            description:
              'Mostra linha a linha quanto cada despesa representa no total do mês. Serve para conferir se algum valor foi digitado errado.'
          }
        ]
      },
      {
        title: 'Histórico salvo',
        fields: [
          {
            label: 'Buscar por descrição ou data',
            tag: 'Opcional',
            description: 'Filtra as configurações já salvas pelo nome do cálculo ou pela data em que foi registrado.'
          },
          {
            label: 'Ações ✎ (editar) e × (excluir)',
            tag: 'Opcional',
            description:
              'Editar traz os valores de volta para o formulário para você ajustar e salvar de novo. Excluir remove a configuração da lista.'
          }
        ]
      }
    ],
    tips: [
      'Preencha esta tela antes das outras: o valor da hora daqui é usado no custo de todos os produtos.',
      'Só salário e horas são obrigatórios — os demais campos podem ficar em branco e ser completados depois.'
    ]
  },

  /* ===================== MEUS INSUMOS ===================== */
  {
    id: 'insumos',
    route: '/meus-insumos',
    icon: '◇',
    label: 'Meus Insumos',
    summary:
      'Cadastro dos ingredientes e embalagens que você compra. Informe a última compra e o sistema calcula quanto custa cada grama, mililitro ou unidade.',
    groups: [
      {
        title: 'Formulário de cadastro',
        fields: [
          {
            label: 'Tipo: Ingrediente ou Embalagem',
            tag: 'Obrigatório',
            description:
              'Ingrediente é o que vai dentro do produto (farinha, leite). Embalagem é o que envolve o produto (pote, sacola, fita). Serve para organizar a lista e o filtro.'
          },
          {
            label: 'Código / ID',
            tag: 'Opcional',
            description:
              'Um código seu para localizar o item mais rápido. Se ficar vazio, o insumo é identificado apenas pelo nome.',
            example: 'INS-01'
          },
          {
            label: 'Nome do item',
            tag: 'Obrigatório',
            description:
              'Como o insumo aparecerá na composição dos produtos. Use nomes específicos para não confundir itens parecidos.',
            example: 'Farinha de trigo — em vez de apenas "Farinha"'
          },
          {
            label: 'Quantidade comprada',
            tag: 'Obrigatório',
            description:
              'O tamanho da embalagem que você comprou, no mesmo padrão da nota fiscal. Aceita valores quebrados.',
            example: 'Um pacote de 5 kg → digite 5'
          },
          {
            label: 'Unidade da compra',
            tag: 'Obrigatório',
            description:
              'Quilograma, grama, litro, mililitro ou unidade. Compras em kg viram gramas e em litros viram mililitros automaticamente, para que tudo possa ser comparado na mesma medida.',
            example: 'Quilograma (kg)'
          },
          {
            label: 'Preço total pago',
            tag: 'Obrigatório',
            description:
              'Quanto você pagou pela compra inteira — não o preço por quilo. O sistema faz a divisão e chega ao custo padronizado.',
            example: 'Pagou R$ 24,90 nos 5 kg → digite 24,90'
          }
        ]
      },
      {
        title: 'Lista de insumos',
        fields: [
          {
            label: 'Buscar por nome do item',
            tag: 'Opcional',
            description: 'Filtra a lista conforme você digita o nome do insumo.'
          },
          {
            label: 'Filtro de tipo',
            tag: 'Opcional',
            description: 'Mostra todos os itens, somente ingredientes ou somente embalagens.'
          },
          {
            label: 'Compra cadastrada',
            tag: 'Automático',
            description: 'Mostra a compra informada e a conversão aplicada.',
            example: '5 kg → 5.000 g'
          },
          {
            label: 'Custo padronizado',
            tag: 'Automático',
            description:
              'O preço total dividido pela quantidade convertida: é esse valor que será usado na composição dos produtos.',
            example: 'R$ 24,90 ÷ 5.000 g'
          }
        ]
      }
    ],
    tips: [
      'Sempre que o fornecedor mudar de preço, edite o insumo: o custo dos produtos que o utilizam é recalculado junto.'
    ]
  },

  /* ===================== MEUS PRODUTOS ===================== */
  {
    id: 'produtos',
    route: '/meus-produtos',
    icon: '▦',
    label: 'Meus Produtos',
    summary:
      'Monte a receita de cada produto. O sistema soma o custo dos materiais com o valor do tempo dedicado à produção e chega ao custo por unidade.',
    groups: [
      {
        title: '01 · Informações do produto',
        fields: [
          {
            label: 'Código / ID do Produto',
            tag: 'Opcional',
            description: 'Código interno para organizar seu catálogo. Pode ficar em branco.',
            example: 'PROD-01'
          },
          {
            label: 'Nome do produto',
            tag: 'Obrigatório',
            description: 'Nome pelo qual o produto aparecerá na precificação e nos resultados.',
            example: 'Bolo de chocolate'
          },
          {
            label: 'Tipo de produção',
            tag: 'Obrigatório',
            description:
              'Produto inteiro: a receita gera uma peça vendida por completo. Porções: a receita rende várias unidades vendidas separadamente.'
          },
          {
            label: 'Rendimento',
            tag: 'Obrigatório',
            description:
              'Quantas unidades a receita produz de uma vez. É por esse número que o custo total será dividido — se estiver errado, o custo unitário sai errado.',
            example: 'Um bolo que rende 12 fatias → digite 12'
          },
          {
            label: 'Nome da unidade',
            tag: 'Obrigatório',
            description: 'Como você chama cada unidade vendida. Aparece junto do preço em todas as telas.',
            example: 'fatia, pote, brigadeiro'
          },
          {
            label: 'Tempo total de produção (minutos)',
            tag: 'Opcional',
            description:
              'Todo o tempo gasto na receita, incluindo preparo, montagem e finalização. Ele é multiplicado pelo valor da sua hora (vindo de Meus Custos) e vira o custo do trabalho.',
            example: '90 minutos com a hora a R$ 20,00 = R$ 30,00 de mão de obra'
          }
        ]
      },
      {
        title: '02 · Composição dos materiais',
        fields: [
          {
            label: 'Item cadastrado',
            tag: 'Obrigatório',
            description:
              'Escolha um insumo já cadastrado em Meus Insumos. Se o item não aparecer na lista, cadastre-o primeiro.'
          },
          {
            label: 'Quantidade usada',
            tag: 'Obrigatório',
            description:
              'Quanto desse insumo a receita consome, sempre na unidade padronizada mostrada ao lado (g, ml ou un).',
            example: '500 (gramas de farinha por receita)'
          },
          {
            label: 'Unidade e Custo da linha',
            tag: 'Automático',
            description:
              'A unidade padrão do insumo e quanto aquela quantidade custa. Atualiza sozinho conforme você digita.'
          }
        ]
      },
      {
        title: 'Painel de custo (lado direito)',
        fields: [
          {
            label: 'Custo por unidade',
            tag: 'Automático',
            description:
              'O custo total da receita dividido pelo rendimento. É o valor mínimo para produzir cada unidade, ainda sem lucro.'
          },
          {
            label: 'Composição do custo',
            tag: 'Automático',
            description:
              'Separa quanto veio dos materiais e quanto veio do tempo de produção, usando o valor da hora configurado em Meus Custos.'
          }
        ]
      }
    ],
    tips: [
      'Se o valor da hora aparecer zerado, configure a tela Meus Custos: sem ele o tempo de produção não é cobrado no produto.'
    ]
  },

  /* ===================== MEUS COLABORADORES ===================== */
  {
    id: 'colaboradores',
    route: '/meus-colaboradores',
    icon: 'RH',
    label: 'Meus Colaboradores',
    summary:
      'Cadastro da sua equipe fixa (CLT) e dos prestadores de serviço (Freelancer). Para CLT, o sistema já provisiona os encargos previstos em lei sobre o salário bruto.',
    groups: [
      {
        title: 'Formulário de cadastro',
        fields: [
          {
            label: 'Tipo de contratação: CLT ou Freelancer',
            tag: 'Obrigatório',
            description:
              'CLT é o colaborador com carteira assinada, com direito a FGTS, 13º e férias. Freelancer é o prestador autônomo, pago exatamente pelo combinado, sem encargos trabalhistas.'
          },
          {
            label: 'Código / ID',
            tag: 'Opcional',
            description:
              'Um código seu para localizar o colaborador mais rápido. Se ficar vazio, ele é identificado apenas pelo nome.',
            example: 'COL-01'
          },
          {
            label: 'Nome completo',
            tag: 'Obrigatório',
            description: 'Como o colaborador aparecerá na lista da equipe.'
          },
          {
            label: 'Cargo / função',
            tag: 'Obrigatório',
            description: 'O que a pessoa faz no negócio.',
            example: 'Confeiteira, Designer de embalagens'
          },
          {
            label: 'Data de admissão',
            tag: 'Opcional',
            description: 'Quando o colaborador começou a trabalhar com você.'
          },
          {
            label: 'Telefone',
            tag: 'Opcional',
            description: 'Contato do colaborador com DDD.'
          },
          {
            label: 'Status',
            tag: 'Ajustável',
            description: 'Marca o colaborador como Ativo ou Inativo, sem excluí-lo do cadastro.'
          },
          {
            label: 'Salário bruto mensal (CLT)',
            tag: 'Obrigatório',
            description:
              'O salário informado em carteira, antes dos descontos do colaborador (INSS, IRRF). É sobre esse valor que os encargos legais são calculados.',
            example: 'R$ 1.900,00'
          },
          {
            label: 'Valor combinado (Freelancer)',
            tag: 'Obrigatório',
            description: 'Quanto você paga ao prestador, conforme a forma de pagamento escolhida.',
            example: 'R$ 45,00 por hora'
          },
          {
            label: 'Forma de pagamento (Freelancer)',
            tag: 'Ajustável',
            description: 'Valor fixo mensal, por hora trabalhada ou por serviço entregue.'
          }
        ]
      },
      {
        title: 'Encargos legais do CLT',
        fields: [
          {
            label: 'FGTS (8%)',
            tag: 'Automático',
            description: 'Depósito mensal obrigatório de 8% sobre o salário bruto, recolhido em nome do colaborador.'
          },
          {
            label: '13º salário (1/12 avos)',
            tag: 'Automático',
            description: 'Provisão mensal de um doze avos do salário, referente ao 13º salário do ano.'
          },
          {
            label: 'Férias + 1/3 constitucional',
            tag: 'Automático',
            description:
              'Provisão mensal de um doze avos do salário para as férias, acrescida do terço constitucional sobre esse valor.'
          },
          {
            label: 'Custo total mensal do colaborador',
            tag: 'Automático',
            description: 'Salário bruto somado a todos os encargos acima — o custo real de manter o colaborador CLT por mês.'
          }
        ]
      },
      {
        title: 'Indicadores e lista',
        fields: [
          {
            label: 'Custo mensal da equipe',
            tag: 'Automático',
            description:
              'Soma o custo total dos colaboradores CLT com o valor dos freelancers de pagamento mensal fixo. Freelancers pagos por hora ou por serviço não entram nessa soma, por não terem valor mensal previsível.'
          },
          {
            label: 'Buscar por nome ou cargo / Filtro de contratação',
            tag: 'Opcional',
            description: 'Filtra a lista pelo texto digitado e pelo tipo de contrato (CLT ou Freelancer).'
          }
        ]
      }
    ],
    tips: [
      'Freelancers não geram FGTS, 13º ou férias: o custo deles é só o valor combinado em contrato.',
      'Editar o salário de um colaborador CLT recalcula os encargos automaticamente.'
    ]
  },

  /* ===================== CALCULAR PREÇOS ===================== */
  {
    id: 'precificacao',
    route: '/precificacao',
    icon: '%',
    label: 'Calcular Preços',
    summary:
      'Área de teste: escolha um produto, experimente margens e preços e veja o lucro antes de vender. Nada aqui altera o cadastro do produto.',
    groups: [
      {
        title: '01 · Produto para precificar',
        fields: [
          {
            label: 'Produto ou receita cadastrada',
            tag: 'Obrigatório',
            description:
              'Seleciona a base do cálculo. O custo por unidade é carregado automaticamente da composição salva em Meus Produtos.'
          }
        ]
      },
      {
        title: '02 · Margem desejada',
        fields: [
          {
            label: 'Ajuste rápido da margem (0% a 150%)',
            tag: 'Ajustável',
            description:
              'Barra deslizante para testar margens rapidamente. Ela e o campo Margem estão ligados: mexer em um atualiza o outro.'
          },
          {
            label: 'Margem (%)',
            tag: 'Ajustável',
            description:
              'O percentual de lucro aplicado sobre o custo. Preço sugerido = custo + (custo × margem). Digitando direto no campo é possível passar de 150%.',
            example: 'Custo R$ 10,00 com margem de 80% → preço sugerido R$ 18,00'
          }
        ]
      },
      {
        title: '03 · Simulação de venda',
        fields: [
          {
            label: 'Preço que pretende cobrar',
            tag: 'Opcional',
            description:
              'Use para testar um preço diferente do sugerido — por exemplo, o preço praticado pelos concorrentes. O sistema mostra se ele ainda cobre o custo.',
            example: 'R$ 15,00 quando o sugerido é R$ 18,00'
          },
          {
            label: 'Quantidade estimada',
            tag: 'Opcional',
            description: 'Quantas unidades você espera vender. Serve para projetar o lucro e a receita totais.',
            example: '50 unidades no mês'
          }
        ]
      },
      {
        title: 'Painel de resultado (lado direito)',
        fields: [
          {
            label: 'Preço de venda sugerido',
            tag: 'Automático',
            description: 'O preço calculado a partir do custo real com a margem escolhida.'
          },
          {
            label: 'Indicador de viabilidade',
            tag: 'Automático',
            description:
              'Sinaliza se o preço testado gera lucro, fica no ponto de equilíbrio (só cobre o custo) ou dá prejuízo.'
          },
          {
            label: 'Diferença do sugerido',
            tag: 'Automático',
            description: 'Quanto o preço que você digitou está acima ou abaixo do preço sugerido pelo sistema.'
          }
        ]
      }
    ],
    tips: [
      'Salvar a simulação é o que faz o produto aparecer em Meus Resultados.',
      'Margem sobre o custo não é a mesma coisa que margem sobre a venda — a margem real aparece na tela de resultados.'
    ]
  },

  /* ===================== MEUS RESULTADOS ===================== */
  {
    id: 'resultados',
    route: '/meus-resultados',
    icon: '↗',
    label: 'Meus Resultados',
    summary:
      'Reúne as simulações salvas para comparar o desempenho dos produtos. Tela de leitura: o único campo preenchido é o filtro de período.',
    groups: [
      {
        title: 'Filtro de período',
        fields: [
          {
            label: 'Período de Análise',
            tag: 'Ajustável',
            description:
              'Escolha entre Todo o período, Hoje, Esta semana, Este mês ou Personalizado. Recorta quais simulações entram nos indicadores e na tabela.'
          },
          {
            label: 'Data inicial e Data final',
            tag: 'Opcional',
            description:
              'Só aparecem no período Personalizado. Informe as duas datas e clique em Filtrar para aplicar o intervalo.'
          }
        ]
      },
      {
        title: 'Indicadores',
        fields: [
          {
            label: 'Lucro total estimado',
            tag: 'Automático',
            description: 'Soma do lucro de todas as unidades simuladas no período. Em vermelho, indica prejuízo.'
          },
          {
            label: 'Margem média real',
            tag: 'Automático',
            description:
              'A margem calculada sobre o preço de venda, ponderada pelo volume — diferente da margem sobre o custo usada na simulação.'
          },
          {
            label: 'Receita total projetada',
            tag: 'Automático',
            description: 'Preço de venda × quantidade estimada, somando todas as simulações do período.'
          },
          {
            label: 'Produtos analisados',
            tag: 'Automático',
            description: 'Quantas simulações estão sendo consideradas no período escolhido.'
          }
        ]
      },
      {
        title: 'Tabela de desempenho',
        fields: [
          {
            label: 'Custo unitário e Preço de venda',
            tag: 'Automático',
            description: 'Compara quanto custa produzir uma unidade e por quanto ela está sendo vendida.'
          },
          {
            label: 'Lucro / unidade',
            tag: 'Automático',
            description: 'Preço de venda menos o custo unitário. Valor negativo significa que a venda dá prejuízo.'
          },
          {
            label: 'Margem real',
            tag: 'Automático',
            description: 'Quanto do preço de venda sobra como lucro, em porcentagem.'
          },
          {
            label: 'Viabilidade',
            tag: 'Automático',
            description:
              'Resumo da situação do produto: com lucro, no equilíbrio ou em prejuízo. Use para decidir o que reajustar primeiro.'
          }
        ]
      }
    ],
    tips: ['Se a tabela estiver vazia, amplie o período ou salve novas simulações em Calcular Preços.']
  },

  /* ===================== ACESSO E CONTA ===================== */
  {
    id: 'conta',
    route: '/',
    icon: '☺',
    label: 'Acesso e conta',
    summary: 'Campos das telas de entrada no sistema e opções do menu do usuário.',
    groups: [
      {
        title: 'Criar conta',
        fields: [
          {
            label: 'Nome',
            tag: 'Obrigatório',
            description: 'Seu nome. É ele que aparece na saudação do painel e no canto superior da tela.',
            example: 'Maria Silva'
          },
          {
            label: 'Telefone',
            tag: 'Obrigatório',
            description: 'Telefone de contato com DDD.',
            example: '(11) 99999-9999'
          },
          {
            label: 'E-mail',
            tag: 'Obrigatório',
            description: 'Será o seu login. Use um e-mail que você acessa, pois a recuperação de senha passa por ele.'
          },
          {
            label: 'Senha e Confirmar senha',
            tag: 'Obrigatório',
            description:
              'As duas precisam ser idênticas. A confirmação evita cadastrar a conta com um erro de digitação.'
          }
        ]
      },
      {
        title: 'Recuperação de senha',
        fields: [
          {
            label: 'E-mail (Esqueci a senha)',
            tag: 'Obrigatório',
            description: 'Informe o e-mail cadastrado para receber o código de verificação.'
          },
          {
            label: 'Código de Verificação',
            tag: 'Obrigatório',
            description: 'O código enviado por e-mail. Digite-o junto com a nova senha para concluir a redefinição.'
          },
          {
            label: 'Nova Senha',
            tag: 'Obrigatório',
            description: 'A senha que passará a valer no próximo acesso.'
          }
        ]
      },
      {
        title: 'Menu do usuário',
        fields: [
          {
            label: 'Sair',
            tag: 'Opcional',
            description:
              'Encerra a sessão e volta para a tela de login. Use ao terminar de trabalhar em um computador compartilhado.'
          }
        ]
      }
    ],
    tips: ['O botão ? na barra superior abre esta central já na tela em que você está.']
  }
];
