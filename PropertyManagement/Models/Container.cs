using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PropertyManagement.Models
{
    public class ECommerceContainer
    {
        public int ContainerID;
        public string ContainerName;
        public int ContainerNumber;
        public DateTime ShippedDate;
        public DateTime EstimateArrivalDate;
        public DateTime ArrivalDate;
        public DateTime UnloadDate;
        public DateTime MarketDate;
        public string UnloadBy;
        public float UnloadTimePeriod;
        public string Notes;
    }
}