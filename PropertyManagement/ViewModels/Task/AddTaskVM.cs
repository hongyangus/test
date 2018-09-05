using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System;

namespace PropertyManagement.ViewModels.Task
{
    public class AddTaskVM
    {
        public int TaskID { get; set; }
        public string Title { get; set; }
        public HtmlString  TaskDetailHtml { get; set; }
        public string TaskDetail { get; set; }
        public string UserName { get; set; }
        public int StatusID { get; set; }
        public string StatusName { get; set; }
        public int ContractorID { get; set; }
        public int AdminID { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public DateTime ClosedDate { get; set; }
        public double Hours { get; set; }
        public double Milage { get; set; }
        public double Material { get; set; }
        public double Labor { get; set; }
        public double TotalPayment { get; set; }
        public int UnitID { get; set; }
        public string Address { get; set; }
        public int ParentTaskID { get; set; }
        public int BankAccountID { get; set; }
        public int LinkedExpenseID { get; set; }
        public IEnumerable<SelectListItem> AllStatus { get; set; }
        public IEnumerable<SelectListItem> AllTenant { get; set; }
        public IEnumerable<SelectListItem> AllUnit { get; set; }
        public IEnumerable<SelectListItem> AllBankAccount { get; set; }

    }
}