// Domain/Entities/Produtos/Produto.cs
using Domain.Common;

namespace Domain.Entities.Produtos;

public class Produto
{
    public const int NomeTamanhoMaximo = 80;
    public const int NomeUnidadeTamanhoMaximo = 30;

    /// <summary>Casas decimais dos valores monetários derivados.</summary>
    private const int CasasDecimais = 4;

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public TipoProducao TipoProducao { get; private set; }

    /// <summary>Quantidade produzida pela receita — divisor do custo unitário.</summary>
    public int Rendimento { get; private set; }
    public string NomeUnidade { get; private set; } = string.Empty;
    public int TempoProducaoMinutos { get; private set; }

    private readonly List<ItemComposicao> _composicao = new();
    public IReadOnlyList<ItemComposicao> Composicao => _composicao;

    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    private Produto() { }

    public static Result<Produto> Criar(
        Guid usuarioId,
        string? nome,
        TipoProducao tipoProducao,
        int rendimento,
        string? nomeUnidade,
        int tempoProducaoMinutos,
        IEnumerable<(Guid InsumoId, decimal Quantidade)> composicao)
    {
        var itens = composicao?.ToList() ?? new List<(Guid, decimal)>();

        try { Validar(nome, rendimento, nomeUnidade, tempoProducaoMinutos, itens); }
        catch (ArgumentException ex) { return Result<Produto>.Failure(ex.Message); }

        var agora = DateTime.UtcNow;

        var produto = new Produto
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            CriadoEm = agora,
            AtualizadoEm = agora
        };

        produto.AplicarValores(nome!, tipoProducao, rendimento, nomeUnidade!, tempoProducaoMinutos, itens);

        return Result<Produto>.Success(produto);
    }

    public Result Atualizar(
        string? nome,
        TipoProducao tipoProducao,
        int rendimento,
        string? nomeUnidade,
        int tempoProducaoMinutos,
        IEnumerable<(Guid InsumoId, decimal Quantidade)> composicao)
    {
        var itens = composicao?.ToList() ?? new List<(Guid, decimal)>();

        try { Validar(nome, rendimento, nomeUnidade, tempoProducaoMinutos, itens); }
        catch (ArgumentException ex) { return Result.Failure(ex.Message); }

        AplicarValores(nome!, tipoProducao, rendimento, nomeUnidade!, tempoProducaoMinutos, itens);
        AtualizadoEm = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Calcula a ficha com os preços vigentes. Nada aqui é persistido —
    /// o custo do produto acompanha a variação dos insumos e do valor da hora.
    /// </summary>
    public FichaCalculada Calcular(IReadOnlyDictionary<Guid, decimal> custoUnitarioPorInsumo, decimal valorHora)
    {
        var linhas = _composicao
            .Select(item =>
            {
                // Insumo excluído depois da ficha criada: linha sobrevive com custo zero
                var custoUnitario = custoUnitarioPorInsumo.TryGetValue(item.InsumoId, out var custo) ? custo : 0m;
                return new LinhaCalculada(
                    InsumoId: item.InsumoId,
                    Quantidade: item.Quantidade,
                    CustoUnitarioInsumo: custoUnitario,
                    Custo: Arredondar(item.Quantidade * custoUnitario)
                );
            })
            .ToList();

        var materiais = Arredondar(linhas.Sum(linha => linha.Custo));
        var trabalho = Arredondar((TempoProducaoMinutos / 60m) * Math.Max(0m, valorHora));
        var total = Arredondar(materiais + trabalho);
        var unitario = Rendimento > 0 ? Arredondar(total / Rendimento) : 0m;

        return new FichaCalculada(linhas, materiais, trabalho, total, unitario);
    }

    private void AplicarValores(
        string nome,
        TipoProducao tipoProducao,
        int rendimento,
        string nomeUnidade,
        int tempoProducaoMinutos,
        IReadOnlyList<(Guid InsumoId, decimal Quantidade)> composicao)
    {
        Nome = nome.Trim();
        TipoProducao = tipoProducao;
        Rendimento = rendimento;
        NomeUnidade = nomeUnidade.Trim();
        TempoProducaoMinutos = tempoProducaoMinutos;

        // A composição enviada substitui a anterior por inteiro
        _composicao.Clear();
        foreach (var (insumoId, quantidade) in composicao)
            _composicao.Add(new ItemComposicao(insumoId, quantidade));
    }

    private static void Validar(
        string? nome,
        int rendimento,
        string? nomeUnidade,
        int tempoProducaoMinutos,
        IReadOnlyList<(Guid InsumoId, decimal Quantidade)> composicao)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome do produto é obrigatório.");
        if (nome.Trim().Length > NomeTamanhoMaximo)
            throw new ArgumentException($"O nome deve ter no máximo {NomeTamanhoMaximo} caracteres.");
        if (string.IsNullOrWhiteSpace(nomeUnidade))
            throw new ArgumentException("O nome da unidade é obrigatório.");
        if (nomeUnidade.Trim().Length > NomeUnidadeTamanhoMaximo)
            throw new ArgumentException($"O nome da unidade deve ter no máximo {NomeUnidadeTamanhoMaximo} caracteres.");
        if (rendimento < 1)
            throw new ArgumentException("O rendimento deve ser de no mínimo 1.");
        if (tempoProducaoMinutos < 0)
            throw new ArgumentException("O tempo de produção não pode ser negativo.");

        if (composicao.Any(item => item.InsumoId == Guid.Empty))
            throw new ArgumentException("Há um item da composição sem insumo selecionado.");
        if (composicao.Any(item => item.Quantidade <= 0))
            throw new ArgumentException("A quantidade usada de cada insumo deve ser maior que zero.");

        var duplicado = composicao
            .GroupBy(item => item.InsumoId)
            .FirstOrDefault(grupo => grupo.Count() > 1);
        if (duplicado is not null)
            throw new ArgumentException("O mesmo insumo aparece mais de uma vez na composição. Some as quantidades em uma única linha.");
    }

    private static decimal Arredondar(decimal valor) =>
        Math.Round(valor, CasasDecimais, MidpointRounding.AwayFromZero);
}
