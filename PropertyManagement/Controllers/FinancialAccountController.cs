using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Configuration;
using System.Data.SqlClient;
using PropertyManagement.Models;
using System.Text;
using System.Data;
using System.IO;
using LumenWorks.Framework.IO.Csv;
using PropertyManagement.ViewModels.BankAccount;
using PropertyManagement.ViewModels.Property;

namespace PropertyManagement.Controllers
{
    [Authorize]
    public class FinancialAccountController : BaseController
    {
        //private string reporttitle = "Financial Account Statement"; // Specify the report title here
        //private string errorMessage = ""; // Specify the error message

        [AllowAnonymous]
        public ActionResult Index()
        {
            // Enable security to redirect to login page if user is not logged in or we are not running in the VS IDE
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }

            List<BankAccount> att = BankAccountManager.GetAllBankAccounts(Helpers .Helpers .GetUserManagedCompanyString (Session["UserID"].ToString ()));
            return View(att);
        }
        

        [AllowAnonymous]
        public ActionResult Add()
        {
            // add new user
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Create new financial account";
            // Get the users from the DB for the drop-down listbox

            BankAccount model = new BankAccount();
            
            model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);
            model.AllCompany = GetSelectListItems((short)Helpers.Helpers.ListType.company);
            model.AllUser = GetSelectListItems((short)Helpers.Helpers.ListType.allUser);
            model.AllAccountType = GetSelectListItems((short)Helpers.Helpers.ListType.allAccountType);

            return View(model);
        }


        [HttpPost]
        [AllowAnonymous]
        public ActionResult Add(BankAccount model)
        {
            BankAccountManager.Add(model, model.CompanyID);
            return RedirectToAction("Index");
        }



        [AllowAnonymous]
        public ActionResult Edit(int id)
        {
            try
            {
                if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
                ViewBag.ReportTitle = "Edit Financial Account";

                var model =  BankAccountManager .GetBankAccountByID(id);

                model.AllUser= GetSelectListItems((short)Helpers.Helpers.ListType.allUser);
                model.AllAccountType = GetSelectListItems((short)Helpers.Helpers.ListType.allAccountType);
                model.AllCompany = GetSelectListItems((short)Helpers.Helpers.ListType.company );
                model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);

                return View(model);
            }
            catch (Exception ex)
            {
                LogException(ex.Message);
                return View();
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Edit(BankAccount model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Edit Financial Account Record";

            BankAccountManager.Edit(model, model.CompanyID );
            return RedirectToAction("Index");
        }

    }

}
