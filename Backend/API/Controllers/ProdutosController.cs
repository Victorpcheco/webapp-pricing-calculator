// API/Controllers/ProdutosController.cs
using Application.Produtos.Commands;
using Application.Produtos.Queries;
using Application.Produtos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/produtos")]
[Authorize]
public class ProdutosController : ControllerBase
{
    private readonly ProdutoService _produtoService;

    public ProdutosController(ProdutoService produtoService)
    {
        _produtoService = produtoService;
    }

    /// <summary>GET /api/produtos — lista as fichas do usuário com os custos recalculados.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] ListarProdutosQuery query, CancellationToken ct)
    {
        var resultado = await _produtoService.ListarAsync(query, ct);
        if (resultado.IsFailure)
            return BadRequest(new { Error = resultado.Error });

        return Ok(resultado.Value);
    }

    /// <summary>POST /api/produtos — cria uma ficha técnica.</summary>
    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarProdutoCommand command, CancellationToken ct)
    {
        var resultado = await _produtoService.CriarAsync(command, ct);
        if (resultado.IsFailure)
            return BadRequest(new { Error = resultado.Error });

        return CreatedAtAction(nameof(Listar), resultado.Value);
    }

    /// <summary>PUT /api/produtos/{id} — substitui a ficha, inclusive a composição.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarProdutoCommand command, CancellationToken ct)
    {
        // O id da rota é a fonte de verdade; um id enviado no body é ignorado
        var commandComId = command with { Id = id };
        var resultado = await _produtoService.AtualizarAsync(commandComId, ct);

        if (resultado.IsFailure)
            return BadRequest(new { Error = resultado.Error });

        return Ok(resultado.Value);
    }

    /// <summary>DELETE /api/produtos/{id} — remove a ficha e sua composição.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        var resultado = await _produtoService.ExcluirAsync(new ExcluirProdutoCommand(id), ct);

        if (resultado.IsFailure)
            return NotFound(new { Error = resultado.Error });

        return NoContent();
    }

    /// <summary>DELETE /api/produtos — apaga todas as fichas do usuário ("Limpar dados").</summary>
    [HttpDelete]
    public async Task<IActionResult> Limpar(CancellationToken ct)
    {
        await _produtoService.LimparAsync(new LimparProdutosCommand(), ct);
        return NoContent();
    }
}
