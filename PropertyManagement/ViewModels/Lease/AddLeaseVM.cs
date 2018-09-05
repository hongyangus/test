using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PropertyManagement.Models;

namespace PropertyManagement.ViewModels.Tenant
{
    public class AddTenantVM
    {
        public int TenantID { get; set; }
        public int PropertyID { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Zip { get; set; }
        public double PurchasePrice { get; set; }
        public double PropertyTaxYearPayment { get; set; }
        public string PropertyTaxMailingAddress { get; set; }
        public DateTime PropertyTaxDueDate { get; set; }
        public int StatusID { get; set; }
        public string note { get; set; }
        public double InterestRate { get; set; }
        public double LoanAmount { get; set; }
        public string InsuranceCompany { get; set; }
        public double InsurancePremium { get; set; }
        public string InsurancePolicyNumber { get; set; }
        public DateTime InsuranceDueDate { get; set; }
        public string InsuranceBillMailingAddress { get; set; }
        public DateTime SoldDate { get; set; }
        public double amortization { get; set; }
        public double CurrentEstimateMarketValue { get; set; }
        public double ShareHoldPercentage { get; set; }
        public IEnumerable<SelectListItem> AllStatus { get; set; }
        public IEnumerable<SelectListItem> AllCompany { get; set; }
        public AddTenantVM()
        {
            PurchaseDate = DateTime.Now;
            PropertyTaxDueDate = DateTime.Now;
            InsuranceDueDate = DateTime.Now;
        }
    }
}