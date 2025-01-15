using Delivery_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Delivery_API.Data
{
    public class DBContext : DbContext
    {
        public DBContext(DbContextOptions<DBContext> options) : base(options)
        {
        }

        // Define your DbSet for the dishes
        public DbSet<Dish> Dish { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<DishBasketDto> Basket { get; set; }
        public DbSet<OrderDto> Order { get; set; }
        public DbSet<DishOrder> DishOrders { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Category as string in the database
            modelBuilder.Entity<Dish>()
                .Property(d => d.Category)
                .HasConversion(
                    v => v.ToString(),  // Convert to string before saving to DB
                    v => (DishCategory)Enum.Parse(typeof(DishCategory), v)  // Convert back from string to enum when reading from DB
                );

            modelBuilder.Entity<OrderDto>()
               .HasMany(o => o.Dishes)
               .WithOne(d => d.Order)
               .HasForeignKey(d => d.OrderId)
               .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
