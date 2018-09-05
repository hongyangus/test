using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.Data.SqlClient;
using System.Data.Odbc;
using System.Text;
using PropertyManagement.Models;
using System.Data;

namespace PropertyManagement.Controllers
{
    [Authorize]
    public class ExpenseController :BaseController
    {
        private string reporttitle = "Expense"; // Specify the report title here

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
            Session["selectedCompanyIDs"] = companyIDs;
            Session["selectedPropertyIDs"] = propertyIDs;
            Session["selectedUnitIDs"] = unitIDs;
            Session["selectedAccountIDs"] = bankAccountIDs;
            Session["selectedStatusIDs"] = statusIDs;
            Session["selectedContractorIDs"] = contractorIDs;
            Session["selectedCategoryIDs"] = categoryIDs;
            Session["expenseValue"] = expense;
            DateTime start = DateTime.Parse (startDate);
            DateTime end = DateTime.Parse(endDate);

            List<OperationRecord> result = OperationRecordManager.GetExpense(startDate, endDate, companyIDs, propertyIDs, unitIDs, bankAccountIDs, statusIDs, contractorIDs, categoryIDs,  expense, (int)Session["UserID"]);
            bool isStartNull = start.Equals(DateTime.MinValue);
            bool isEndNull = end.Equals(DateTime.MinValue);
            ViewBag.TableCaption = reporttitle + " Expense: ";
            if (!start.Equals(DateTime.MinValue))
            {
                ViewBag.TableCaption += " fromt " + start.ToString("g");
            }
            if (!end.Equals(DateTime.MinValue))
            {
                ViewBag.TableCaption += " thru " + end.ToString("g");
            }

            ViewBag.TotalPayment = 0;
            ViewBag.TotalDeposit = 0;
            ViewBag.TotalBalace = 0;
            return PartialView("ReportView", result);
        }


        [HttpGet]
        [AllowAnonymous]
        public ActionResult Add()
        {
            // add new user
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Create new expense";
            // Get the users from the DB for the drop-down listbox
            var allUsers = GetList((short)Helpers.Helpers.ListType.allUser);
            //ViewBag.AllTenants = new MultiSelectList(allUsers, "id", "description");

            var allBankAccount = GetList((short)Helpers.Helpers.ListType.bankaccount);
            // ViewBag.bankAccounts = new MultiSelectList(allBankAccount, "id", "description");


            var allUnits = GetList((short)Helpers.Helpers.ListType.unit);
            // ViewBag.allUnits = new MultiSelectList(allUnits, "id", "description");

            var units = GetList((short)Helpers.Helpers.ListType.unit);
            ViewBag.units = new MultiSelectList(units, "id", "description");

            //string[] unitIDs = new string[1];
            //Session["selectedUnitIDs"] = unitIDs;

            OperationRecord model = new OperationRecord();
            model.StatusID = 3;

            model.AllTenant = GetSelectListItems((short)Helpers.Helpers.ListType.allUser);
            model.AllUnits = GetSelectListItems((short)Helpers.Helpers.ListType.unit);
            model.AllBankAccount = GetSelectListItems((short)Helpers.Helpers.ListType.bankaccount);
            model.AllCategory = GetSelectListItems((short)Helpers.Helpers.ListType.allExpenseCategory);
            model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Add(OperationRecord model, string actionName)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Add new expense";

            SqlConnection sqlConn = new SqlConnection(ConfigurationManager.ConnectionStrings["SunriseConnectionString"].ConnectionString);
            SqlCommand cmd = sqlConn.CreateCommand();
            DataTable dtSearchResult = new DataTable();
            SqlDataAdapter daSearchResult = new SqlDataAdapter();

            try
            {
                sqlConn.Open();
                if (model.CompleteDate ==null)
                {
                    model.CompleteDate = model.DueDate;
                }
                int invoiceID = 0;
                foreach (int unitID in model.SelectedUnitIDs)
                {
                    model.UnitID = unitID;
                    invoiceID = OperationRecordManager.CreateOperationRecord (model);
                }
                if (model.IsEmailReceipt)
                {
                    Email.EmailInvoice(invoiceID);
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message);
            }
            finally
            {
                sqlConn.Close();
            }
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public ActionResult Edit(int id)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Edit Expense";

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
            if(String.IsNullOrEmpty (model.FinancialBankAccountID ))
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
            model.UploadBy = Int32.Parse(Session["UserID"].ToString ());
            OperationRecordManager.Reimburse(model);

            //send email to end user
            if (model.IsEmailReceipt )
            {
                Email.EmailPayment(model.ID, model.ContractorID, model.UnitID, model.CompleteDate,model.FinancialBankAccountID , model.DueAmount, model.Payment, (int)Helpers.Helpers.EmailType.Invoice);
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
