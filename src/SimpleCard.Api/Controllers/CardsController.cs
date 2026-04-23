using MediatR;
using Microsoft.AspNetCore.Mvc;
using SimpleCard.Api.Models;
using SimpleCard.Application.Cards.Commands.CreateCard;
using SimpleCard.Application.Cards.Commands.CreateTransaction;
using SimpleCard.Application.Cards.Queries.GetCardBalance;

namespace SimpleCard.Api.Controllers;

[ApiController]
[Route("api/cards")]
public class CardsController(ISender mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateCardResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateCardResponse>> CreateCard(
        [FromBody] CreateCardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateCardCommand(request.CreditLimit), cancellationToken);
        return CreatedAtAction(nameof(GetBalance), new { cardId = result.Id }, result);
    }

    [HttpPost("{cardId:guid}/transactions")]
    [ProducesResponseType(typeof(CreateTransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreateTransactionResponse>> CreateTransaction(
        Guid cardId,
        [FromBody] CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTransactionCommand(
            cardId, request.Description, request.TransactionDate!.Value, request.Amount);

        var result = await mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            actionName: nameof(TransactionsController.GetTransaction),
            controllerName: "Transactions",
            routeValues: new { transactionId = result.Id },
            value: result);
    }

    [HttpGet("{cardId:guid}/balance")]
    [ProducesResponseType(typeof(CardBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CardBalanceResponse>> GetBalance(
        Guid cardId,
        [FromQuery] string currency = "USD",
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetCardBalanceQuery(cardId, currency), cancellationToken);
        return Ok(result);
    }
}
