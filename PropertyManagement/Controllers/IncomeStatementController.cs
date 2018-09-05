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
    public class dropdown_list
    {
        public long id { get; set; }
        public string description { get; set; }
    }

    [Authorize]
    public class IncomeStatementController : BaseController
    {
        private string reporttitle = "Income Statement Report"; // Specify the report title here

        [AllowAnonymous]
        public ActionResult Index()
        {
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
            ViewBag.intervalList = new MultiSelectList(statusList, "id", "description");

            var drilldownlevel = GetDropdownDrillDownLevel();
            ViewBag.DrillDownLevel = new MultiSelectList(drilldownlevel, "id", "description");
            
            dropdown_list dl = new dropdown_list();
            var accountingBasisList = new List<dropdown_list>(2);
            dl = new dropdown_list();
            dl.id = 1;
            dl.description = "Cash";
            accountingBasisList.Add(dl);
            dl.id = 2;
            dl.description = "Accrual";
            accountingBasisList.Add(dl);
            ViewBag.accountingBasisList = new MultiSelectList(accountingBasisList, "id", "description");
            

            //setup default value of the start date and end date
            DateTime oneMonth = DateTime.Now.AddMonths(-1);
            ViewBag.startDate = new DateTime(oneMonth.Year, oneMonth.Month, 1).ToString("MM/dd/yyyy");
            ViewBag.endDate = DateTime.Now.ToString("MM/dd/yyyy");

            return View();
        }

        [AllowAnonymous]
        public PartialViewResult ReportView(string startDate, string endDate, string[] companyIDs, string[] propertyIDs, string[] unitIDs, string[] bankAccountIDs, string[] statusIDs, string[] contractorIDs, string[] categoryIDs)
        {
            DateTime start = DateTime.MinValue;
            DateTime end = DateTime.MinValue;
            double totalPayment = 0;
            double totalDeposit = 0;
            double totalBalace = 0;
            StringBuilder sbOperation = new StringBuilder();
            sbOperation.Append("Select distinct ID, [tblUnitOperation].DueDate, FinishDate, PaidBy.FirstName +' ' + PaidBy.LastName as PaidBy, ");
            sbOperation.Append("tblUnitOperation.Notes,Amount, tblAccount.AccountName, tblProperty.Address, tblPropertyUnit.UnitName, IsCredit, ");
            sbOperation.Append(" DueAmount, [tblUnitOperation].StatusID, cStatusType.Name  as StatusName, cExpenseCategory.CategoryName from tblUnitOperation ");
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
            // Add modality id to the where clause if appropriate
            if (bankAccountIDs != null && bankAccountIDs.Count() > 0 && !string.IsNullOrEmpty(bankAccountIDs[0]))
            {
                whereClause.Append(" AND tblUnitOperation.FinancialAccountID IN (" + String.Join(",", bankAccountIDs) + ")");
            }
            // Add modality id to the where clause if appropriate
            if (companyIDs != null && companyIDs.Count() > 0 && !string.IsNullOrEmpty(companyIDs[0]))
            {
                whereClause.Append(" AND mCompanyProperty.CompanyID IN (" + String.Join(",", companyIDs) + ")");
            }
            // Add modality id to the where clause if appropriate
            if (propertyIDs != null && propertyIDs.Count() > 0 && !string.IsNullOrEmpty(propertyIDs[0]))
            {
                whereClause.Append(" AND tblProperty.PropertyID IN (" + String.Join(",", propertyIDs) + ")");
            }
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
            if (categoryIDs != null && categoryIDs.Count() > 0 && !string.IsNullOrEmpty(categoryIDs[0]))
            {
                whereClause.Append(" AND [tblUnitOperation].CategoryID IN (" + String.Join(",", categoryIDs) + ")");
            }

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
                    for (int i = 0; i <tb.Rows.Count; i++)
                    {
                        DataRow dr = tb.Rows[i];
                        OperationRecord row = new OperationRecord();
                        if (dr["DueDate"] != DBNull.Value)
                        {
                            row.DueDate = DateTime.Parse(dr["DueDate"].ToString());
                        }
                        if (dr["FinishDate"] != DBNull.Value) { row.CompleteDate = DateTime.Parse(dr["FinishDate"].ToString()); }
                        row.PaidBy = dr["PaidBy"].ToString();
                        row.Memo = dr["Notes"].ToString();
                        if (dr["Amount"] != DBNull.Value)
                        {
                            row.Payment = double.Parse(dr["Amount"].ToString());
                        }
                        row.BankAccountName = dr["AccountName"].ToString();
                        row.Address = dr["Address"].ToString() + dr["UnitName"].ToString();
                        if (dr["DueAmount"] != DBNull.Value)
                        {
                            row.DueAmount = double.Parse(dr["DueAmount"].ToString());
                        }
                        row.ID = int.Parse(dr["ID"].ToString());
                        if (dr["StatusID"] != DBNull.Value)
                        {
                            row.StatusName = dr["StatusName"].ToString();
                        }
                        row.CategoryName = dr["CategoryName"].ToString();
                        result.Add(row);
                        totalDeposit += row.Payment;
                        totalPayment += row.DueAmount;
                        totalBalace += row.Payment - row.DueAmount;
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
            return PartialView("IncomeStatementReport", result);
        }
    }
}
