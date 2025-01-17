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
    public class BasketController : ControllerBase
    {
        private readonly DBContext _context;

        public BasketController(DBContext context)
        {
            _context = context;
        }

        // Helper method to get the user's ID
        private async Task<string> GetUserId()
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

        // GET: api/Basket
        [HttpGet]
        public async Task<IActionResult> GetBasket()
        {
            try
            {
                var userId = await GetUserId();

                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { message = "User ID is missing or invalid." });
                }

                // Retrieve basket items for the current user, excluding userId
                var basketItems = await _context.Basket
                    .Where(b => b.UserId == userId)
                    .Select(b => new
                    {
                        b.Id,
                        b.Name,
                        b.Price,
                        b.TotalPrice,
                        b.Amount,
                        b.Image
                    })
                    .ToListAsync();

                return Ok(basketItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the basket.", error = ex.Message });
            }
        }


        // POST: api/Basket/dish/{dishId}
        [HttpPost("dish/{dishId}")]
        public async Task<IActionResult> AddToBasket(Guid dishId)
        {
            try
            {
                var userId = await GetUserId();

                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { message = "User ID is missing or invalid." });
                }

                // Find the dish by ID
                var dish = await _context.Dish.FirstOrDefaultAsync(d => d.Id == dishId);

                if (dish == null)
                {
                    return NotFound(new { message = $"Dish with ID {dishId} not found." });
                }

                // Check if the dish is already in the basket for the current user
                var basketItem = await _context.Basket
                    .FirstOrDefaultAsync(b => b.UserId == userId && b.Name == dish.Name);

                if (basketItem != null)
                {
                    // If the dish is already in the basket, increment the amount
                    basketItem.Amount++;
                    basketItem.TotalPrice = basketItem.Price * basketItem.Amount;
                    _context.Basket.Update(basketItem);
                }
                else
                {
                    // Generate a new unique ID for the basket item
                    var newBasketItem = new DishBasketDto
                    {
                        Id = Guid.NewGuid(), // Use a new unique ID for the basket item
                        Name = dish.Name,
                        Price = dish.Price,
                        TotalPrice = dish.Price, // Initially, the total price is just the price for one item
                        Amount = 1, // Amount starts at 1 when added for the first time
                        Image = dish.Image,
                        UserId = userId
                    };

                    // Add the new basket item
                    await _context.Basket.AddAsync(newBasketItem);
                }

                // Save changes to the database
                await _context.SaveChangesAsync();

                return Ok(new { message = "Dish added to the basket successfully." });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while adding the dish to the basket.",
                    error = dbEx.InnerException?.Message ?? dbEx.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", error = ex.Message });
            }
        }



        [HttpDelete("dish/{dishId}")]
        public async Task<IActionResult> ModifyBasket(Guid dishId, [FromQuery] bool increase = false)
        {
            try
            {
                // Get the user ID from the authentication context
                var userId = await GetUserId();

                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { message = "User ID is missing or invalid." });
                }

                // Find the dish by ID
                var dish = await _context.Dish.FirstOrDefaultAsync(d => d.Id == dishId);

                if (dish == null)
                {
                    return NotFound(new { message = $"Dish with ID {dishId} not found." });
                }

                // Check if the dish exists in the basket for the current user
                var basketItem = await _context.Basket
                    .FirstOrDefaultAsync(b => b.UserId == userId && b.Name == dish.Name);

                if (basketItem == null)
                {
                    return NotFound(new { message = $"Dish with ID {dishId} not found in the basket for the current user." });
                }

                // Handle the case when `increase` is true, meaning we decrease the quantity
                if (increase)
                {
                    if (basketItem.Amount > 1)
                    {
                        // Decrease the quantity
                        basketItem.Amount--;
                        basketItem.TotalPrice = basketItem.Price * basketItem.Amount;
                        _context.Basket.Update(basketItem);
                    }
                    else
                    {
                        // If the amount is 1, remove the item from the basket completely
                        _context.Basket.Remove(basketItem);
                    }
                }
                else
                {
                    // If `increase` is false, completely remove the dish from the basket
                    _context.Basket.Remove(basketItem);
                }

                // Save the changes to the database
                await _context.SaveChangesAsync();

                return Ok(new { message = "Dish updated or removed from the basket successfully." });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while updating the basket.",
                    error = dbEx.InnerException?.Message ?? dbEx.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", error = ex.Message });
            }
        }



    }
}
