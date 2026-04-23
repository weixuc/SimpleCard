namespace SimpleCard.Domain.Entities;

public class Card
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public decimal CreditLimit { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
