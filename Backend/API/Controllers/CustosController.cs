// API/Controllers/CustosController.cs
using Application.Custos.Commands;
using Application.Custos.Queries;
using Application.Custos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/custos")]
[Authorize]
public class CustosController : ControllerBase
{
    private readonly CustoService _custoService;

    public CustosController(CustoService custoService)
    {
        _custoService = custoService;
    }

    /// <summary>GET /api/custos — retorna o histórico do usuário autenticado, do mais recente ao mais antigo.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var resultado = await _custoService.ListarAsync(new ListarCustosQuery(), ct);
        return Ok(resultado);
    }

    /// <summary>POST /api/custos — persiste uma nova configuração de custo.</summary>
    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarCustoCommand command, CancellationToken ct)
    {
        var resultado = await _custoService.CriarAsync(command, ct);
        if (resultado.IsFailure)
            return BadRequest(new { Error = resultado.Error });

        return CreatedAtAction(nameof(Listar), resultado.Value);
    }

    /// <summary>PUT /api/custos/{id} — atualiza os valores de uma configuração existente.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarCustoCommand command, CancellationToken ct)
    {
        var commandComId = command with { Id = id };
        var resultado = await _custoService.AtualizarAsync(commandComId, ct);

        if (resultado.IsFailure)
            return BadRequest(new { Error = resultado.Error });

        return Ok(resultado.Value);
    }

    /// <summary>DELETE /api/custos/{id} — remove uma configuração específica do histórico.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        var resultado = await _custoService.ExcluirAsync(new ExcluirCustoCommand(id), ct);

        if (resultado.IsFailure)
            return NotFound(new { Error = resultado.Error });

        return NoContent();
    }

    /// <summary>DELETE /api/custos — apaga todo o histórico do usuário autenticado.</summary>
    [HttpDelete]
    public async Task<IActionResult> LimparHistorico(CancellationToken ct)
    {
        await _custoService.LimparHistoricoAsync(new LimparHistoricoCustosCommand(), ct);
        return NoContent();
    }
}
