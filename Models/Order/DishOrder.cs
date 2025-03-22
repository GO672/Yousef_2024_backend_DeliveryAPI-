using System;
using System.ComponentModel.DataAnnotations;

public class DishOrder
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid(); // Unique identifier for each dish in an order

    [Required]
    [MinLength(1)]
    public string Name { get; set; } // Name of the dish

    [Required]
    [Range(0.01, double.MaxValue)]
    public double Price { get; set; } // Price per unit of the dish

    [Required]
    [Range(1, int.MaxValue)]
    public int Amount { get; set; } // Quantity of the dish

    public string Image { get; set; } // Optional URL for the dish image

    // Foreign Key for the associated order
    [Required]
    public Guid OrderId { get; set; }

    // Navigation Property
    public OrderDto Order { get; set; }

    // Computed property to calculate total price
    public double TotalPrice => Price * Amount; // Automatically calculate total price

    // New Rating fields to store user's rating and their initial rating
    public double? Rating { get; set; } // Rating for the dish (nullable to allow no rating initially)

    // Track user's initial rating
    public double? InitialRating { get; set; } // To track the first rating
}

