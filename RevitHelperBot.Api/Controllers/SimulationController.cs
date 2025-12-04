using Microsoft.AspNetCore.Mvc;
using RevitHelperBot.Contracts;
using RevitHelperBot.Services;

namespace RevitHelperBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimulationController : ControllerBase
{
    private readonly SimulationRunner runner;
    private readonly ILogger<SimulationController> logger;

    public SimulationController(SimulationRunner runner, ILogger<SimulationController> logger)
    {
        this.runner = runner;
        this.logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Simulate([FromBody] SimulateRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest("Request body is required.");
        }

        try
        {
            var response = await runner.RunAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (FileNotFoundException ex)
        {
            logger.LogError(ex, "Scenario file not found");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Scenario file not found. Please upload it and try /reload.");
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Scenario is not configured");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message);
        }
    }
}
