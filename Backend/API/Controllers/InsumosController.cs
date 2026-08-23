// API/Controllers/InsumosController.cs
using Application.Insumos.Commands;
using Application.Insumos.Queries;
using Application.Insumos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/insumos")]
[Authorize]
public class InsumosController : ControllerBase
{
    private readonly InsumoService _insumoService;

    public InsumosController(InsumoService insumoService)
    {
        _insumoService = insumoService;
    }

    /// <summary>GET /api/insumos — lista os insumos do usuário autenticado, com filtros opcionais de nome e tipo, mais os totais dos cards.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] ListarInsumosQuery query, CancellationToken ct)
    {
        var resultado = await _insumoService.ListarAsync(query, ct);
        if (resultado.IsFailure)
            return BadRequest(new { Error = resultado.Error });

        return Ok(resultado.Value);
    }

    /// <summary>POST /api/insumos — cadastra um novo ingrediente ou embalagem.</summary>
    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarInsumoCommand command, CancellationToken ct)
    {
        var resultado = await _insumoService.CriarAsync(command, ct);
        if (resultado.IsFailure)
            return BadRequest(new { Error = resultado.Error });

        return CreatedAtAction(nameof(Listar), resultado.Value);
    }

    /// <summary>PUT /api/insumos/{id} — substitui os dados do insumo e recalcula o custo padronizado.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarInsumoCommand command, CancellationToken ct)
    {
        // O id da rota é a fonte de verdade; um id enviado no body é ignorado
        var commandComId = command with { Id = id };
        var resultado = await _insumoService.AtualizarAsync(commandComId, ct);

        if (resultado.IsFailure)
            return BadRequest(new { Error = resultado.Error });

        return Ok(resultado.Value);
    }

    /// <summary>DELETE /api/insumos/{id} — remove um insumo específico do cadastro.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        var resultado = await _insumoService.ExcluirAsync(new ExcluirInsumoCommand(id), ct);

        if (resultado.IsFailure)
            return NotFound(new { Error = resultado.Error });

        return NoContent();
    }

    /// <summary>DELETE /api/insumos — apaga todos os insumos do usuário autenticado ("Limpar dados").</summary>
    [HttpDelete]
    public async Task<IActionResult> Limpar(CancellationToken ct)
    {
        await _insumoService.LimparAsync(new LimparInsumosCommand(), ct);
        return NoContent();
    }
}
