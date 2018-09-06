using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PropertyManagement.Models
{
    public class ECommerceContainer
    {
        public int ContainerID { get; set; }
        public string ContainerName { get; set; }
        public int ContainerNumber { get; set; }
        public DateTime ShippedDate { get; set; }
        public DateTime EstimateArrivalDate { get; set; }
        public DateTime ArrivalDate { get; set; }
        public DateTime UnloadDate { get; set; }
        public DateTime MarketDate { get; set; }
        public string UnloadBy { get; set; }
        public float UnloadTimePeriod { get; set; }
        public string Notes { get; set; }
    }
}