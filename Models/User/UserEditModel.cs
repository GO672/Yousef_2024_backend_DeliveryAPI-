using Delivery_API.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Delivery_API.Models
{
    public class UserEditModel
    {
        [Required]
        [MinLength(1)]
        public string FullName { get; set; }

        public Gender Gender { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? BirthDate { get; set; }

        [DataType(DataType.Text)]
        public string Address { get; set; }

        [DataType(DataType.PhoneNumber)]
        public string PhoneNumber { get; set; }
    }
}
