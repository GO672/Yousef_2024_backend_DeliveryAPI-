using Delivery_API.Models;
using Delivery_API.Models.Response;
using Delivery_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Delivery_API.Controllers
{
    [ApiController]
    [Route("api/account")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        [SwaggerOperation(Summary = "Register new user")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(TokenRespone), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] UserRegisterModel model)
        {
            return await _userService.Register(model);
        }

        [HttpPost("login")]
        [SwaggerOperation(Summary = "Log in to the system")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(TokenRespone), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginCredentials credentials)
        {
            return await _userService.Login(credentials);
        }


        [Authorize]
        [HttpPost("logout")]
        [SwaggerOperation(Summary = "Log out system user")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status500InternalServerError)]
        public IActionResult Logout()
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new Response { Status = "400", Message = "Authorization token is required." });
            }

            _userService.Logout(token);
            return Ok(new Response { Status = "200", Message = "User logged out successfully." });
        }

        [Authorize]
        [HttpGet("profile")]
        [SwaggerOperation(Summary = "Get user profile")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUser()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new Response { Status = "401", Message = "Unauthorized access." });
            }
            return await _userService.GetUser(email);
        }

        [Authorize]
        [HttpPut("profile")]
        [SwaggerOperation(Summary = "Edit user Profile")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUser([FromBody] UserEditModel model)
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new Response { Status = "401", Message = "Unauthorized access." });
            }
            return await _userService.UpdateUser(model, email);
        }
    }
}
