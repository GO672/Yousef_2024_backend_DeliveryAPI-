using System;
using System.ComponentModel.DataAnnotations;

public class DishBasketDto
{
    [Key]
    public Guid Id { get; set; } // Unique identifier for the dish

    [Required]
    [MinLength(1)]
    public string Name { get; set; } // Name of the dish

    [Required]
    [Range(0.01, double.MaxValue)]
    public double Price { get; set; } // Price per unit of the dish

    [Required]
    [Range(0.01, double.MaxValue)]
    public double TotalPrice { get; set; } // Total cost (price × amount)

    [Required]
    [Range(1, int.MaxValue)]
    public int Amount { get; set; } // Quantity of the dish

    public string Image { get; set; } // Optional URL for the dish image

    [Required]
    public string UserId { get; set; } // ID of the user to whom the basket item belongs
}
