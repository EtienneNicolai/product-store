using Microsoft.EntityFrameworkCore;
using Store.Api.Models;

namespace Store.Api.Data;

public class StoreDbContext : DbContext
{
    public StoreDbContext(DbContextOptions<StoreDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Slug)
            .IsUnique();

        // Every cart lookup happens by SessionId - see guideline 02.
        modelBuilder.Entity<Cart>()
            .HasIndex(c => c.SessionId);

        modelBuilder.Entity<Cart>()
            .HasMany(c => c.Items)
            .WithOne()
            .HasForeignKey(ci => ci.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict, not Cascade: deleting a Product must never silently delete
        // CartItems/OrderItems that reference it. OrderItem also snapshots
        // ProductName/UnitPriceCents specifically so historical orders survive a
        // product being deleted or repriced later - see Models/OrderItem.cs.
        modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.Product)
            .WithMany()
            .HasForeignKey(ci => ci.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // OrderItem has no Product navigation property (it's a snapshot, not a
        // live reference) but still needs the FK constraint itself set to Restrict.
        modelBuilder.Entity<OrderItem>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Name = "Sticker Pack",
                Slug = "sticker-pack",
                Description = "A set of 5 vinyl stickers.",
                PriceCents = 500,
                Currency = "aud",
                ImageUrl = "https://picsum.photos/seed/sticker-pack/600/400",
                StockQuantity = 100,
                IsActive = true,
            },
            new Product
            {
                Id = 2,
                Name = "Enamel Pin",
                Slug = "enamel-pin",
                Description = "A hard enamel pin, 3cm, with a butterfly clasp back.",
                PriceCents = 1200,
                Currency = "aud",
                ImageUrl = "https://picsum.photos/seed/enamel-pin/600/400",
                StockQuantity = 60,
                IsActive = true,
            },
            new Product
            {
                Id = 3,
                Name = "Coffee Mug",
                Slug = "coffee-mug",
                Description = "A 350ml ceramic mug, dishwasher and microwave safe.",
                PriceCents = 2200,
                Currency = "aud",
                ImageUrl = "https://picsum.photos/seed/coffee-mug/600/400",
                StockQuantity = 40,
                IsActive = true,
            },
            new Product
            {
                Id = 4,
                Name = "Tote Bag",
                Slug = "tote-bag",
                Description = "A heavyweight cotton canvas tote bag.",
                PriceCents = 1800,
                Currency = "aud",
                ImageUrl = "https://picsum.photos/seed/tote-bag/600/400",
                StockQuantity = 50,
                IsActive = true,
            },
            new Product
            {
                Id = 5,
                Name = "Notebook",
                Slug = "notebook",
                Description = "An A5 dot grid notebook, 120 pages.",
                PriceCents = 1500,
                Currency = "aud",
                ImageUrl = "https://picsum.photos/seed/notebook/600/400",
                StockQuantity = 75,
                IsActive = true,
            },
            new Product
            {
                Id = 6,
                Name = "Discontinued Keychain",
                Slug = "discontinued-keychain",
                Description = "A small acrylic keychain. No longer sold.",
                PriceCents = 800,
                Currency = "aud",
                ImageUrl = "https://picsum.photos/seed/keychain/600/400",
                StockQuantity = 0,
                IsActive = false,
            }
        );
    }
}
