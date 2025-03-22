using Delivery_API.Models;
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
    [Route("api/order")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "Order")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        private async Task<string> GetUserIdByEmail()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            return await _orderService.GetUserIdByEmail(email);
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get information about a specific order")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOrder(Guid id)
        {
            var userId = await GetUserIdByEmail();
            if (userId == null)
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "User is not authenticated.",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            var order = await _orderService.GetOrder(id, userId);
            if (order is NotFoundObjectResult)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Order Not Found",
                    Detail = $"Order with ID {id} not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return order;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get a list of orders")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(OrderInfoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllOrders()
        {
            var userId = await GetUserIdByEmail();
            if (userId == null)
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            var orders = await _orderService.GetAllOrders(userId);
            if (orders is NotFoundObjectResult)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "No Orders Found",
                    Detail = "No orders found for the current user.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return orders;
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Create an order from dishes in the basket")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateRequest orderRequest)
        {
            var userId = await GetUserIdByEmail();
            if (userId == null)
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            if (orderRequest.DeliveryTime <= DateTime.UtcNow.AddMinutes(60))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Delivery Time",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            return await _orderService.CreateOrder(orderRequest, userId);
        }

        [HttpPost("{id}/status")]
        [SwaggerOperation(Summary = "Confirm order delivery")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateOrderStatus(Guid id)
        {
            var result = await _orderService.UpdateOrderStatus(id);
            if (result is NotFoundObjectResult)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Order Not Found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            if (result is BadRequestObjectResult)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Order Status",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            return result;
        }
    }
}
