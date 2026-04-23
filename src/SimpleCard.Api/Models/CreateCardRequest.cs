using System.ComponentModel.DataAnnotations;

namespace SimpleCard.Api.Models;

public record CreateCardRequest(
    [Range(0.01, double.MaxValue, ErrorMessage = "CreditLimit must be greater than 0.")]
    decimal CreditLimit);
