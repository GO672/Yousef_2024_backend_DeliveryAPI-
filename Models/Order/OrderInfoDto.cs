﻿using System;

namespace Delivery_API.Models
{
    public class OrderInfoDto
    {
        public Guid Id { get; set; }
        public DateTime DeliveryTime { get; set; }
        public DateTime OrderTime { get; set; }
        public OrderStatus Status { get; set; }
        public double Price { get; set; }
    }
}

