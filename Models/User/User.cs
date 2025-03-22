using Delivery_API.Models.Enums;
using System;

namespace Delivery_API.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string PasswordHash { get; set; }  // Store password hash
        public string Email { get; set; }
        public string Address { get; set; }
        public DateTime? BirthDate { get; set; }
        public Gender Gender { get; set; }
        public string PhoneNumber { get; set; }
    }
}

