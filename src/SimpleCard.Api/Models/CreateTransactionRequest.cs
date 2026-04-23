using System.ComponentModel.DataAnnotations;

namespace SimpleCard.Api.Models;

public record CreateTransactionRequest(
    [Required]
    [MaxLength(500)]
    string Description,
    [Required]
    DateOnly? TransactionDate,
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    decimal Amount);
