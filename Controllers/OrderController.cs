using Delivery_API.Data;
using Delivery_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Delivery_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly DBContext _context;

        public OrderController(DBContext context)
        {
            _context = context;
        }

        // Helper method to get the authenticated user's ID using email
        private async Task<string> GetUserIdByEmail()
        {
            // Extract email from the token claims
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(email))
            {
                return null;
            }

            // Query the database to find the user by their email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            return user?.Id.ToString(); // Return the user ID as a string, or null if not found
        }

        #region GET Requests

        // GET: api/order/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(Guid id)
        {
            try
            {
                // Get the user ID using email
                var userId = await GetUserIdByEmail();

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User is not authenticated." });
                }

                // Retrieve the order by ID and ensure it belongs to the authenticated user
                var order = await _context.Order
                    .Include(o => o.Dishes)
                    .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId); // Ensure order belongs to the user

                if (order == null)
                {
                    return NotFound(new { message = $"Order with ID {id} not found for the current user." });
                }

                var response = new
                {
                    Id = order.Id,
                    DeliveryTime = order.DeliveryTime,
                    OrderTime = order.OrderTime,
                    Status = order.Status,
                    Price = order.Price,
                    Dishes = order.Dishes.Select(dish => new
                    {
                        Id = dish.Id,
                        Name = dish.Name,
                        Price = dish.Price,
                        TotalPrice = dish.TotalPrice, // Use the computed property
                        Amount = dish.Amount,
                        Image = dish.Image
                    }),
                    Address = order.Address
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the order.", error = ex.Message });
            }
        }

        // GET: api/order
        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                // Get the user ID using email
                var userId = await GetUserIdByEmail();

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User is not authenticated." });
                }

                // Retrieve all orders for the authenticated user
                var orders = await _context.Order
                    .Where(o => o.UserId == userId) // Filter by UserId
                    .Select(o => new OrderInfoDto
                    {
                        Id = o.Id,
                        DeliveryTime = o.DeliveryTime,
                        OrderTime = o.OrderTime,
                        Status = o.Status,
                        Price = o.Price
                    })
                    .ToListAsync();

                if (!orders.Any())
                {
                    return NotFound(new { message = "No orders found." });
                }

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the orders.", error = ex.Message });
            }
        }

        #endregion

        #region POST Requests

        // POST: api/order
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateRequest orderRequest)
        {
            try
            {
                // Get the user ID using email
                var userId = await GetUserIdByEmail();

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User is not authenticated." });
                }

                // Validate the delivery time
                if (orderRequest.DeliveryTime <= DateTime.UtcNow.AddMinutes(60))
                {
                    return BadRequest(new
                    {
                        status = "Error",
                        message = "Invalid delivery time. Delivery time must be more than current datetime by 60 minutes"
                    });
                }

                // Retrieve all dishes in the basket for the current user
                var basketDishes = await _context.Basket
                    .Where(b => b.UserId == userId)
                    .ToListAsync();

                if (!basketDishes.Any())
                {
                    return BadRequest(new { message = "The basket is empty. Cannot create an order." });
                }

                // Calculate the total price for the order
                double totalPrice = basketDishes.Sum(d => d.TotalPrice);

                // Create the new order
                var newOrder = new OrderDto
                {
                    DeliveryTime = orderRequest.DeliveryTime,
                    OrderTime = DateTime.UtcNow,
                    Status = OrderStatus.InProcess,
                    Price = totalPrice,
                    Address = orderRequest.Address,
                    UserId = userId // Associate with the authenticated user
                };

                // Create the dish orders for the new order
                var dishOrders = basketDishes.Select(b => new DishOrder
                {
                    Name = b.Name,
                    Price = b.Price,
                    Amount = b.Amount,
                    Image = b.Image,
                    OrderId = newOrder.Id
                }).ToList();

                newOrder.Dishes = dishOrders;

                // Add the new order and dish orders to the database
                await _context.Order.AddAsync(newOrder);
                await _context.DishOrders.AddRangeAsync(dishOrders);

                // Remove the items from the basket after the order is created
                _context.Basket.RemoveRange(basketDishes);

                // Save the changes to the database
                await _context.SaveChangesAsync();

                return Ok(new { message = "Order created successfully.", orderId = newOrder.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the order.", error = ex.Message });
            }
        }

        // POST: api/order/{id}/status
        [HttpPost("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(Guid id)
        {
            try
            {
                // Find the order by ID
                var order = await _context.Order.FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound(new { message = $"Order with ID {id} not found." });
                }

                // Check if the current status is 'InProcess'
                if (order.Status != OrderStatus.InProcess)
                {
                    return BadRequest(new { message = "Order is not in 'InProcess' status and cannot be marked as 'Delivered'." });
                }

                // Update the status to 'Delivered'
                order.Status = OrderStatus.Delivered;

                // Save changes to the database
                _context.Order.Update(order);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Order status updated to 'Delivered'.",
                    orderId = order.Id,
                    newStatus = order.Status
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the order status.", error = ex.Message });
            }
        }

        #endregion
    }

    public class OrderCreateRequest
    {
        public DateTime DeliveryTime { get; set; }
        public string Address { get; set; }
    }
}
