using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PropertyManagement.Models
{
    public class BankAccount
    {
        public BankAccount()
        {
            PaymentDueDate = DateTime.Now;
            StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            FrozenDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        }

        public int FinancialAccountID { get; set; }
        public DateTime StartDate { get; set; }
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public string RoutingNumber { get; set; }
        public int AccountType { get; set; }
        public string AccountTypeName { get; set; }
        public int StatusID { get; set; }
        public string MailingAddress { get; set; }
        public DateTime PaymentDueDate { get; set; }
        public int AccountOwnerID { get; set; }
        public string AccountOwner { get; set; }
        public DateTime FrozenDateTime { get; set; }
        public string LinkWebsite { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int CompanyID { get; set; }
        public IEnumerable<SelectListItem> AllStatus { get; set; }
        public IEnumerable<SelectListItem> AllCompany { get; set; }
        public IEnumerable<SelectListItem> AllAccountType { get; set; }
        public IEnumerable<SelectListItem> AllUser { get; set; }

    }
}
