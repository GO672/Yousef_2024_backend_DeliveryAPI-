using Delivery_API.Data;
using Delivery_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;

namespace Delivery_API.Controllers
{
    [ApiController]
    [Route("api/account")]
    public class UsersController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly IConfiguration _configuration;

        public UsersController(DBContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Register endpoint
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterModel model)
        {
            // Step 1: Validate Phone Number
            if (!IsValidPhoneNumber(model.PhoneNumber))
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "One or more validation errors occurred.",
                    status = 400,
                    traceId = HttpContext.TraceIdentifier,
                    errors = new
                    {
                        PhoneNumber = new[] { "The PhoneNumber field is not a valid phone number." }
                    }
                });
            }

            // Step 2: Validate Email Format
            if (!IsValidEmail(model.Email))
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "One or more validation errors occurred.",
                    status = 400,
                    traceId = HttpContext.TraceIdentifier,
                    errors = new
                    {
                        Email = new[] { "The Email field is not a valid email address." }
                    }
                });
            }

            // Step 3: Validate Password
            if (!PasswordContainsDigit(model.Password))
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "One or more validation errors occurred.",
                    status = 400,
                    traceId = HttpContext.TraceIdentifier,
                    errors = new
                    {
                        Password = new[] { "Password requires at least one digit" }
                    }
                });
            }

            // Step 4: Check if Email already exists
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "One or more validation errors occurred.",
                    status = 400,
                    traceId = HttpContext.TraceIdentifier,
                    errors = new
                    {
                        Email = new[] { "Email already exists." }
                    }
                });
            }

            // Step 5: If all validations pass, proceed with registration
            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = model.FullName,
                PasswordHash = HashPassword(model.Password),
                Email = model.Email,
                Address = model.Address,
                BirthDate = model.BirthDate,
                Gender = model.Gender,
                PhoneNumber = model.PhoneNumber
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }


        // Login endpoint
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCredentials credentials)
        {
            // Step 1: Validate the provided credentials
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "One or more validation errors occurred.",
                    status = 400,
                    traceId = HttpContext.TraceIdentifier,
                    errors = new
                    {
                        Email = new[] { "Email is required and must be valid." },
                        Password = new[] { "Password is required." }
                    }
                });
            }

            // Step 2: Check if the user exists and the password matches
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == credentials.Email);

            if (user == null)
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "One or more validation errors occurred.",
                    status = 400,
                    traceId = HttpContext.TraceIdentifier,
                    errors = new
                    {
                        Email = new[] { "Invalid email or password." }
                    }
                });
            }

            // Step 3: Validate the password (assuming it was hashed during registration)
            var hashedPassword = HashPassword(credentials.Password);
            if (hashedPassword != user.PasswordHash)
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "One or more validation errors occurred.",
                    status = 400,
                    traceId = HttpContext.TraceIdentifier,
                    errors = new
                    {
                        Password = new[] { "Invalid email or password." }
                    }
                });
            }

            // Step 4: Generate JWT token
            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        // Logout endpoint
        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(new
            {
                status = (string)null,
                message = "Logged Out"
            });
        }

        // Add this to the UsersController
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetUser()
        {
            // Step 1: Extract the email claim from the token
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (email == null)
            {
                return Unauthorized(new
                {
                    type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                    title = "Unauthorized",
                    status = 401,
                    traceId = HttpContext.TraceIdentifier
                });
            }

            // Step 2: Fetch the user by email from the database
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            // Step 3: If the user doesn't exist, return NotFound
            if (user == null)
            {
                return NotFound(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    title = "User not found.",
                    status = 404,
                    traceId = HttpContext.TraceIdentifier
                });
            }

            // Step 4: Map the user data to the response format
            var response = new
            {
                fullName = user.FullName,
                birthDate = user.BirthDate,
                gender = user.Gender,
                address = user.Address,
                email = user.Email,
                phoneNumber = user.PhoneNumber,
                id = user.Id
            };

            // Step 5: Return the user data
            return Ok(response);
        }




        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateUser([FromBody] UserEditModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "One or more validation errors occurred.",
                    status = 400,
                    traceId = HttpContext.TraceIdentifier,
                    errors = ModelState.SelectMany(x => x.Value.Errors)
                                       .Select(error => error.ErrorMessage)
                });
            }

            // Retrieve the user's email from the JWT token
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(emailClaim))
            {
                return Unauthorized(new
                {
                    type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                    title = "Unauthorized",
                    status = 401,
                    traceId = HttpContext.TraceIdentifier,
                    errors = new { Message = "User is not authorized." }
                });
            }

            // Find the user in the database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailClaim);

            if (user == null)
            {
                return NotFound(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    title = "Not Found",
                    status = 404,
                    traceId = HttpContext.TraceIdentifier,
                    errors = new { Message = "User not found." }
                });
            }

            // Update user details
            user.FullName = model.FullName;
            user.BirthDate = model.BirthDate;
            user.Gender = model.Gender;
            user.Address = model.Address;
            user.PhoneNumber = model.PhoneNumber;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User details updated successfully.",
                user = new
                {
                    user.FullName,
                    user.BirthDate,
                    user.Gender,
                    user.Address,
                    user.PhoneNumber
                }
            });
        }




        // Helper method to check if phone number is valid
        private bool IsValidPhoneNumber(string phoneNumber)
        {
            var phoneRegex = @"^(\+)?([\d\s\-\(\)]*)$"; // Example: valid phone numbers could be like 1234567890
            return Regex.IsMatch(phoneNumber, phoneRegex);
        }

        // Helper method to check if the password contains at least one digit
        private bool PasswordContainsDigit(string password)
        {
            return password.Any(char.IsDigit); // Checks if the password contains at least one digit
        }

        // Helper method to hash password
        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        // Helper method to generate JWT token
        private string GenerateJwtToken(User user)
        {
            var key = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key), "Jwt:Key cannot be null or empty.");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenKey = Encoding.UTF8.GetBytes(key);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email)
        }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = _configuration["Jwt:Issuer"], // Ensure this matches "MyAppIssuer"
                Audience = _configuration["Jwt:Audience"], // Ensure this matches "MyAppAudience"
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
