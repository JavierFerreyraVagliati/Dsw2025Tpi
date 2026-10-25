using Dsw2025Tpi.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Dsw2025Tpi.Data;

// ✅ Hereda de IdentityDbContext en lugar de DbContext
public class Dsw2025TpiContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public Dsw2025TpiContext(DbContextOptions<Dsw2025TpiContext> options)
        : base(options)
    {
    }

    // DbSets para tus entidades
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // ✅ IMPORTANTE: Llamar al base primero

        // ✅ Configurar nombres de tablas de Identity (igual que en AuthenticateContext)
        modelBuilder.Entity<IdentityUser>(b => { b.ToTable("Usuarios"); });
        modelBuilder.Entity<IdentityRole>(b => { b.ToTable("Roles"); });
        modelBuilder.Entity<IdentityUserRole<string>>(b => { b.ToTable("UsuariosRoles"); });
        modelBuilder.Entity<IdentityUserClaim<string>>(b => { b.ToTable("UsuariosClaims"); });
        modelBuilder.Entity<IdentityUserLogin<string>>(b => { b.ToTable("UsuariosLogins"); });
        modelBuilder.Entity<IdentityRoleClaim<string>>(b => { b.ToTable("RolesClaims"); });
        modelBuilder.Entity<IdentityUserToken<string>>(b => { b.ToTable("UsuariosTokens"); });

        // Configuración de Customer
        modelBuilder.Entity<Customer>(customer =>
        {
            customer.ToTable("Customers");
            customer.HasKey(c => c.Id);
            customer.Property(c => c.Email)
                    .HasMaxLength(100);
            customer.Property(c => c.Name)
                    .HasMaxLength(100);
            customer.Property(c => c.PhoneNumber)
                    .HasMaxLength(20);
            customer.HasMany(c => c.Order)
                    .WithOne(o => o.Customer)
                    .HasForeignKey(o => o.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

            // ✅ Foreign key a la tabla Usuarios (IdentityUser)
            customer.HasOne<IdentityUser>()
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(order =>
        {
            order.ToTable("Orders");
            order.HasKey(o => o.Id);
            order.Property(o => o.ShippingAddress)
                 .HasMaxLength(200);
            order.Property(o => o.BillingAddress)
                 .HasMaxLength(200);
            order.Property(o => o.Notes)
                 .HasMaxLength(500);
            order.Property(o => o.Date)
                 .IsRequired();
            order.Property(o => o.OrderStatus)
                 .IsRequired();
            order.HasOne(o => o.Customer)
                 .WithMany(c => c.Order)
                 .HasForeignKey(o => o.CustomerId)
                 .OnDelete(DeleteBehavior.Restrict);
            order.HasMany(o => o.OrderItem)
                 .WithOne(oi => oi.Order)
                 .HasForeignKey(oi => oi.OrderId)
                 .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(item =>
        {
            item.ToTable("OrderItems");
            item.HasKey(i => i.Id);
            item.Property(i => i.UnitPrice)
                .HasPrecision(15, 2)
                .IsRequired();
            item.Property(i => i.Quantity)
                .IsRequired();
            item.HasOne(i => i.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(product =>
        {
            product.ToTable("Products");
            product.HasKey(p => p.Id);
            product.Property(p => p.Sku)
                   .HasMaxLength(20)
                   .IsRequired();
            product.HasIndex(p => p.Sku)
                   .IsUnique();
            product.Property(p => p.Name)
                   .HasMaxLength(60)
                   .IsRequired();
            product.Property(p => p.Description)
                   .HasMaxLength(500);
            product.Property(p => p.CurrentUnitPrice)
                   .HasPrecision(15, 2)
                   .IsRequired();
            product.Property(p => p.StockQuantity)
                   .IsRequired();
            product.Property(p => p.IsActive)
                   .IsRequired();
        });
    }
}