// API/Controllers/PrecificacoesController.cs
using Application.Precificacoes.Commands;
using Application.Precificacoes.Queries;
using Application.Precificacoes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/precificacoes")]
[Authorize]
public class PrecificacoesController : ControllerBase
{
    private readonly PrecificacaoService _precificacaoService;

    public PrecificacoesController(PrecificacaoService precificacaoService)
    {
        _precificacaoService = precificacaoService;
    }

    /// <summary>GET /api/precificacoes — retorna as simulações do usuário autenticado, da mais recente para a mais antiga.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var resultado = await _precificacaoService.ListarAsync(new ListarSimulacoesQuery(), ct);
        return Ok(resultado);
    }

    /// <summary>POST /api/precificacoes — salva uma simulação, resolvendo o custo atual do produto escolhido.</summary>
    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarSimulacaoCommand command, CancellationToken ct)
    {
        var resultado = await _precificacaoService.CriarAsync(command, ct);
        if (resultado.IsFailure)
            return BadRequest(new { Error = resultado.Error });

        return CreatedAtAction(nameof(Listar), resultado.Value);
    }

    /// <summary>PUT /api/precificacoes/{id} — atualiza uma simulação existente.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarSimulacaoCommand command, CancellationToken ct)
    {
        // O id da rota é a fonte de verdade; um id enviado no body é ignorado
        var commandComId = command with { Id = id };
        var resultado = await _precificacaoService.AtualizarAsync(commandComId, ct);

        if (resultado.IsFailure)
            return BadRequest(new { Error = resultado.Error });

        return Ok(resultado.Value);
    }

    /// <summary>DELETE /api/precificacoes/{id} — remove uma simulação específica.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        var resultado = await _precificacaoService.ExcluirAsync(new ExcluirSimulacaoCommand(id), ct);

        if (resultado.IsFailure)
            return NotFound(new { Error = resultado.Error });

        return NoContent();
    }

    /// <summary>DELETE /api/precificacoes — apaga todas as simulações do usuário autenticado ("Limpar dados").</summary>
    [HttpDelete]
    public async Task<IActionResult> Limpar(CancellationToken ct)
    {
        await _precificacaoService.LimparAsync(new LimparSimulacoesCommand(), ct);
        return NoContent();
    }
}
