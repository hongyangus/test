using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using CCHMC.Radiology.Library.Authentication;
using CCHMC.AD.Library.Extensions;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using PropertyManagement.Models;
using PropertyManagement.ViewModels;
using PropertyManagement.ViewModels.User;
using System.Data;
using System.Text;
using PropertyManagement.Helpers;
using MySql.Data.MySqlClient;
using Dapper;

namespace PropertyManagement.Controllers
{
    [Authorize]
    public class ECommerceContainerController : BaseController
    {
        //
        // GET: /ManageUser/

        private string reporttitle = "Manage Container";

        [AllowAnonymous]
        public ActionResult Index()
        {
            //var errMsg = TempData["ErrorMessage"];

            // Enable security to redirect to login page if user is not logged in or we are not running in the VS IDE
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            // Save the report title to the ViewBag
            ViewBag.ReportTitle = reporttitle;

            var companies = GetList((short)Helpers.Helpers.ListType.company);
            ViewBag.companies = new MultiSelectList(companies, "id", "description");

            var properties = GetList((short)Helpers.Helpers.ListType.property);
            ViewBag.properties = new MultiSelectList(properties, "id", "description");

            var units = GetList((short)Helpers.Helpers.ListType.unit);
            ViewBag.units = new MultiSelectList(units, "id", "description");

            var bankAccounts = GetList((short)Helpers.Helpers.ListType.bankaccount);
            ViewBag.bankAccounts = new MultiSelectList(bankAccounts, "id", "description");

            var statusList = GetList((short)Helpers.Helpers.ListType.allStatus);
            ViewBag.statusList = new MultiSelectList(statusList, "id", "description");

            var categoryList = GetList((short)Helpers.Helpers.ListType.allExpenseCategory);
            ViewBag.categories = new MultiSelectList(categoryList, "id", "description");

            var contractors = GetList((short)Helpers.Helpers.ListType.allUser);
            ViewBag.contractors = new MultiSelectList(contractors, "id", "description");


            //setup default value of the start date and end date
            if (Session["startDate"] == null)
            {
                DateTime oneMonth = DateTime.Now.AddMonths(-1);
                Session["startDate"] = new DateTime(oneMonth.Year, oneMonth.Month, 1).ToString("MM/dd/yyyy");
                Session["endDate"] = DateTime.Now.ToString("MM/dd/yyyy");
            }

            return View();
        }

        [AllowAnonymous]
        public PartialViewResult ReportView(string startDate, string endDate, string[] companyIDs, string[] propertyIDs, string[] unitIDs, string[] bankAccountIDs, string[] statusIDs, string[] contractorIDs, string[] categoryIDs, string expense)
        {
            Session["startDate"] = startDate;
            Session["endDate"] = endDate;
            DateTime start = DateTime.Parse(startDate);
            DateTime end = DateTime.Parse(endDate);

            string sqlSelect = "SELECT * FROM tblcontainer WHERE ShippedDate>=@StartDate AND ShippedDate>=@EndDate";
            DynamicParameters dp = new DynamicParameters();
            dp.Add("@StartDate", startDate);
            dp.Add("@EndDate", endDate);
            List<ECommerceContainer> constainers = DBHelper<ECommerceContainer>.QueryMySQL(sqlSelect, dp);

            //List<OperationRecord> result = OperationRecordManager.GetExpense(startDate, endDate, companyIDs, propertyIDs, unitIDs, bankAccountIDs, statusIDs, contractorIDs, categoryIDs, expense, (int)Session["UserID"]);
            //bool isStartNull = start.Equals(DateTime.MinValue);
            //bool isEndNull = end.Equals(DateTime.MinValue);
            //ViewBag.TableCaption = reporttitle + " Expense: ";
            //if (!start.Equals(DateTime.MinValue))
            //{
            //    ViewBag.TableCaption += " fromt " + start.ToString("g");
            //}
            //if (!end.Equals(DateTime.MinValue))
            //{
            //    ViewBag.TableCaption += " thru " + end.ToString("g");
            //}

            return PartialView("ReportView", constainers);
        }


        [HttpGet]
        [AllowAnonymous]
        public ActionResult Add()
        {
            // add new user
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Create new container";
            
            var model = new ECommerceContainer();
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Add(ECommerceContainer model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Add new expense";
            AddECommerceContainer(model);
            return RedirectToAction("Index");
        }

        public void AddECommerceContainer(ECommerceContainer model)
        {
            string sqlInsert = "INSERT tblcontainer (ContainerName, ContainerNumber, ShippedDate, EstimateArrivalDate, ArrivalDate, UnloadDate, MarketDate, UnloadBy, UnloadTimePeriod, Notes) "
            + "VALUE (@ContainerName, @ContainerNumber, @ShippedDate, @EstimateArrivalDate, @ArrivalDate, @UnloadDate, @MarketDate, @UnloadBy, @UnloadTimePeriod, @Notes)";
            using (MySqlConnection connection = new MySqlConnection(Helpers.Helpers.GetERPConnectionString()))
            {
                try
                {
                    int result = connection.Execute(sqlInsert, model);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    connection.Close();
                }

            }
        }

        [AllowAnonymous]
        public ActionResult Edit(int id)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Edit Container";

            var model = OperationRecordManager.GetExpenseByID(id);

            model.AllUnits = GetSelectListItems((short)Helpers.Helpers.ListType.unit);
            model.AllBankAccount = GetSelectListItems((short)Helpers.Helpers.ListType.bankaccount);
            model.AllTenant = GetSelectListItems((short)Helpers.Helpers.ListType.allUser);
            model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);
            model.AllCategory = GetSelectListItems((short)Helpers.Helpers.ListType.allExpenseCategory);

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Edit(OperationRecord model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Edit Expense";
            if (String.IsNullOrEmpty(model.FinancialBankAccountID))
            {
                model.FinancialBankAccountID = "0";
            }
            OperationRecordManager.Edit(model);

            //send email to end user

            return RedirectToAction("Index");
        }


        [AllowAnonymous]
        public ActionResult Reimburse(int id)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Reimburse Expense";

            OperationRecord model = OperationRecordManager.GetExpenseByID(id);

            model.AllUnits = GetSelectListItems((short)Helpers.Helpers.ListType.unit);
            model.AllBankAccount = GetSelectListItems((short)Helpers.Helpers.ListType.bankaccount);
            model.AllTenant = GetSelectListItems((short)Helpers.Helpers.ListType.allUser);
            model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);
            model.AllCategory = GetSelectListItems((short)Helpers.Helpers.ListType.allExpenseCategory);

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Reimburse(OperationRecord model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            if (String.IsNullOrEmpty(model.TransferedFinancialBankAccountID))
            {
                return ExecutionError("Please select either Tenant or Unit");
            }
            model.UploadBy = Int32.Parse(Session["UserID"].ToString());
            OperationRecordManager.Reimburse(model);

            //send email to end user
            if (model.IsEmailReceipt)
            {
                Email.EmailPayment(model.ID, model.ContractorID, model.UnitID, model.CompleteDate, model.FinancialBankAccountID, model.DueAmount, model.Payment, (int)Helpers.Helpers.EmailType.Invoice);
            }

            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public ActionResult Remind(int id)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            var record = OperationRecordManager.GetExpenseByID(id);
            Email.EmailInvoice(record.ID);
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public ActionResult Delete(int id)
        {
            // Enable security to redirect to login page if user is not logged in or we are not running in the VS IDE
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            OperationRecordManager.Delete(id);
            return RedirectToAction("Index");
        }

    }
    
}

