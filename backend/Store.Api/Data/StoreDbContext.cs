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

        // GET /api/orders/{id} looks up an order's owning session - see
        // Models/Order.cs for why SessionId lives on Order.
        modelBuilder.Entity<Order>()
            .HasIndex(o => o.SessionId);

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
                Description = "A set of 5 vinyl stickers, perfect for decorating a laptop, water bottle, or journal.",
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
                Description = "A hard enamel pin, 3cm, with a butterfly clasp back - a little something for your jacket or tote.",
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
                Description = "A 350ml ceramic mug for your morning coffee or afternoon tea, dishwasher and microwave safe.",
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
                Description = "A heavyweight cotton canvas tote bag, roomy enough for groceries, books, or everyday errands.",
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
                Description = "An A5 dot grid notebook, 120 pages - good for journaling, planning, or sketching.",
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
            },
            new Product
            {
                Id = 7,
                Name = "Chunky Knit Throw",
                Slug = "chunky-knit-throw",
                Description = "An oversized, soft knit throw blanket - the kind you fight over on movie night.",
                PriceCents = 4500,
                Currency = "aud",
                ImageUrl = "https://picsum.photos/seed/knit-throw/600/400",
                StockQuantity = 35,
                IsActive = true,
            },
            new Product
            {
                Id = 8,
                Name = "Vanilla Cedar Candle",
                Slug = "vanilla-cedar-candle",
                Description = "A hand-poured soy candle in a warm vanilla and cedar scent, roughly 40 hours of burn time.",
                PriceCents = 2800,
                Currency = "aud",
                ImageUrl = "https://picsum.photos/seed/candle/600/400",
                StockQuantity = 45,
                IsActive = true,
            },
            new Product
            {
                Id = 9,
                Name = "Ceramic Coaster Set",
                Slug = "ceramic-coaster-set",
                Description = "A set of 4 handmade ceramic coasters with a soft matte glaze finish.",
                PriceCents = 1800,
                Currency = "aud",
                ImageUrl = "https://picsum.photos/seed/coasters/600/400",
                StockQuantity = 55,
                IsActive = true,
            },
            new Product
            {
                Id = 10,
                Name = "Chamomile Lavender Tea",
                Slug = "chamomile-lavender-tea",
                Description = "A calming loose leaf chamomile and lavender blend, 100g tin, good for winding down.",
                PriceCents = 1600,
                Currency = "aud",
                ImageUrl = "https://picsum.photos/seed/tea-tin/600/400",
                StockQuantity = 70,
                IsActive = true,
            },
            new Product
            {
                Id = 11,
                Name = "Reading Pillow",
                Slug = "reading-pillow",
                Description = "A plush reading pillow with arms and back support, for curling up with a good book.",
                PriceCents = 5200,
                Currency = "aud",
                ImageUrl = "https://picsum.photos/seed/reading-pillow/600/400",
                StockQuantity = 25,
                IsActive = true,
            }
        );
    }
}
