using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Text;
using System.Drawing;
using System.Globalization;
using System.Web.Script.Serialization;
using PropertyManagement.Models;
using PropertyManagement.ViewModels;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Net;
using System.IO;
using System.ComponentModel;

namespace PropertyManagement.Controllers
{
    [Authorize]
    public class ECommerceSaleMapController : BaseController
    {
        private string reporttitle = "E Commerce Sales Map"; // Specify the report title here
        private bool shouldNotRemoveSerie = false;

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Index()
        {
            // Enable security to redirect to login page if user is not logged in or we are not running in the VS IDE
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            // Save the report title to the ViewBag
            ViewBag.ReportTitle = reporttitle;


            ViewBag.drillDownLevel = new SelectList(GetDropdownDrillDownLevel(), "id", "description", "");
            ViewBag.comparisonVariable = new SelectList(GetDropdownComparisonVariable(), "id", "description", "");
            ViewBag.chartType = new SelectList(GetDropdownChartType(), "id", "description", "");
            ViewBag.valueType = new SelectList(GetDropdownValueType(), "id", "description", "");

            // Get the values from the DB for the drop-down listboxs, and pre-populate from the report_filters if necessary
            ViewBag.vendors = new MultiSelectList(GetDropdownVendors(), "id", "description", "");
            ViewBag.accounts = new MultiSelectList(GetDropdownAccounts(), "id", "description", "");
            ViewBag.states = new MultiSelectList(GetDropdownStates(), "abbre", "description", "");
            ViewBag.skus = new MultiSelectList(GetDropdownSKU(), "abbre", "description", "");
            ViewBag.warehouses = new MultiSelectList(GetDropdownWarehouse(), "id", "description", "");
            ViewBag.dateRange = "previousmonth";
            ViewBag.startDate = DateTime.Now.AddMonths(-1).ToShortDateString();
            ViewBag.endDate = DateTime.Now.ToShortDateString();

            return View();
        }


