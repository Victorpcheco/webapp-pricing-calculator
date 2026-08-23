// API/Controllers/ColaboradoresController.cs
using Application.Colaboradores.Commands;
using Application.Colaboradores.Queries;
using Application.Colaboradores.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/colaboradores")]
[Authorize]
public class ColaboradoresController : ControllerBase
{
    private readonly ColaboradorService _colaboradorService;

    public ColaboradoresController(ColaboradorService colaboradorService)
    {
        _colaboradorService = colaboradorService;
    }

    /// <summary>GET /api/colaboradores — lista a equipe do usuário autenticado, com filtros opcionais de busca e contratação, mais os totais dos cards.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] ListarColaboradoresQuery query, CancellationToken ct)
    {
        var resultado = await _colaboradorService.ListarAsync(query, ct);
        if (resultado.IsFailure)
            return BadRequest(new { Error = resultado.Error });

        return Ok(resultado.Value);
    }

    /// <summary>POST /api/colaboradores — cadastra um colaborador CLT ou freelancer.</summary>
    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarColaboradorCommand command, CancellationToken ct)
    {
        var resultado = await _colaboradorService.CriarAsync(command, ct);
        if (resultado.IsFailure)
            return BadRequest(new { Error = resultado.Error });

        return CreatedAtAction(nameof(Listar), resultado.Value);
    }

    /// <summary>PUT /api/colaboradores/{id} — substitui os dados do colaborador e reprovisiona os encargos.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarColaboradorCommand command, CancellationToken ct)
    {
        // O id da rota é a fonte de verdade; um id enviado no body é ignorado
        var commandComId = command with { Id = id };
        var resultado = await _colaboradorService.AtualizarAsync(commandComId, ct);

        if (resultado.IsFailure)
            return BadRequest(new { Error = resultado.Error });

        return Ok(resultado.Value);
    }

    /// <summary>DELETE /api/colaboradores/{id} — remove um colaborador específico do quadro.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        var resultado = await _colaboradorService.ExcluirAsync(new ExcluirColaboradorCommand(id), ct);

        if (resultado.IsFailure)
            return NotFound(new { Error = resultado.Error });

        return NoContent();
    }

    /// <summary>DELETE /api/colaboradores — apaga todos os colaboradores do usuário autenticado ("Limpar dados").</summary>
    [HttpDelete]
    public async Task<IActionResult> Limpar(CancellationToken ct)
    {
        await _colaboradorService.LimparAsync(new LimparColaboradoresCommand(), ct);
        return NoContent();
    }
}
