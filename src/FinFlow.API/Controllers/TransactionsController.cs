using FinFlow.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _svc;

    public TransactionsController(ITransactionService svc) => _svc = svc;

    /// <summary>Submit a new payment transaction.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest(new { error = "Amount must be greater than zero." });

        var tx = await _svc.CreateAsync(request);
        return CreatedAtAction(nameof(GetRecent), new { id = tx.Id }, tx);
    }

    /// <summary>Get the 50 most recent transactions.</summary>
    [HttpGet]
    public async Task<IActionResult> GetRecent()
    {
        var txns = await _svc.GetRecentAsync();
        return Ok(txns);
    }

    /// <summary>Update transaction status (called internally by PaymentService via webhook or polling).</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        await _svc.UpdateStatusAsync(id, request.Status);
        return NoContent();
    }
}

public record UpdateStatusRequest(string Status);
