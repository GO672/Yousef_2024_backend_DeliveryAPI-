using Delivery_API.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Delivery_API.Models
{
    public class UserRegisterModel
    {
        public string FullName { get; set; }

        public string Password { get; set; }

        public string Email { get; set; }

        public string Address { get; set; }

        public DateTime? BirthDate { get; set; }

        public Gender Gender { get; set; }

        public string PhoneNumber { get; set; }
    }
}
