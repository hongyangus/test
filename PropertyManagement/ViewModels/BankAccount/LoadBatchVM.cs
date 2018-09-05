using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PropertyManagement.Models;

namespace PropertyManagement.ViewModels.BankAccount
{
    public class LoadBatchVM
    {
        public string BankAccountName;
        public int FinancialBankAccountID { get; set; }
        public IEnumerable<SelectListItem> AllBankAccount;
        public int FinancialAccountTypeID { get; set; }

        public IEnumerable<SelectListItem> AllAccountType;
        public List<OperationRecord> operations;
        public LoadBatchVM()
        {
        }
    }
}