# Guideline 02 - Data Layer

## Goal
The EF models, DbContext and first migration. No controllers in this session.

## Owns
- `Models/Product.cs`, `Cart.cs`, `CartItem.cs`, `Order.cs`, `OrderItem.cs` (shapes defined in
  guideline 01)
- `Data/StoreDbContext.cs` - `DbSet<T>` for each model, `OnModelCreating` with explicit decimal
  precision if any money field ends up as decimal anywhere (it should not - see CLAUDE.md Key
  Trap 1, everything money-related is `int` cents)
- Initial EF migration
- Seed data: at least 5 real or placeholder products with images, prices and stock

## Requirements
- Foreign key relationships must cascade delete appropriately (deleting a Cart deletes its
  CartItems, deleting an Order deletes its OrderItems) but must NOT cascade delete a Product
  when a CartItem or OrderItem references it - `OrderItem` already snapshots `ProductName` and
  `UnitPriceCents` specifically so historical orders survive a product being deleted or
  changed later
- `Cart.SessionId` should be indexed - it is how every cart lookup happens

## Depends on
Nothing. This is the first session.
