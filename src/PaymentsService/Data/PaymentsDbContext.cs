using Microsoft.EntityFrameworkCore;

namespace PaymentsService.Data;

public class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<PaymentTransaction> Transactions => Set<PaymentTransaction>();
    public DbSet<InboxMessage> Inbox => Set<InboxMessage>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("payments");
        modelBuilder.Entity<Account>().ToTable("accounts");
        modelBuilder.Entity<Account>().Property(a => a.UserId).HasColumnName("userid");
        modelBuilder.Entity<Account>().Property(a => a.Balance).HasColumnName("balance");
        modelBuilder.Entity<Account>().Property(a => a.Version).HasColumnName("version");
        modelBuilder.Entity<Account>().HasKey(a => a.UserId);

        modelBuilder.Entity<PaymentTransaction>().ToTable("transactions");
        modelBuilder.Entity<PaymentTransaction>().Property(t => t.Id).HasColumnName("id");
        modelBuilder.Entity<PaymentTransaction>().Property(t => t.OrderId).HasColumnName("orderid");
        modelBuilder.Entity<PaymentTransaction>().Property(t => t.UserId).HasColumnName("userid");
        modelBuilder.Entity<PaymentTransaction>().Property(t => t.Amount).HasColumnName("amount");
        modelBuilder.Entity<PaymentTransaction>().Property(t => t.Status).HasColumnName("status");
        modelBuilder.Entity<PaymentTransaction>().Property(t => t.CreatedAt).HasColumnName("createdat");
        modelBuilder.Entity<PaymentTransaction>().HasIndex(t => t.OrderId).IsUnique();

        modelBuilder.Entity<InboxMessage>().ToTable("inbox");
        modelBuilder.Entity<InboxMessage>().Property(i => i.Id).HasColumnName("id");
        modelBuilder.Entity<InboxMessage>().Property(i => i.MessageId).HasColumnName("messageid");
        modelBuilder.Entity<InboxMessage>().Property(i => i.Payload).HasColumnName("payload");
        modelBuilder.Entity<InboxMessage>().Property(i => i.ReceivedAt).HasColumnName("receivedat");
        modelBuilder.Entity<InboxMessage>().HasIndex(i => i.MessageId).IsUnique();

        modelBuilder.Entity<OutboxMessage>().ToTable("outbox");
        modelBuilder.Entity<OutboxMessage>().Property(o => o.Id).HasColumnName("id");
        modelBuilder.Entity<OutboxMessage>().Property(o => o.EventType).HasColumnName("eventtype");
        modelBuilder.Entity<OutboxMessage>().Property(o => o.Payload).HasColumnName("payload");
        modelBuilder.Entity<OutboxMessage>().Property(o => o.Published).HasColumnName("published");
        modelBuilder.Entity<OutboxMessage>().Property(o => o.CreatedAt).HasColumnName("createdat");

        base.OnModelCreating(modelBuilder);
    }
}