        [AllowAnonymous]
        [HttpPost]
        public PartialViewResult MapView(int comparisonVariable, int valueType, int chartType, int drillDownLevel, string[] vendors, string[] skus, string[] accounts, string[] warehouses, string[] states, string startDate, string endDate, bool dispalyNegative)
        {
            //get the first day of the week for the start date
            DateTime start_date = DateTime.Parse(startDate);
            DateTime end_date = DateTime.Parse(endDate);
            DateTime startOfWeek = start_date.AddDays(-(int)start_date.DayOfWeek);

            StringBuilder sqlBuilder = new StringBuilder();
            StringBuilder selectClauseBuilder = new StringBuilder();
            StringBuilder whereClauseBuilder = new StringBuilder();
            StringBuilder groupClauseBuilder = new StringBuilder();
            StringBuilder orderClauseBuilder = new StringBuilder();
            string resultSortOrder = "Date";

            //group based on different drill down level
            switch (drillDownLevel)
            {
                //group by daily
                case (int)Helpers.Helpers.DrilldownLevel.Daily:
                    selectClauseBuilder.Append(" Date ");
                    whereClauseBuilder.Append(" date >= '" + startDate + "' and date <= '" + endDate + "'");
                    groupClauseBuilder.Append(" date ");
                    orderClauseBuilder.Append(" date ASC");
                    resultSortOrder = "Date";
                    break;
                //group by weekly
                case (int)Helpers.Helpers.DrilldownLevel.Weekly:
                    selectClauseBuilder.Append(" DATEDIFF(week, '" + startOfWeek + "', date) as date ");
                    whereClauseBuilder.Append(" cast(date as date) >= '" + startDate + "' and cast(date as date) <= '" + endDate + "'");
                    groupClauseBuilder.Append(" DATEDIFF(week, '" + startOfWeek + "', date) ");
                    orderClauseBuilder.Append(" DATEDIFF(week, '" + startOfWeek + "', date) ASC");
                    resultSortOrder = "Date";
                    break;
                //group by monthly
                case (int)Helpers.Helpers.DrilldownLevel.Monthly:
                    selectClauseBuilder.Append(" year(date) as year, month(date)  AS Date  ");
                    whereClauseBuilder.Append(" cast(date as date) >= '" + startDate + "' and cast(date as date) <= '" + endDate + "'");
                    groupClauseBuilder.Append(" year(date), month(date)  ");
                    orderClauseBuilder.Append(" year(date), month(date) ASC");
                    resultSortOrder = "year, Date";
                    break;
                //group by quarterly
                case (int)Helpers.Helpers.DrilldownLevel.Quarterly:
                    selectClauseBuilder.Append(" year(date) as year, DatePart(QUARTER, date) as Date  ");
                    whereClauseBuilder.Append(" date >= '" + start_date + "' and date <= '" + end_date + "'");
                    groupClauseBuilder.Append(" year(date), DatePart(QUARTER, date) ");
                    orderClauseBuilder.Append(" year, date ASC");
                    resultSortOrder = "year, Date";
                    break;
                default:
                    break;
            }

            switch (valueType)
            {
                case (int)Helpers.Helpers.ValueType.TotalRevenue:
                    selectClauseBuilder.Append(" , sum(sales) as Volume");
                    break;
                case (int)Helpers.Helpers.ValueType.TotalCost:
                    selectClauseBuilder.Append(" , sum(cost) as Volume");
                    break;
                case (int)Helpers.Helpers.ValueType.TotalVolume:
                    selectClauseBuilder.Append(" , sum(quantity) as Volume");
                    break;
                case (int)Helpers.Helpers.ValueType.TotalShipping:
                    selectClauseBuilder.Append(" , sum(shippingFee) as Volume");
                    break;
                case (int)Helpers.Helpers.ValueType.TotalInternationalShipping:
                    selectClauseBuilder.Append(" , sum(internationalShippingFee) as Volume");
                    break;
                case (int)Helpers.Helpers.ValueType.TotalFee:
                    selectClauseBuilder.Append(" , (sum(paypalFee)+sum(ebayFee)) as Volume");
                    break;
                case (int)Helpers.Helpers.ValueType.TotalProfit:
                    selectClauseBuilder.Append(" , sum(profit) as Volume");
                    break;
                case (int)Helpers.Helpers.ValueType.AverageFee:
                    selectClauseBuilder.Append(" , avg(paypalFee+ebayFee) as Volume");
                    break;
                case (int)Helpers.Helpers.ValueType.AverageInternationalShipping:
                    selectClauseBuilder.Append(" , avg(internationalShippingFee) as Volume");
                    break;
                case (int)Helpers.Helpers.ValueType.AverageRevenue:
                    selectClauseBuilder.Append(" , avg(sales) as Volume");
                    break;
                case (int)Helpers.Helpers.ValueType.AverageCost:
                    selectClauseBuilder.Append(" , avg(cost) as Volume");
                    break;
                case (int)Helpers.Helpers.ValueType.AverageShipping:
                    selectClauseBuilder.Append(" , avg(shippingFee) as Volume");
                    break;
                case (int)Helpers.Helpers.ValueType.AverageProfit:
                    selectClauseBuilder.Append(" , avg(profit) as Volume");
                    break;
                case (int)Helpers.Helpers.ValueType.AverageVolume:
                    selectClauseBuilder.Append(" , avg(fee) as Volume");
                    break;
                case (int)Helpers.Helpers.ValueType.StandardDeviationShipping:
                    selectClauseBuilder.Append(" , sum(fee) as Volume");
                    break;

            }

            //group based on different comparision variable
            switch (comparisonVariable)
            {
                case (int)Helpers.Helpers.ComparisionType.Revenue:
                    break;
                case (int)Helpers.Helpers.ComparisionType.Sku:
                    selectClauseBuilder.Append(" , sku");
                    groupClauseBuilder.Append(", sku ");
                    break;
                case (int)Helpers.Helpers.ComparisionType.Account:
                    selectClauseBuilder.Append(" , account");
                    groupClauseBuilder.Append(", account");
                    break;
                case (int)Helpers.Helpers.ComparisionType.State:
                    selectClauseBuilder.Append("  , state");
                    groupClauseBuilder.Append(", state");
                    break;
            }

            sqlBuilder.Append(" select");
            sqlBuilder.Append(selectClauseBuilder);
            sqlBuilder.Append(" FROM tblEbayOrder ");

            // Define the base SQL query 
            if (skus != null && skus.Count() > 0 && !string.IsNullOrEmpty(skus[0]))
            {
                whereClauseBuilder.Append(" AND sku in ('" + String.Join("','", skus) + "')");
            }

            // Add the procedure code(s) to the where clause if appropriate
            if (accounts != null && accounts.Count() > 0 && !string.IsNullOrEmpty(accounts[0]))
            {
                whereClauseBuilder.Append(" AND account in (" + String.Join(",", accounts) + ")");
            }

            // Add site id to the where clause if appropriate
            if (warehouses != null && warehouses.Count() > 0 && !string.IsNullOrEmpty(warehouses[0]))
            {
                whereClauseBuilder.Append(" AND warehouse in (" + String.Join(",", warehouses) + ")");
            }

            // Add radiologist id to the where clause if appropriate
            if (states != null && states.Count() > 0 && !string.IsNullOrEmpty(states[0]))
            {
                whereClauseBuilder.Append(" AND state in ('" + String.Join("','", states) + "')");
            }
            // Add Hour of the Day to the where clause if appropriate
            if (vendors != null && vendors.Count() > 0 && !string.IsNullOrEmpty(vendors[0]))
            {
                whereClauseBuilder.Append(" AND vendor in (" + String.Join(",", vendors) + ")");
            }

            sqlBuilder.Append(" where " + whereClauseBuilder);
            sqlBuilder.Append(" group by " + groupClauseBuilder);
            sqlBuilder.Append(" order by " + orderClauseBuilder);

            List<Dictionary<string, object>> dataResult = new List<Dictionary<string, object>>();
            DataTable dataTable = new DataTable();

            //// Execute the SQL query and get the results
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["SunriseConnectionString"].ConnectionString))
            {
                SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection);
                try
                {
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

            //allocate data
            //group based on different drill down level
            string expression = "";
            int seriaNumber = 0;
            switch (comparisonVariable)
            {
                //group by radiologist
                case (int)Helpers.Helpers.ComparisionType.Revenue:
                    for (int i = 0; i < 1; i++)
                    {
                        Dictionary<string, object> aSeries = new Dictionary<string, object>();
                        aSeries["data"] = getAxisLabel(drillDownLevel, start_date, end_date, startOfWeek, Helpers.Helpers.AxisType.yAxis);
                        aSeries["name"] = "revenue";
                        aSeries["data"] = signValueforSerie(dataTable, expression, resultSortOrder, drillDownLevel, (List<object[]>)aSeries["data"], dispalyNegative);
                        if (shouldNotRemoveSerie)
                        {
                            dataResult.Add(aSeries);
                        }
                    }
                    ViewData["chartTitle"] = "Revenue Comparision";
                    break;
                //group by radiologist
                case (int)Helpers.Helpers.ComparisionType.Sku:
                    List<dropdown_list> skuList = GetDropdownSKU();
                    if (skus != null && !string.IsNullOrEmpty(skus[0]))
                    {
                        skuList = GetList((int)Helpers.Helpers.ListType.allSku, skus);
                    }
                    seriaNumber = skuList.Count;
                    for (int i = 0; i < seriaNumber; i++)
                    {
                        Dictionary<string, object> aSeries = new Dictionary<string, object>();
                        aSeries["data"] = getAxisLabel(drillDownLevel, start_date, end_date, startOfWeek, Helpers.Helpers.AxisType.yAxis);
                        aSeries["name"] = skuList[i].description;
                        expression = "sku = '" + skuList[i].description + "'";
                        aSeries["data"] = signValueforSerie(dataTable, expression, resultSortOrder, drillDownLevel, (List<object[]>)aSeries["data"], dispalyNegative);
                        if (shouldNotRemoveSerie)
                        {
                            dataResult.Add(aSeries);
                        }
                    }
                    ViewData["chartTitle"] = "SKU Sales Comparision";
                    break;
                //group by procedure
                case (int)Helpers.Helpers.ComparisionType.State:
                    List<dropdown_list> stateList = GetDropdownStates();
                    if (states != null && !string.IsNullOrEmpty(states[0]))
                    {
                        stateList = GetList((int)Helpers.Helpers.ListType.allState, states);
                    }
                    seriaNumber = stateList.Count;
                    for (int i = 0; i < seriaNumber; i++)
                    {
                        Dictionary<string, object> aSeries = new Dictionary<string, object>();
                        aSeries["data"] = getAxisLabel(drillDownLevel, start_date, end_date, startOfWeek, Helpers.Helpers.AxisType.yAxis);
                        aSeries["name"] = stateList[i].description;
                        expression = "state = '" + stateList[i].abbre + "'";
                        aSeries["data"] = signValueforSerie(dataTable, expression, resultSortOrder, drillDownLevel, (List<object[]>)aSeries["data"], dispalyNegative);
                        if (shouldNotRemoveSerie)
                        {
                            dataResult.Add(aSeries);
                        }
                    }
                    ViewData["chartTitle"] = "State Sales Comparision";
                    break;
                //group by sites
                case (int)Helpers.Helpers.ComparisionType.Account:
                    List<dropdown_list> accountList = GetDropdownAccounts();
                    if (accounts != null && !string.IsNullOrEmpty(accounts[0]))
                    {
                        accountList = GetList((int)Helpers.Helpers.ListType.allEbayAccount, accounts);
                    }
                    seriaNumber = accountList.Count;
                    for (int i = 0; i < seriaNumber; i++)
                    {
                        Dictionary<string, object> aSeries = new Dictionary<string, object>();
                        aSeries["data"] = getAxisLabel(drillDownLevel, start_date, end_date, startOfWeek, Helpers.Helpers.AxisType.yAxis);
                        aSeries["name"] = accountList[i].description;
                        expression = "account = '" + accountList[i].description + "'";
                        aSeries["data"] = signValueforSerie(dataTable, expression, resultSortOrder, drillDownLevel, (List<object[]>)aSeries["data"], dispalyNegative);
                        if (shouldNotRemoveSerie)
                        {
                            dataResult.Add(aSeries);
                        }
                    }
                    ViewData["chartTitle"] = "Account Sales Comparision";
                    break;
                default:
                    break;
            }


            ViewData["yaxisdatatat"] = dataResult;

            ViewData["xaxislabels"] = getAxisLabel(drillDownLevel, start_date, end_date, startOfWeek, Helpers.Helpers.AxisType.xAxis).Select(x => x[0]).ToArray();

            ViewData["chartType"] = ((Helpers.Helpers.ChartType)(chartType)).ToString().ToLower();


            ViewBag.TableCaption = reporttitle + ": ";
            return PartialView("MapView"); // Data is returned in the ViewData objects!
        }

