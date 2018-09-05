using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PropertyManagement.Models
{
    public class OperationRecord
    {
        public OperationRecord()
        {
            CompleteDate = DateTime.Now;
            DueDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        }
        public int ID { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime CompleteDate { get; set; }
        public string PaidBy { get; set; }
        public int ContractorID { get; set; }
        public int UploadBy { get; set; }
        public int UnitID { get; set; }
        public int PaidTo { get; set; }
        public string PaidToPerson { get; set; }
        public string Memo { get; set; }
        public string BankTracking { get; set; }
        public double Payment { get; set; }
        public double Deposit { get; set; }
        public string BankAccountName { get; set; }
        public string FinancialBankAccountID { get; set; }
        public string TransferedFinancialBankAccountID { get; set; }
        public int LinkedExpenseID { get; set; }
        public string Address { get; set; }
        public double DueAmount { get; set; }
        public double Balance { get; set; }
        public string RentMonth { get; set; }
        public bool IsCredit { get; set; }
        public short StatusID { get; set; }
        public string StatusName { get; set; }
        public string InvoiceLink { get; set; }
        public short CategoryID { get; set; }
        public string CategoryName { get; set; }
        public bool IsEmailReceipt { get; set; }
        public bool IsSecurityDeposit { get; set; }
        public int TenantID { get; set; }
        public string LinkedRentID { get; set; }
        public IEnumerable<SelectListItem> AllUnits { get; set; }
        public IEnumerable<SelectListItem> AllBankAccount { get; set; }
        public IEnumerable<SelectListItem> AllTenant { get; set; }
        public IEnumerable<SelectListItem> AllCategory { get; set; }
        public IEnumerable<SelectListItem> AllStatus { get; set; }
        public int[] SelectedUnitIDs { get; set; }
        public string SendEmailAddress { get; set; }

    }
}