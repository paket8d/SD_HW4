using Microsoft.EntityFrameworkCore;

namespace OrdersService.Data;

public class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<InboxMessage> Inbox => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().ToTable("orders");
        modelBuilder.Entity<Order>().Property(o => o.Id).HasColumnName("id");
        modelBuilder.Entity<Order>().Property(o => o.UserId).HasColumnName("userid");
        modelBuilder.Entity<Order>().Property(o => o.Amount).HasColumnName("amount");
        modelBuilder.Entity<Order>().Property(o => o.Status).HasColumnName("status");
        modelBuilder.Entity<Order>().Property(o => o.CreatedAt).HasColumnName("createdat");

        modelBuilder.Entity<OutboxMessage>().ToTable("outbox");
        modelBuilder.Entity<OutboxMessage>().Property(o => o.Id).HasColumnName("id");
        modelBuilder.Entity<OutboxMessage>().Property(o => o.EventType).HasColumnName("eventtype");
        modelBuilder.Entity<OutboxMessage>().Property(o => o.Payload).HasColumnName("payload");
        modelBuilder.Entity<OutboxMessage>().Property(o => o.Published).HasColumnName("published");
        modelBuilder.Entity<OutboxMessage>().Property(o => o.CreatedAt).HasColumnName("createdat");

        modelBuilder.Entity<InboxMessage>().ToTable("inbox");
        modelBuilder.Entity<InboxMessage>().Property(o => o.Id).HasColumnName("id");
        modelBuilder.Entity<InboxMessage>().Property(o => o.MessageId).HasColumnName("messageid");
        modelBuilder.Entity<InboxMessage>().Property(o => o.Payload).HasColumnName("payload");
        modelBuilder.Entity<InboxMessage>().Property(o => o.ReceivedAt).HasColumnName("receivedat");
        modelBuilder.Entity<InboxMessage>().HasIndex(i => i.MessageId).IsUnique();
        base.OnModelCreating(modelBuilder);
    }
}
