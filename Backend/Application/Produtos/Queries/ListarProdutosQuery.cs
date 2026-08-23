// Application/Produtos/Queries/ListarProdutosQuery.cs
namespace Application.Produtos.Queries;

/// <summary>Busca da toolbar da tabela. Sem filtro, retorna todas as fichas do usuário.</summary>
public record ListarProdutosQuery(string? Nome = null);
