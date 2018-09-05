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
    public class SupplyController : BaseController
    {
        private string reporttitle = "Supply Request"; // Specify the report title here

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
        public PartialViewResult ReportView(string startDate, string endDate, string[] unitIDs, string[] statusIDs, string[] contractorIDs)
        {
            Session["startDate"] = startDate;
            Session["endDate"] = endDate;
            Session["selectedUnitIDs"] = unitIDs;
            Session["selectedStatusIDs"] = statusIDs;
            Session["selectedContractorIDs"] = contractorIDs;
            DateTime start = DateTime.Parse(startDate);
            DateTime end = DateTime.Parse(endDate);
            double totalPayment = 0;
            double totalDeposit = 0;
            double totalBalace = 0;
            StringBuilder sbOperation = new StringBuilder();
            sbOperation.Append("Select distinct ID, [tblUnitOperation].DueDate, FinishDate, PaidBy.FirstName +' ' + PaidBy.LastName as PaidBy, ");
            sbOperation.Append("tblUnitOperation.Notes,[tblUnitOperation].LinkedExpenseID, Amount, tblAccount.AccountName, tblProperty.Address, tblPropertyUnit.UnitName, IsCredit, ");
            sbOperation.Append(" DueAmount, [tblUnitOperation].StatusID, cStatusType.Name  as StatusName, cExpenseCategory.CategoryName,tblUnitOperation.BankTracking  from tblUnitOperation ");
            sbOperation.Append(" inner join  tblPropertyUnit on tblPropertyUnit.UnitID =  tblUnitOperation.UnitID ");
            sbOperation.Append(" INNER JOIN  tblProperty ON tblProperty.PropertyID = tblPropertyUnit.PropertyID ");
            sbOperation.Append(" INNER JOIN mCompanyProperty on mCompanyProperty.PropertyID = tblProperty.PropertyID ");
            sbOperation.Append(" LEFT OUTER JOIN cUser as PaidBy on PaidBy.UserID = tblUnitOperation.ContractorID ");
            sbOperation.Append(" LEFT OUTER JOIN cStatusType on cStatusType.StatusTypeID = tblUnitOperation.StatusID ");
            sbOperation.Append(" LEFT OUTER JOIN cExpenseCategory on cExpenseCategory.CategoryID = tblUnitOperation.CategoryID ");
            sbOperation.Append(" LEFT OUTER JOIN tblAccount as tblAccount on tblAccount.FinancialAccountID = tblUnitOperation.FinancialAccountID ");

            StringBuilder whereClause = new StringBuilder();

            if (!String.IsNullOrEmpty(startDate))
            {
                start = DateTime.Parse(startDate);
                whereClause.Append(" and [tblUnitOperation].FinishDate>='" + start.ToShortDateString() + "' ");
            }
            if (!String.IsNullOrEmpty(endDate))
            {
                end = DateTime.Parse(endDate);
                whereClause.Append(" and [tblUnitOperation].FinishDate<='" + end.ToShortDateString() + "'");
            }

            //get the companys only the owner can access
            whereClause.Append(" AND mCompanyProperty.CompanyID IN (" + GetUserManagedCompanyString() + ")");

            // Add modality id to the where clause if appropriate
            if (unitIDs != null && unitIDs.Count() > 0 && !string.IsNullOrEmpty(unitIDs[0]))
            {
                whereClause.Append(" AND tblPropertyUnit.UnitID IN (" + String.Join(",", unitIDs) + ")");
            }
            if (statusIDs != null && statusIDs.Count() > 0 && !string.IsNullOrEmpty(statusIDs[0]))
            {
                whereClause.Append(" AND [tblUnitOperation].StatusID IN (" + String.Join(",", statusIDs) + ")");
            }
            if (contractorIDs != null && contractorIDs.Count() > 0 && !string.IsNullOrEmpty(contractorIDs[0]))
            {
                whereClause.Append(" AND [tblUnitOperation].ContractorID IN (" + String.Join(",", contractorIDs) + ")");
            }

            whereClause.Append(" AND [tblUnitOperation].CategoryID IN (18)");

            sbOperation.Append(whereClause.Remove(0, 4).Insert(0, " where "));

            sbOperation.Append(" Order by DueDate");

            // Create a list of our result class to hold the data from the query
            // Please ensure you instatiate the class for this controller and not a different controller
            List<OperationRecord> result = new List<OperationRecord>();
            // Execute the SQL query and get the results

            using (SqlDataAdapter adapter = new SqlDataAdapter(sbOperation.ToString(), Helpers.Helpers.GetAppConnectionString()))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                if (tb != null && tb.Rows.Count > 0)
                {
                    for (int i = 0; i < tb.Rows.Count; i++)
                    {
                        DataRow dr = tb.Rows[i];
                        OperationRecord row = new OperationRecord();
                        if (dr["DueDate"] != DBNull.Value)
                        {
                            row.DueDate = DateTime.Parse(dr["DueDate"].ToString());
                        }
                        if (dr["FinishDate"] != DBNull.Value) { row.CompleteDate = DateTime.Parse(dr["FinishDate"].ToString()); }
                        row.PaidBy = dr["PaidBy"].ToString();
                        if (dr["BankTracking"] != DBNull.Value)
                        {
                            row.Memo = dr["Notes"].ToString() + " " + dr["BankTracking"].ToString();
                        }
                        else
                        {
                            row.Memo = dr["Notes"].ToString();
                        }
                        if (dr["Amount"] != DBNull.Value)
                        {
                            row.Payment = double.Parse(dr["Amount"].ToString());
                        }
                        row.BankAccountName = dr["AccountName"].ToString();
                        row.Address = dr["Address"].ToString() + " -- " + dr["UnitName"].ToString();
                        if (dr["DueAmount"] != DBNull.Value)
                        {
                            row.DueAmount = double.Parse(dr["DueAmount"].ToString());
                        }
                        row.ID = int.Parse(dr["ID"].ToString());
                        if (dr["StatusID"] != DBNull.Value)
                        {
                            row.StatusID = short.Parse(dr["StatusID"].ToString());
                            row.StatusName = dr["StatusName"].ToString();
                        }
                        if (dr["LinkedExpenseID"] != DBNull.Value)
                        {
                            row.LinkedExpenseID = Int32.Parse(dr["LinkedExpenseID"].ToString());
                        }
                        row.CategoryName = dr["CategoryName"].ToString();
                        result.Add(row);
                        totalDeposit += row.Payment;
                        totalPayment += row.DueAmount;
                        totalBalace += row.DueAmount + row.Payment;
                    }
                }
            }
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

            ViewBag.TotalPayment = totalPayment;
            ViewBag.TotalDeposit = totalDeposit;
            ViewBag.TotalBalace = totalBalace;
            return PartialView("ReportView", result);
        }


        [HttpGet]
        [AllowAnonymous]
        public ActionResult Add()
        {
            // add new user
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Request new supply request";
            // Get the users from the DB for the drop-down listbox
            var allUsers = GetList((short)Helpers.Helpers.ListType.allUser);
            //ViewBag.AllTenants = new MultiSelectList(allUsers, "id", "description");

            var units = GetList((short)Helpers.Helpers.ListType.unit);
            ViewBag.units = new MultiSelectList(units, "id", "description");

            OperationRecord model = new OperationRecord();
            
            model.AllUnits = GetSelectListItems((short)Helpers.Helpers.ListType.unit);

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Add(OperationRecord model, string actionName)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Add new supply request";
            model.StatusID = 3;
            model.UploadBy = (int)Session["UserID"];
            model.CategoryID = 18;

            SqlConnection sqlConn = new SqlConnection(ConfigurationManager.ConnectionStrings["SunriseConnectionString"].ConnectionString);
            SqlCommand cmd = sqlConn.CreateCommand();
            DataTable dtSearchResult = new DataTable();
            SqlDataAdapter daSearchResult = new SqlDataAdapter();

            try
            {
                sqlConn.Open();
                if (model.CompleteDate == null)
                {
                    model.CompleteDate = model.DueDate;
                }
                int invoiceID = 0;
                foreach (int unitID in model.SelectedUnitIDs)
                {
                    model.UnitID = unitID;
                    invoiceID = OperationRecordManager.CreateOperationRecord(model);
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
