using Delivery_API.Models;
using System.ComponentModel.DataAnnotations;

public class OrderDto
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public DateTime DeliveryTime { get; set; }

    [Required]
    public DateTime OrderTime { get; set; }

    [Required]
    public OrderStatus Status { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public double Price { get; set; }

    [Required]
    [MinLength(1)]
    public string Address { get; set; }

    // Link to user
    [Required]
    public string UserId { get; set; }

    // Navigation Property
    public List<DishOrder> Dishes { get; set; } = new List<DishOrder>();
}