        public List<object[]> signValueforSerie(DataTable dataTable, string expression, string sortOrder, int drillDownLevel, List<object[]> aSeries, bool displayNegative)
        {
            DataRow[] foundRows = dataTable.Select(expression, sortOrder);
            DateTime calendarDate;
            double value;
            shouldNotRemoveSerie = false;

            // Print column 0 of each returned row.
            foreach (DataRow dr in foundRows)
            {
                string valueName = "";
                switch (drillDownLevel)
                {
                    //group by daily
                    case 1:
                        //query result is Date
                        calendarDate = DateTime.Parse(dr[0].ToString());
                        valueName = getLabelName(drillDownLevel.ToString(), calendarDate, 0, 0, 0, 0);
                        break;
                    //group by weekly
                    case 2:
                        //query result is week
                        valueName = getLabelName(drillDownLevel.ToString(), DateTime.Now, (Int32.Parse(dr[0].ToString()) + 1), 0, 0, 0);
                        break;
                    //group by monthly
                    case 3:
                        //query result is year, month
                        DateTime dt = new DateTime(Int32.Parse(dr[0].ToString()), Int32.Parse(dr[1].ToString()), 1);
                        valueName = getLabelName(drillDownLevel.ToString(), dt, 0, 0, 0, 0);
                        break;
                    //group by quarterly
                    case 4:
                        //query result is year, quarter
                        valueName = getLabelName(drillDownLevel.ToString(), DateTime.Now, 0, 0, (int)dr[1], (int)dr[0]);
                        break;
                }
                foreach (object[] defaultValue in aSeries)
                {
                    if (defaultValue[0].ToString().Equals(valueName))
                    {
                        if (drillDownLevel == 4 || drillDownLevel == 3)
                        {
                            value = double.Parse(dr[2].ToString());
                            if (!(displayNegative && value > 0))
                            {
                                defaultValue[1] = value;
                                shouldNotRemoveSerie = true;
                            }
                        }
                        else
                        {
                            value = double.Parse(dr[1].ToString());
                            if (!(displayNegative && value > 0))
                            {
                                defaultValue[1] = value;
                                shouldNotRemoveSerie = true;
                            }
                        }
                        break;

                    }
                }
            }
            return aSeries;
        }
        public List<object[]> getAxisLabel(int drillDownLevel, DateTime start_date, DateTime end_date, DateTime startOfWeek, Helpers.Helpers.AxisType axisType)
        {
            List<object[]> listData = new List<object[]>();
            switch (drillDownLevel)
            {
                //group by daily
                case 1:
                    while (start_date <= end_date)
                    {
                        object[] values = new object[2];
                        values[0] = getLabelName(drillDownLevel.ToString(), start_date, 0, 0, 0, 0);
                        values[1] = 0;
                        listData.Add(values);
                        start_date = start_date.AddDays(1);
                    }
                    break;
                //group by weekly
                case 2:
                    int startWeek = 1;
                    while (startOfWeek <= end_date)
                    {
                        object[] values = new object[2];
                        // pull out week
                        values[0] = getLabelName(drillDownLevel.ToString(), start_date, startWeek, 0, 0, 0);
                        values[1] = 0;
                        listData.Add(values);

                        startOfWeek = startOfWeek.AddDays(7);
                        startWeek += 1;
                    }
                    break;
                //group by monthly
                case 3:
                    start_date = new DateTime(start_date.Year, start_date.Month, 1);
                    while (start_date <= end_date)
                    {
                        object[] values = new object[2];
                        // pull out month and year
                        values[0] = getLabelName(drillDownLevel.ToString(), start_date, 0, start_date.Month, 0, start_date.Year);
                        values[1] = 0;
                        listData.Add(values);
                        start_date = start_date.AddMonths(1);
                    }
                    break;
                //group by quarterly
                case 4:
                    start_date = new DateTime(start_date.Year, start_date.Month, 1);
                    end_date = end_date;
                    while (start_date <= end_date)
                    {
                        object[] values = new object[2];
                        // pull out month and year
                        values[0] = getLabelName(drillDownLevel.ToString(), start_date, 0, 0, (int)Math.Ceiling(start_date.Month / 3.0), start_date.Year);
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

        public string getLabelName(string drillDownLevel, DateTime start_date, int week, int month, int quarter, int year)
        {
            switch (drillDownLevel)
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


    }

}