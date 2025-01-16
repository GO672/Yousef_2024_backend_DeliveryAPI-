using Delivery_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Delivery_API.Controllers
{
    [Route("api/dish")]
    [ApiController]
    
    [ApiExplorerSettings(GroupName = "Dish")]
    public class DishesController : ControllerBase
    {
        private readonly Data.DBContext _context;

        public DishesController(Data.DBContext context)
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


        [HttpPost("{id}/rating")]
        public async Task<IActionResult> AddDishRating(Guid id, [FromQuery] int ratingScore)
        {
            try
            {
                // Validate the rating score
                if (ratingScore < 1 || ratingScore > 10)
                {
                    return BadRequest(new { message = "Rating score must be between 1 and 10." });
                }

                // Get the authenticated user's ID
                var userId = await GetUserIdByEmail();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User is not authenticated." });
                }

                // Check if the dish exists
                var dish = await _context.Dish.FirstOrDefaultAsync(d => d.Id == id);
                if (dish == null)
                {
                    return NotFound(new { message = "Dish not found." });
                }

                // Check if the user has purchased the dish
                var hasPurchased = await _context.Order
                    .Include(o => o.Dishes)
                    .Where(o => o.UserId == userId)
                    .AnyAsync(o => o.Dishes.Any(d => d.Name == dish.Name));

                if (!hasPurchased)
                {
                    return BadRequest(new { message = "You can only rate dishes you have purchased." });
                }

                // Check if the user has rated this dish before
                var existingDishOrder = await _context.DishOrders
                    .FirstOrDefaultAsync(d => d.Order.UserId == userId && d.Name == dish.Name);

                if (existingDishOrder == null)
                {
                    // Check the current dish rating before assigning to InitialRating
                    Console.WriteLine($"Dish.Rating before setting InitialRating: {dish.Rating}");

                    var newDishOrder = new DishOrder
                    {
                        Id = Guid.NewGuid(),
                        Name = dish.Name,
                        Price = dish.Price,
                        Amount = 1,
                        Image = dish.Image,
                        OrderId = Guid.NewGuid(), // Replace with the user's actual order ID if available
                        Rating = ratingScore, // User's rating
                        InitialRating = dish.Rating // Record the dish's current rating as InitialRating
                    };

                    // Ensure InitialRating is being set correctly
                    Console.WriteLine($"NewDishOrder.InitialRating: {newDishOrder.InitialRating}");

                    // Update the dish's overall rating
                    dish.Rating = dish.Rating == 0
                        ? ratingScore // If dish had no ratings, use the first rating directly
                        : (dish.Rating + ratingScore) / 2.0; // Otherwise, calculate the new average

                    // Save to the database
                    await _context.DishOrders.AddAsync(newDishOrder);
                    _context.Dish.Update(dish);
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        message = "Rating added successfully.",
                        newRating = dish.Rating
                    });
                }

                else
                {
                    // Case 2: User has rated this dish before and is updating their rating
                    var previousRating = existingDishOrder.Rating ?? 0;

                    // Update the user's current rating
                    existingDishOrder.Rating = ratingScore;

                    // Recalculate the dish's average rating
                    var allRatings = await _context.DishOrders
                        .Where(d => d.Name == dish.Name && d.Rating != null)
                        .ToListAsync();

                    var totalRatings = allRatings.Count;
                    var totalRatingSum = allRatings.Sum(d => d.Rating ?? 0);

                    dish.Rating = totalRatings > 0
                        ? (totalRatingSum - previousRating + ratingScore) / totalRatings
                        : ratingScore;

                    // Save to the database
                    _context.DishOrders.Update(existingDishOrder);
                    _context.Dish.Update(dish);
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        message = "Rating updated successfully.",
                        newRating = dish.Rating
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while adding the rating.", error = ex.Message });
            }
        }





        // GET: api/dish/{id}/rating/check
        [HttpGet("{id}/rating/check")]
        public async Task<IActionResult> CheckDishRatingEligibility(Guid id)
        {
            try
            {
                // Get the authenticated user's ID
                var userId = await GetUserIdByEmail();

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User is not authenticated." });
                }

                // Check if the dish exists
                var dish = await _context.Dish.FirstOrDefaultAsync(d => d.Id == id);
                if (dish == null)
                {
                    return NotFound(new { message = "Dish not found." });
                }

                // Check if the user has purchased this dish
                var hasPurchased = await _context.Order
                    .Include(o => o.Dishes) // Include the dishes in the order
                    .Where(o => o.UserId == userId) // Only check orders made by the user
                    .AnyAsync(o => o.Dishes.Any(d => d.Name == dish.Name));

                if (!hasPurchased)
                {
                    return Ok(new
                    {
                        canRate = false,
                        message = "You have not purchased this dish and cannot give it a rating."
                    });
                }

                // The user is eligible to rate the dish
                return Ok(new
                {
                    canRate = true,
                    message = "You are eligible to give this dish a rating."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while checking rating eligibility.", error = ex.Message });
            }
        }

        // GET: api/dish
        [HttpGet]
        public IActionResult GetDishes(
            [FromQuery] DishCategory[] categories,  // Accepts an array of DishCategory enums
            [FromQuery] bool? vegetarian,           // Filter by vegetarian
            [FromQuery] DishSorting? sorting,       // Sort by specific criteria
            [FromQuery] int page = 1               // Page number
        )
        {
            var query = _context.Dish.AsQueryable();

            if (categories != null && categories.Length > 0)
            {
                query = query.Where(d => categories.Contains(d.Category));
            }

            if (vegetarian.HasValue)
            {
                query = query.Where(d => d.Vegetarian == vegetarian.Value);
            }

            query = sorting switch
            {
                DishSorting.NameDesc => query.OrderByDescending(d => d.Name),
                DishSorting.PriceAsc => query.OrderBy(d => d.Price),
                DishSorting.PriceDesc => query.OrderByDescending(d => d.Price),
                DishSorting.RatingAsc => query.OrderBy(d => d.Rating),
                DishSorting.RatingDesc => query.OrderByDescending(d => d.Rating),
                _ => query.OrderBy(d => d.Name)
            };

            int totalItems = query.Count();
            var dish = query
                .Skip((page - 1) * 5)
                .Take(5)
                .ToList();

            return Ok(new
            {
                dish,
                pagination = new
                {
                    size = 5,
                    count = (int)Math.Ceiling(totalItems / (double)5),
                    current = page
                }
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDishById(Guid id)
        {
            var dish = await _context.Dish.FindAsync(id);

            if (dish == null)
            {
                return NotFound();
            }

            return Ok(dish);
        }
    }


}
