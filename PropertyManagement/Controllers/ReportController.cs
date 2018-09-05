using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Configuration;
using System.Data.SqlClient;
using PropertyManagement.Models;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using CCHMC.Radiology.Library.Authentication;
using CCHMC.AD.Library.Extensions;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using PropertyManagement.ViewModels;
using PropertyManagement.ViewModels.Task;
using System.Data;
using System.Text;
using PropertyManagement.Helpers;

namespace PropertyManagement.Controllers
{
    [Authorize]
    public class ReportController : BaseController
    {
        private string reporttitle = "Report Management";
        //
        // GET: /Report/
        
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

            var statusList = GetList((short)Helpers.Helpers.ListType.allStatus);
            ViewBag.statusList = new MultiSelectList(statusList, "id", "description");

            var ReportType = GetDropdownReportList();
            ViewBag.ReportType = new MultiSelectList(ReportType, "id", "description");

            var units = GetList((short)Helpers.Helpers.ListType.unit);
            ViewBag.units = new MultiSelectList(units, "id", "description");

            var bankAccounts = GetList((short)Helpers.Helpers.ListType.bankaccount);
            ViewBag.bankAccounts = new MultiSelectList(bankAccounts, "id", "description");
            
            var categoryList = GetList((short)Helpers.Helpers.ListType.allExpenseCategory);
            ViewBag.categories = new MultiSelectList(categoryList, "id", "description");

            var contractors = GetList((short)Helpers.Helpers.ListType.allUser);
            ViewBag.contractors = new MultiSelectList(contractors, "id", "description");

            // Get the drill down level from the DB for the drop-down listbox
            var drilldownlevel = GetDropdownDrillDownLevel();
            ViewBag.DrillDownLevel = new MultiSelectList(drilldownlevel, "id", "description");

            var ChartType = GetDropdownChartType();
            ViewBag.ChartType = new MultiSelectList(ChartType, "id", "description");

            //setup default value of the start date and end date
            if (Session["startDate"] == null)
            {
                DateTime oneMonth = DateTime.Now.AddMonths(-1);
                Session["startDate"] = new DateTime(oneMonth.Year, 1, 1).ToString("MM/dd/yyyy");
                Session["endDate"] = DateTime.Now.ToString("MM/dd/yyyy");
            }

            return View();
        }

