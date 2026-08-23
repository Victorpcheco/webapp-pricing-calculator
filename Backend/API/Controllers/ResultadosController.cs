// API/Controllers/ResultadosController.cs
using Application.Resultados.Queries;
using Application.Resultados.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/resultados")]
[Authorize]
public class ResultadosController : ControllerBase
{
    private readonly ResultadoService _resultadoService;

    public ResultadosController(ResultadoService resultadoService)
    {
        _resultadoService = resultadoService;
    }

    /// <summary>
    /// GET /api/resultados?periodo=all|today|week|month|custom&amp;inicio=&amp;fim=
    /// Consolida fichas técnicas e simulações de preço do usuário autenticado no período informado.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] ListarResultadosQuery query, CancellationToken ct)
    {
        var resultado = await _resultadoService.ListarAsync(query, ct);
        return Ok(resultado);
    }
}
