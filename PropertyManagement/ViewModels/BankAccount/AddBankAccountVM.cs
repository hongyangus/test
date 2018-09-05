using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PropertyManagement.Models;

namespace PropertyManagement.ViewModels.BankAccount
{
    public class AddBankAccountVM
    {
        public PropertyManagement.Models.BankAccount bankAccount { get; set; }
        public int CompanyID { get; set; }
        public IEnumerable<SelectListItem> AllStatus { get; set; }
        public IEnumerable<SelectListItem> AllCompany { get; set; }
        public IEnumerable<SelectListItem> AllAccountType { get; set; }
        public IEnumerable<SelectListItem> AllUser { get; set; }

        public AddBankAccountVM ()
        {
            bankAccount = new Models.BankAccount();
        }
    }
}