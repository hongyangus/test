using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PropertyManagement.Models
{
    public class Tenant
    {
        public Tenant()
        {
            StartDate = DateTime.Now;
            SecurityDepositPaidDate = DateTime.Now;
        }
        public int TenantID { get; set; }
        public int UserID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int UnitId { get; set; }
        public string Address { get; set; }
        public int StatusID { get; set; }
        public string StatusName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime SecurityDepositPaidDate { get; set; }
        public int LeaseTerm { get; set; }
        public double MonthlyPayment { get; set; }
        public double SecurityDeposit { get; set; }
        public double PaidSecurityDeposit { get; set; }
        public double Balance { get; set; }
        public string Note { get; set; }
        public bool IsEmailReceipt { get; set; }
        public string FinancialBankAccountID { get; set; }
        public IEnumerable<SelectListItem> AllTenant { get; set; }
        public string LinkedRentID { get; set; }
        public IEnumerable<SelectListItem> AllUnits { get; set; }
        public IEnumerable<SelectListItem> AllBankAccount { get; set; }
        public IEnumerable<SelectListItem> AllStatus { get; set; }
        public string SendEmailAddress { get; set; }
    }
}