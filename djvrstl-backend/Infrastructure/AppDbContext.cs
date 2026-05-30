using Djvrstl.Backend.Domain;
using Microsoft.EntityFrameworkCore;

namespace Djvrstl.Backend.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingHold> BookingHolds => Set<BookingHold>();
    public DbSet<BookingCalendarBlock> BookingCalendarBlocks => Set<BookingCalendarBlock>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<AdminSession> AdminSessions => Set<AdminSession>();
    public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();
    public DbSet<Lead> Leads => Set<Lead>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(product => product.Id);
            entity.HasIndex(product => product.Slug).IsUnique();
            entity.OwnsOne(product => product.Dimensions);
            entity.Property(product => product.Tags).HasColumnType("text[]");
            entity.Property(product => product.Colors).HasColumnType("text[]");
            entity.Property(product => product.Images).HasColumnType("text[]");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");
            entity.HasKey(customer => customer.Id);
            entity.HasIndex(customer => customer.Email);
            entity.OwnsOne(customer => customer.Address);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(order => order.Id);
            entity.HasIndex(order => order.Status);
            entity.HasIndex(order => order.CreatedAt);
            entity.OwnsOne(order => order.Customer, customer =>
            {
                customer.OwnsOne(snapshot => snapshot.Address);
            });
            entity.HasMany(order => order.Items)
                .WithOne()
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.ProductId);
            entity.OwnsOne(item => item.Dimensions);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("bookings");
            entity.HasKey(booking => booking.Id);
            entity.HasIndex(booking => booking.EventDate);
            entity.HasIndex(booking => booking.Status);
            entity.OwnsOne(booking => booking.EventAddress);
            entity.OwnsOne(booking => booking.Customer, customer =>
            {
                customer.OwnsOne(snapshot => snapshot.Address);
            });
        });

        modelBuilder.Entity<BookingHold>(entity =>
        {
            entity.ToTable("booking_holds");
            entity.HasKey(hold => hold.Id);
            entity.HasIndex(hold => new { hold.EventDate, hold.Status });
            entity.HasIndex(hold => hold.ExpiresAt);
            entity.OwnsOne(hold => hold.QuoteAddress);
            entity.OwnsOne(hold => hold.Customer, customer =>
            {
                customer.OwnsOne(snapshot => snapshot.Address);
            });
        });

        modelBuilder.Entity<BookingCalendarBlock>(entity =>
        {
            entity.ToTable("booking_calendar_blocks");
            entity.HasKey(block => block.Id);
            entity.HasIndex(block => new { block.Date, block.Status });
            entity.OwnsOne(block => block.Customer, customer =>
            {
                customer.OwnsOne(snapshot => snapshot.Address);
            });
        });

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.ToTable("admin_users");
            entity.HasKey(user => user.Id);
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<AdminSession>(entity =>
        {
            entity.ToTable("admin_sessions");
            entity.HasKey(session => session.Id);
            entity.HasIndex(session => session.AdminUserId);
            entity.HasIndex(session => session.ExpiresAt);
            entity.HasIndex(session => session.SessionTokenHash).IsUnique();
        });

        modelBuilder.Entity<PaymentEvent>(entity =>
        {
            entity.ToTable("payment_events");
            entity.HasKey(paymentEvent => paymentEvent.Id);
            entity.HasIndex(paymentEvent => paymentEvent.ProviderEventId).IsUnique();
            entity.HasIndex(paymentEvent => paymentEvent.ProviderPaymentId);
            entity.HasIndex(paymentEvent => paymentEvent.ProviderPreferenceId);
        });

        modelBuilder.Entity<Lead>(entity =>
        {
            entity.ToTable("leads");
            entity.HasKey(lead => lead.Id);
            entity.HasIndex(lead => lead.CreatedAt);
            entity.HasIndex(lead => lead.Email);
        });
    }
}
