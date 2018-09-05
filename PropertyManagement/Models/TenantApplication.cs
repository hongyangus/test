using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PropertyManagement.Models
{
    public class TenantApplication
    {
        public User user { get; set; }
        public double firstIncome { get; set; }
        public double secondIncome { get; set; }
        public double thirdIncome { get; set; }
        public string currentHomeAddress { get; set; }
        public string mostSecondAddress { get; set; }
        public string currentLandlordName { get; set; }
        public string secondLandlordName { get; set; }
        public string currentLandlordPhone { get; set; }
        public string secondLandlordPhone { get; set; }
        public string bankName { get; set; }
        public string bankAccount { get; set; }
        public string driversLicense { get; set; }
        public string autobrand { get; set; }
        public string autoyear { get; set; }
        public string autoplate { get; set; }
    }
}