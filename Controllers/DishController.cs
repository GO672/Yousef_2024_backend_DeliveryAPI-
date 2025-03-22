using Delivery_API.Models;
using Delivery_API.Models.Page;
using Delivery_API.Models.Response;
using Delivery_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Delivery_API.Controllers
{
    [ApiController]
    [Route("api/dish")]
    [ApiExplorerSettings(GroupName = "Dish")]
    public class DishesController : ControllerBase
    {
        private readonly IDishService _dishService;
        private readonly ILogger<DishesController> _logger;

        public DishesController(IDishService dishService, ILogger<DishesController> logger)
        {
            _dishService = dishService;
            _logger = logger;
        }

        private async Task<string> GetUserIdByEmail()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            return await _dishService.GetUserIdByEmail(email);
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get a list of dishes (menu)")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(DishPagedListDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDishes(
            [FromQuery] DishCategory[] categories,
            [FromQuery] bool? vegetarian,
            [FromQuery] DishSorting? sorting,
            [FromQuery] int page = 1)
        {
            if (page < 1 || page > 5)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid page number",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var result = await _dishService.GetDishes(categories, vegetarian, sorting, page);
            return result;
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get information about a specific dish")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(Dish), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDishById(Guid id)
        {
            
            if (await _dishService.GetDishById(id) == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Dish not found",
                    Status = StatusCodes.Status404NotFound
                });
            }
            return await _dishService.GetDishById(id);
        }

        [Authorize]
        [HttpGet("{id}/rating/check")]
        [Produces("application/json")]
        [SwaggerOperation(Summary = "Checks if user is able to set rating of the dish")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CheckDishRatingEligibility(Guid id)
        {
            var userId = await GetUserIdByEmail();
            return await _dishService.CheckDishRatingEligibility(id, userId);
        }


        [Authorize]
        [HttpPost("{id}/rating")]
        [SwaggerOperation(Summary = "Set a rating for a dish")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddDishRating(Guid id, [FromQuery] int ratingScore)
        {
            if (ratingScore < 1 || ratingScore > 10)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid rating score",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var userId = await GetUserIdByEmail();
            var result = await _dishService.AddDishRating(id, ratingScore, userId);
            return result;
        }
    }
}