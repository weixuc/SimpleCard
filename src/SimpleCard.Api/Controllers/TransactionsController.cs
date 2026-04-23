using MediatR;
using Microsoft.AspNetCore.Mvc;
using SimpleCard.Application.Transactions.Queries.GetTransaction;

namespace SimpleCard.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController(ISender mediator) : ControllerBase
{
    [HttpGet("{transactionId:guid}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> GetTransaction(
        Guid transactionId,
        [FromQuery] string currency = "USD",
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetTransactionQuery(transactionId, currency), cancellationToken);
        return Ok(result);
    }
}
