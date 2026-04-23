using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleCard.Domain.Entities;

namespace SimpleCard.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Description).IsRequired().HasMaxLength(500);
        builder.Property(t => t.Amount).HasPrecision(18, 4).IsRequired();
        builder.HasOne(t => t.Card)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