        [AllowAnonymous]
        public ActionResult IncomeStatementReport(string startDate, string endDate, string[] companyIDs, string[] propertyIDs, string[] unitIDs, string[] ContractorIDs, string[] FinancialAccountIDs, string[] CategoryIDs, string[] StatusIDs, string[] ChartTypeID, string[] drillDownLevelID)
        {
            Session["startDate"] = startDate;
            Session["endDate"] = endDate;
            Session["selectedCompanyIDs"] = companyIDs;
            Session["selectedPropertyIDs"] = propertyIDs;
            Session["selectedUnitIDs"] = unitIDs;
            Session["selectedAccountIDs"] = FinancialAccountIDs;
            Session["selectedStatusIDs"] = StatusIDs;
            Session["selectedContractorIDs"] = ContractorIDs;
            Session["selectedCategoryIDs"] = CategoryIDs;

            Dictionary<string, double> columnSum = new Dictionary<string, double>();
            //get the first day of the week for the start date
            DateTime start_date = DateTime.Parse(startDate);
            DateTime end_date = DateTime.Parse(endDate);
            DateTime startOfWeek = start_date.AddDays(-(int)start_date.DayOfWeek);
            //string joinWeekEndExamDate = "LEFT OUTER JOIN(SELECT DATEDIFF(week, '"+ startOfWeek+"', dateadd(hour, -7, end_exam_dt)) as group_date, id  FROM rad_exam_facts) as endexamview on endexamview.id = xf.id";
            //string joinWeekFirstReportDate = "LEFT OUTER JOIN(SELECT DATEDIFF(week, '" + startOfWeek + "', dateadd(hour, -7, first_report_dt)) as group_date, id  FROM rad_exam_facts) as reportexamview on reportexamview.id = xf.id";
            string joinDrillDownLevel = " LEFT OUTER JOIN(SELECT FinishDate as group_date,Year(FinishDate) as FiscalYear, DatePart(QUARTER,  FinishDate) AS Quarter, id  FROM tblUnitOperation) as endexamview on endexamview.id = tblUnitOperation.id";

            StringBuilder sqlBuilder = new StringBuilder();
            StringBuilder selectClauseBuilder = new StringBuilder();
            StringBuilder whereClauseBuilder = new StringBuilder();
            StringBuilder groupClauseBuilder = new StringBuilder();
            StringBuilder orderClauseBuilder = new StringBuilder();
          //  StringBuilder interestSelectClause = new StringBuilder();
            string resultSortOrder = "Date";
            int drillDownLeve = Int32.Parse(drillDownLevelID[0]);
            //group based on different drill down level
            switch (drillDownLeve)
            {
                //group by daily
                case (int)Helpers.Helpers.DrilldownLevel.Daily:
                    selectClauseBuilder.Append(" cast(FinishDate as date)  AS Date  ");
                    whereClauseBuilder.Append(" cast(FinishDate as date) >= '" + startDate + "' and cast(FinishDate as date) <= '" + endDate + "'");
                    groupClauseBuilder.Append(" cast(FinishDate as date) , CategoryName ");
                    orderClauseBuilder.Append("CategoryName, cast(FinishDate as date) ASC ");
                    resultSortOrder = "Date";
               //     interestSelectClause.Append("select LoanAmount*InterestRate/365 as Amount");
                    break;
                //group by weekly
                case (int)Helpers.Helpers.DrilldownLevel.Weekly:
                    selectClauseBuilder.Append(" DATEDIFF(week, '" + startOfWeek + "', FinishDate) as date ");
                    whereClauseBuilder.Append(" cast(FinishDate as date) >= '" + startDate + "' and cast(FinishDate as date) <= '" + endDate + "'");
                    groupClauseBuilder.Append(" DATEDIFF(week, '" + startOfWeek + "', FinishDate),  CategoryName ");
                    orderClauseBuilder.Append("CategoryName, DATEDIFF(week, '" + startOfWeek + "', FinishDate) ASC ");
                    resultSortOrder = "Date";
                //    interestSelectClause.Append("select LoanAmount*InterestRate/365*7 as Amount");
                    break;
                //group by monthly
                case (int)Helpers.Helpers.DrilldownLevel.Monthly:
                    selectClauseBuilder.Append(" year(FinishDate) as year, month(FinishDate)  AS Date  ");
                    whereClauseBuilder.Append(" cast(FinishDate as date) >= '" + startDate + "' and cast(FinishDate as date) <= '" + endDate + "'");
                    groupClauseBuilder.Append(" year(FinishDate), month(FinishDate), CategoryName   ");
                    orderClauseBuilder.Append(" CategoryName, year(FinishDate), month(FinishDate) ASC ");
                    resultSortOrder = "year, Date";
                 //   interestSelectClause.Append("select LoanAmount*InterestRate/12 as Amount");
                    break;
                //group by quarterly
                case (int)Helpers.Helpers.DrilldownLevel.Quarterly:
                    selectClauseBuilder.Append(" FiscalYear as year, Quarter as Date  ");
                    whereClauseBuilder.Append(" cast(FinishDate as date) >= '" + start_date + "' and cast(FinishDate as date) <= '" + end_date + "'");
                    groupClauseBuilder.Append(" FiscalYear, Quarter , CategoryName ");
                    orderClauseBuilder.Append("  CategoryName, FiscalYear, Quarter ASC ");
                    resultSortOrder = "year, Date";
                   // interestSelectClause.Append("select LoanAmount*InterestRate/4 as Amount");
                    break;
                //group by YEARLY
                case (int)Helpers.Helpers.DrilldownLevel.Yearly:
                    selectClauseBuilder.Append(" FiscalYear as year, FiscalYear as Date  ");
                    whereClauseBuilder.Append(" cast(FinishDate as date) >= '" + start_date + "' and cast(FinishDate as date) <= '" + end_date + "'");
                    groupClauseBuilder.Append(" FiscalYear, FiscalYear , CategoryName ");
                    orderClauseBuilder.Append("  CategoryName, FiscalYear ASC  ");
                    resultSortOrder = "year, Date";
                    //interestSelectClause.Append("select LoanAmount*InterestRate as Amount from tblProperty ");
                    break;
                default:
                    break;
            }
            sqlBuilder.Append(" select  ");
            sqlBuilder.Append(" CategoryName, sum(Amount) AS Amount, " + selectClauseBuilder );
            sqlBuilder.Append(" FROM tblUnitOperation");
            sqlBuilder.Append(" INNER JOIN tblPropertyUnit on tblPropertyUnit.UnitID = tblUnitOperation.UnitID ");
            sqlBuilder.Append(" INNER JOIN mCompanyProperty on mCompanyProperty.PropertyID = tblPropertyUnit.PropertyID ");
            sqlBuilder.Append(" INNER JOIN cExpenseCategory on cExpenseCategory.CategoryID = tblUnitOperation.CategoryID ");
            sqlBuilder.Append(joinDrillDownLevel);

            // Add modality id to the where clause if appropriate
            if (companyIDs != null && companyIDs.Count() > 0 && !string.IsNullOrEmpty(companyIDs[0]))
            {
                whereClauseBuilder.Append(" AND mCompanyProperty.CompanyID IN (" + String.Join(",", companyIDs) + ")");
            }
            else
            {
                //get the companys only the owner can access
                whereClauseBuilder.Append(" AND mCompanyProperty.CompanyID IN (" + Helpers.Helpers.GetUserManagedCompanyString(Session ["UserID"].ToString()) + ")");
            }
            // Add modality id to the where clause if appropriate
            if (propertyIDs != null && propertyIDs.Count() > 0 && !string.IsNullOrEmpty(propertyIDs[0]))
            {
                whereClauseBuilder.Append(" AND tblPropertyUnit.PropertyID IN (" + String.Join(",", propertyIDs) + ")");
            }
            // Add modality id to the where clause if appropriate
            if (StatusIDs != null && StatusIDs.Count() > 0 && !string.IsNullOrEmpty(StatusIDs[0]))
            {
                whereClauseBuilder.Append(" AND tblUnitOperation.StatusID IN (" + String.Join(",", StatusIDs) + ")");
            }

            // Add radiologist id to the where clause if appropriate
            if (ContractorIDs != null && ContractorIDs.Count() > 0 && !string.IsNullOrEmpty(ContractorIDs[0]))
            {
                whereClauseBuilder.Append(" AND tblUnitOperation.ContractorID in (" + String.Join(",", ContractorIDs) + ")");
            }
            // Add Hour of the Day to the where clause if appropriate
            if (FinancialAccountIDs != null && FinancialAccountIDs.Count() > 0 && !string.IsNullOrEmpty(FinancialAccountIDs[0]))
            {
                whereClauseBuilder.Append(" AND tblUnitOperation.FinancialAccountID in (" + String.Join(",", FinancialAccountIDs) + ")");
            }

            // Add the Service id to the where clause if appropriate
            if (CategoryIDs != null && CategoryIDs.Count() > 0 && !string.IsNullOrEmpty(CategoryIDs[0]))
            {
                whereClauseBuilder.Append(" AND tblUnitOperation.CategoryID in (" + String.Join(",", CategoryIDs) + ")");
            }

            sqlBuilder.Append(" where " + whereClauseBuilder );
            sqlBuilder.Append(" group by " + groupClauseBuilder);
            sqlBuilder.Append(" order by " + orderClauseBuilder);

            List<Dictionary<string, object>> dataResult = new List<Dictionary<string, object>>();

            DataTable pivotTable = new DataTable();
            DataTable dataTable = new DataTable();

            //// Execute the SQL query and get the results
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection);
                try
                {
                    dataTable.Columns.Add(new DataColumn("CategoryName", typeof(string)));
                    dataTable.Columns.Add(new DataColumn("Amount", typeof(double)));
                    dataTable.Columns.Add(new DataColumn("Year", typeof(int)));
                    dataTable.Columns.Add(new DataColumn("Date", typeof(int)));
                    dataTable.Columns.Add(new DataColumn("MonthNumber", typeof(int)));

                    connection.Open();
                    SqlDataAdapter da = new SqlDataAdapter(command);
                    da.Fill(dataTable);
                    connection.Close();

                }
                catch (Exception ex)
                {
                    string exms = ex.Message;
                }
            }

            pivotTable.Columns.Add(new DataColumn("CategoryName", typeof(string)));
            if (drillDownLeve == (int)Helpers.Helpers.DrilldownLevel.Quarterly)
            {
                int quarterSpan = (end_date.Year - start_date.Year) * 4 + (int)(end_date.Month / 3) - (int)(start_date.Month / 3) + 1;
                for (int quarter = 1; quarter <= quarterSpan; quarter++)
                {
                    pivotTable.Columns.Add("Quarter-" + new DataColumn(quarter.ToString(), typeof(double)));
                    columnSum.Add("Quarter-" + new DataColumn(quarter.ToString(), typeof(double)),0);
                }
                string category = "";
                DataRow dr = null;
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    string category1 = (string)dataTable.Rows[i][0];
                    if (category1 != category)
                    {
                        dr = pivotTable.NewRow();
                        dr[0] = category1;
                        pivotTable.Rows.Add(dr);
                        category = category1;
                    }
                    String col = "Quarter-"+(((int)(dataTable.Rows[i][2]) - start_date.Year) * 4 + (int)(dataTable.Rows[i][3])).ToString();
                    dr[col] = dataTable.Rows[i][1];
                    columnSum[col] += (double)dataTable.Rows[i][1];
                }
            }
            else if (drillDownLeve == (int)Helpers.Helpers.DrilldownLevel.Monthly)
            {
                int monthSpan = (end_date.Year - start_date.Year) * 12 + end_date.Month - start_date.Month + 1;
                for (int month = 1; month <= monthSpan; month++)
                {
                    pivotTable.Columns.Add(new DataColumn("Month-"+month.ToString(), typeof(double)));
                    columnSum.Add("Month-" + month.ToString(), 0);
                }
                string category = "";
                DataRow dr = null;
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    string category1 = (string)dataTable.Rows[i][0];
                    if (category1 != category)
                    {
                        dr = pivotTable.NewRow();
                        dr[0] = category1;
                        pivotTable.Rows.Add(dr);
                        category = category1;
                    }
                    DateTime temp = new DateTime((int)(dataTable.Rows[i][2]), (Int32)(dataTable.Rows[i][3]), 1);
                    String col = "Month-"+((temp.Year - start_date.Year) * 12 + temp.Month - start_date.Month + 1).ToString();
                    dr[col] = dataTable.Rows[i][1];
                    columnSum[col] += (double)dataTable.Rows[i][1];
                }
            }
            else if (drillDownLeve == (int)Helpers.Helpers.DrilldownLevel.Yearly)
            {
                for (int year = start_date.Year; year <= end_date.Year; year++)
                {
                    pivotTable.Columns.Add(new DataColumn("Year-"+year.ToString() , typeof(double)));
                    columnSum.Add("Year-" + year.ToString(),0);
                }
                string category = "";
                DataRow dr = null;
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    string category1 = (string)dataTable.Rows[i][0];
                    if (category1 != category)
                    {
                        dr = pivotTable.NewRow();
                        dr[0] = category1;
                        pivotTable.Rows.Add(dr);
                        category = category1;
                    }
                    String col = "Year-" + (dataTable.Rows[i][2]).ToString();
                    dr[col] = dataTable.Rows[i][1];
                    columnSum[col] += (double)dataTable.Rows[i][1];
                }
            }
            else if (drillDownLeve == (int)Helpers.Helpers.DrilldownLevel.Daily)
            {
                for (int day = start_date.DayOfYear; day <= end_date.DayOfYear; day++)
                {
                    pivotTable.Columns.Add(new DataColumn("Day-" + day.ToString(), typeof(double)));
                    columnSum.Add("Day-" + day.ToString(), 0);
                }
                string category = "";
                DataRow dr = null;
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    string category1 = (string)dataTable.Rows[i][0];
                    if (category1 != category)
                    {
                        dr = pivotTable.NewRow();
                        dr[0] = category1;
                        pivotTable.Rows.Add(dr);
                        category = category1;
                    }
                    String col = "Day-"+(dataTable.Rows[i][2]).ToString();
                    dr[col] = dataTable.Rows[i][1];
                    columnSum[col] += (double)dataTable.Rows[i][1];
                }
            }

            DataRow summaryRow = pivotTable.NewRow();
            summaryRow[0] = "Summary";
            foreach (KeyValuePair<string, double> pair in columnSum)
            {
                summaryRow [ pair.Key] = (double)pair.Value;
            }
            pivotTable.Rows.Add(summaryRow);
            ////allocate data
            ////group based on different drill down level
            //string expression = "";
            //int seriaNumber = 1;
            //Dictionary<string, object> aSeries = new Dictionary<string, object>();
            //aSeries["data"] = getAxisLabel(drillDownLevelID[0], start_date, end_date, startOfWeek, Helpers.Helpers.AxisType.yAxis);
            //aSeries["name"] = "";
            //expression = "id = " + 1;
            //aSeries["data"] = signValueforSerie(dataTable, expression, resultSortOrder, drillDownLevelID[0], (List<object[]>)aSeries["data"]);
            //dataResult.Add(aSeries);

            //ViewData["chartTitle"] = "Income statement";

            //ViewData["yaxisdatatat"] = dataResult;

            //ViewData["xaxislabels"] = getAxisLabel(drillDownLevelID[0], start_date, end_date, startOfWeek, Helpers.Helpers.AxisType.xAxis).Select(x => x[0]).ToArray();

            //ViewData["chartType"] = ((Helpers.Helpers.ChartType)(Int32.Parse(ChartTypeID[0]))).ToString().ToLower();

            ViewBag.TableCaption = reporttitle + ": ";
            return PartialView("IncomeStatementReport", pivotTable); // Data is returned in the ViewData objects!
        }


        [AllowAnonymous]
        public ActionResult IncomeDetailReport(string categoryName)
        {
            DateTime startDate = DateTime.Parse((string)Session["startDate"]);
            DateTime endDate = DateTime.Parse((string)Session["endDate"]);
            string[] companyIDs = (Session["selectedCompanyIDs"] != null)? (string[])Session["selectedCompanyIDs"]:null;
            string[] propertyIDs= (Session["selectedPropertyIDs"] != null) ? (string[])Session["selectedPropertyIDs"] : null; 
            string[] unitIDs= (Session["selectedUnitIDs"] != null) ? (string[])Session["selectedUnitIDs"] : null; 
            string[] bankAccountIDs= (Session["selectedAccountIDs"] != null) ? (string[])Session["selectedAccountIDs"] : null; 
            string[] statusIDs= (Session["selectedStatusIDs"] != null) ? (string[])Session["selectedStatusIDs"] : null; 
            string[] contractorIDs= (Session["selectedContractorIDs"] != null) ? (string[])Session["selectedContractorIDs"] : null; 
            string[] categoryIDs= (Session["selectedCategoryIDs"] != null) ? (string[])Session["selectedCategoryIDs"] : null; 
            List<OperationRecord> result = OperationRecordManager.GetExpense(startDate.ToString (), endDate.ToString (), companyIDs, propertyIDs, unitIDs, bankAccountIDs, statusIDs, contractorIDs, categoryIDs, "", (int)Session["UserID"]);
            result = (from r in result
                      where r.CategoryName == categoryName
                      select r).ToList();
            bool isStartNull = startDate.Equals(DateTime.MinValue);
            bool isEndNull = endDate.Equals(DateTime.MinValue);
            ViewBag.TableCaption = reporttitle + " Expense: ";
            if (!startDate.Equals(DateTime.MinValue))
            {
                ViewBag.TableCaption += " fromt " + startDate.ToString("g");
            }
            if (!endDate.Equals(DateTime.MinValue))
            {
                ViewBag.TableCaption += " thru " + endDate.ToString("g");
            }
            
            return View("IncomeDetailReport", result);

        }


        [AllowAnonymous]
        public ActionResult AssetReport(string startDate, string endDate, string[] companyIDs, string[] propertyIDs, string[] statusIDs)
        {
            StringBuilder sbOperation = new StringBuilder();
            StringBuilder whereClause = new StringBuilder();
            List<Property> allProperty = new List<Property>();

            sbOperation.Append("select tblProperty.*, mCompanyProperty.CompanyID from tblProperty ");
            sbOperation.Append(" INNER JOIN mCompanyProperty on mCompanyProperty.PropertyID = tblProperty.PropertyID ");

               // Add modality id to the where clause if appropriate
            if (companyIDs != null && companyIDs.Count() > 0 && !string.IsNullOrEmpty(companyIDs[0]))
            {
                whereClause.Append(" AND mCompanyProperty.CompanyID IN (" + String.Join(",", companyIDs) + ")");
            }
            else
            {
                //get the companys only the owner can access
                whereClause.Append(" AND mCompanyProperty.CompanyID IN (" + Helpers.Helpers.GetUserManagedCompanyString(Session["UserID"].ToString()) + ")");
            }
            // Add modality id to the where clause if appropriate
            if (propertyIDs != null && propertyIDs.Count() > 0 && !string.IsNullOrEmpty(propertyIDs[0]))
            {
                whereClause.Append(" AND tblProperty.PropertyID IN (" + String.Join(",", propertyIDs) + ")");
            }
            // Add modality id to the where clause if appropriate
            if (statusIDs != null && statusIDs.Count() > 0 && !string.IsNullOrEmpty(statusIDs[0]))
            {
                whereClause.Append(" AND tblProperty.StatusID IN (" + String.Join(",", statusIDs) + ")");
            }

            sbOperation.Append(whereClause.Remove(0, 4).Insert(0, " where "));
            sbOperation.Append(" Order by Address");

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
                        Property property = PropertyManager.FillInPropertyWithData(dr);
                        allProperty.Add(property);
                    }
                }
            }
            return PartialView("AssetReport", allProperty );
        }
        public List<object[]> signValueforSerie(DataTable dataTable, string expression, string sortOrder, string drillDownLevel, List<object[]> aSeries)
        {
            DataRow[] foundRows = dataTable.Select(expression, sortOrder);
            DateTime calendarDate;

            // Print column 0 of each returned row.
            foreach (DataRow dr in foundRows)
            {
                string valueName = "";
                switch (drillDownLevel)
                {
                    //group by daily
                    case "1":
                        //query result is Date
                        calendarDate = DateTime.Parse(dr[0].ToString());
                        valueName = getLabelName(drillDownLevel, calendarDate, 0, 0, 0, 0);
                        break;
                    //group by weekly
                    case "2":
                        //query result is week
                        valueName = getLabelName(drillDownLevel, DateTime.Now, (Int32.Parse(dr[0].ToString()) + 1), 0, 0, 0);
                        break;
                    //group by monthly
                    case "3":
                        //query result is year, month
                        DateTime dt = new DateTime(Int32.Parse(dr[0].ToString()), Int32.Parse(dr[1].ToString()), 1);
                        valueName = getLabelName(drillDownLevel, dt, 0, 0, 0, 0);
                        break;
                    //group by quarterly
                    case "4":
                        //query result is year, quarter
                        valueName = getLabelName(drillDownLevel, DateTime.Now, 0, 0, (int)dr[1], (int)dr[0]);
                        break;
                }
                foreach (object[] defaultValue in aSeries)
                {
                    if (defaultValue[0].ToString().Equals(valueName))
                    {
                        if (drillDownLevel.Equals("4") || drillDownLevel.Equals("3"))
                        {
                            defaultValue[1] = int.Parse(dr[2].ToString());
                        }
                        else
                        {
                            defaultValue[1] = (decimal)dr[1];
                        }
                        break;

                    }
                }
            }
            return aSeries;
        }

        public List<object[]> getAxisLabel(string drillDownLevelID, DateTime start_date, DateTime end_date, DateTime startOfWeek, Helpers.Helpers.AxisType axisType)
        {
            List<object[]> listData = new List<object[]>();
            switch (drillDownLevelID)
            {
                //group by daily
                case "1":
                    while (start_date <= end_date)
                    {
                        object[] values = new object[2];
                        values[0] = getLabelName(drillDownLevelID, start_date, 0, 0, 0, 0);
                        values[1] = 0;
                        listData.Add(values);
                        start_date = start_date.AddDays(1);
                    }
                    break;
                //group by weekly
                case "2":
                    int startWeek = 1;
                    while (startOfWeek <= end_date)
                    {
                        object[] values = new object[2];
                        // pull out week
                        values[0] = getLabelName(drillDownLevelID, start_date, startWeek, 0, 0, 0);
                        values[1] = 0;
                        listData.Add(values);

                        startOfWeek = startOfWeek.AddDays(7);
                        startWeek += 1;
                    }
                    break;
                //group by monthly
                case "3":
                    start_date = new DateTime(start_date.Year, start_date.Month, 1);
                    while (start_date <= end_date)
                    {
                        object[] values = new object[2];
                        // pull out month and year
                        values[0] = getLabelName(drillDownLevelID, start_date, 0, start_date.Month, 0, start_date.Year);
                        values[1] = 0;
                        listData.Add(values);
                        start_date = start_date.AddMonths(1);
                    }
                    break;
                //group by quarterly
                case "4":
                    start_date = new DateTime(start_date.Year, start_date.Month, 1).AddMonths(6);
                    end_date = end_date.AddMonths(6);
                    while (start_date <= end_date)
                    {
                        object[] values = new object[2];
                        // pull out month and year
                        values[0] = getLabelName(drillDownLevelID, start_date, 0, 0, (int)Math.Ceiling(start_date.Month / 3.0), start_date.Year);
                        values[1] = 0;
                        listData.Add(values);
                        start_date = start_date.AddMonths(3);
                    }
                    break;
                default:
                    break;
            }
            return listData;
        }

        public string getLabelName(string drillDownLevelID, DateTime start_date, int week, int month, int quarter, int year)
        {
            switch (drillDownLevelID)
            {
                //group by daily
                case "1":
                    return start_date.ToString("MM/dd/yy");
                //group by weekly
                case "2":
                    return "Week" + "-" + week.ToString();
                //group by monthly
                case "3":
                    return start_date.ToString("MM") + "/" + start_date.ToString("yy");
                //group by quarterly
                case "4":
                    return year + "/Q-" + quarter;
                default:
                    break;
            }
            return "";
        }


        //private List<OperationRecord> GetInterestList()
        //{
        //    DateTime start = startDate.SelectedDate.Value;
        //    DateTime end = endDate.SelectedDate.Value;

        //    StringBuilder sb = new StringBuilder();
        //    sb.Append(" select distinct.tblProperty.Address, CASE WHEN '" + start + "' >tblProperty.PurchaseDate ");
        //    sb.Append(" THEN tblProperty.LoanAmount*tblProperty.InterestRate/365*datediff(dd, '" + start + "', '" + end + "')");
        //    sb.Append(" WHEN tblProperty.PurchaseDate IS NOT NULL AND '" + end + "' < tblProperty.PurchaseDate THEN tblProperty.loanAmount*0");
        //    sb.Append(" WHEN tblProperty.SoldDate IS NOT NULL AND '" + end + "' > tblProperty.SoldDate THEN tblProperty.LoanAmount*tblProperty.InterestRate/365*datediff(dd, '" + start + "', tblProperty.SoldDate)");
        //    sb.Append(" ELSE tblProperty.LoanAmount*tblProperty.InterestRate/365*datediff(dd, tblProperty.PurchaseDate, '" + end + "')");
        //    sb.Append(" END AS Interest");
        //    sb.Append(" from tblProperty, tblPropertyUnit where tblProperty.PropertyID = tblPropertyUnit.PropertyID ");
        //    sb.Append(" AND tblPropertyUnit.UnitID in ( " + getUnitIdString() + ")");
        //    sb.Append(" AND tblProperty.InterestRate is not null and tblProperty.InterestRate > 0 ");
        //    sb.Append(" AND tblProperty.LoanAmount is not null and tblProperty.LoanAmount > 0 AND tblProperty.PurchaseDate is not null");

        //    System.Web.UI.DataSourceSelectArguments args = new System.Web.UI.DataSourceSelectArguments();
        //    DataView view = (DataView)SqlDataSource1.Select(args);
        //    SqlDataSource1.SelectCommand = sb.ToString();
        //    rgInterest.DataSource = SqlDataSource1;
        //    rgInterest.DataBind();
        //}
        

    }
}
