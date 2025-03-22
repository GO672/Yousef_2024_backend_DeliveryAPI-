using Delivery_API.Models;
using Delivery_API.Models.Response;
using Delivery_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BasketController : ControllerBase
{
    private readonly IBasketService _basketService;

    public BasketController(IBasketService basketService)
    {
        _basketService = basketService;
    }

    // Helper method to get the user's ID
    private async Task<string> GetUserId()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            return null;
        }

        return await _basketService.GetUserId(email);
    }

    // GET: api/Basket
    [HttpGet]
    [SwaggerOperation(Summary = "Get user cart")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(DishBasketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Response), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBasket()
    {
        var userId = await GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var result = await _basketService.GetBasket(userId);
        return result;
    }

    // POST: api/Basket/dish/{dishId}
    [HttpPost("dish/{dishId}")]
    [SwaggerOperation(Summary = "Add dish to cart")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddToBasket(Guid dishId)
    {
        var userId = await GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var result = await _basketService.AddToBasket(dishId, userId);
        return result;
    }

    // DELETE: api/Basket/dish/{dishId}
    [HttpDelete("dish/{dishId}")]
    [Produces("application/json")]
    [SwaggerOperation(Summary = "Decrease the number of dishes in the cart (if increase = true), or remove the dish completely (increase = false)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ModifyBasket(Guid dishId, [FromQuery] bool increase = false)
    {
        var userId = await GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var result = await _basketService.ModifyBasket(dishId, userId, increase);
        return result;
    }
}
