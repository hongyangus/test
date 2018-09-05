using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PropertyManagement.Models
{
    public class Company
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public DateTime StartDate { get; set; }
        public string AgentFirstName { get; set; }
        public string AgentLastName { get; set; }
        public string CompanyCellPhone { get; set; }
        public string CompanyPhone { get; set; }
        public string Address { get; set; }
        public string EmailAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string EIN { get; set; }
        public string BankAccount { get; set; }
        public string RountingNo { get; set; }
        public int StatusID { get; set; }
        public string Status { get; set; }
        public int AdminID { get; set; }
        public string AdminName { get; set; }
        public virtual ICollection<Property> Properties { get; set; }
    }
}