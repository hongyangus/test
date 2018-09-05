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
    public class ECommerceSaleForcastController : BaseController
    {
        private string reporttitle = "E Commerce Forcast"; // Specify the report title here

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
        public PartialViewResult ChartView(int comparisonVariable, int chartType, int drillDownLevel, string[] vendors, string[] skus, string[] accounts, string[] warehouses, string[] states, string startDate, string endDate, bool displayGreater10)
        {
            //get the first day of the week for the start date
            DateTime start_date = DateTime.Parse(startDate);
            DateTime end_date = DateTime.Parse(endDate);
            DateTime startOfWeek = start_date.AddDays(-(int)start_date.DayOfWeek);

            StringBuilder sqlBuilder = new StringBuilder();
            StringBuilder sqlBuilder2 = new StringBuilder();
            StringBuilder selectClauseBuilder = new StringBuilder();
            StringBuilder selectClauseBuilder2 = new StringBuilder();
            StringBuilder whereClauseBuilder = new StringBuilder();
            StringBuilder whereClauseBuilder2 = new StringBuilder();
            StringBuilder groupClauseBuilder = new StringBuilder();
            StringBuilder orderClauseBuilder = new StringBuilder();

            DateTime recentStartDate = DateTime .Now ;
            double totalPeriod = 0;
            //group based on different drill down level
            switch (drillDownLevel)
            {
                //group by daily
                case (int)Helpers.Helpers.DrilldownLevel.Daily:
                    totalPeriod = (end_date - start_date).TotalDays*1.0;
                    recentStartDate = end_date;
                    break;
                //group by weekly
                case (int)Helpers.Helpers.DrilldownLevel.Weekly:
                    totalPeriod = (end_date - start_date).TotalDays/7*1.0;
                    recentStartDate = end_date.AddDays (-7);
                    break;
                //group by monthly
                case (int)Helpers.Helpers.DrilldownLevel.Monthly:
                    totalPeriod = (end_date - start_date).TotalDays / 365.25*12;
                    recentStartDate = end_date.AddDays(-30);
                    break;
                case (int)Helpers.Helpers.DrilldownLevel.Quarterly :
                    //group by quarterly
                    totalPeriod = (end_date - start_date).TotalDays / 365.25 * 4;
                    recentStartDate = end_date.AddDays(-120);
                    break;
                default:
                    break;
            }

            selectClauseBuilder.Append("  sum(quantity)/" + String.Format("{0:0.00}", totalPeriod) +" as Volume");
            selectClauseBuilder2.Append("  sum(quantity)" + " as Volume");

            //group based on different comparision variable
            switch (comparisonVariable)
            {
                case (int)Helpers.Helpers.ComparisionType.Revenue:
                    break;
                case (int)Helpers.Helpers.ComparisionType.Sku:
                    selectClauseBuilder.Append(" , sku");
                    selectClauseBuilder2.Append(" , sku");
                    groupClauseBuilder.Append(" sku ");
                    orderClauseBuilder.Append(" sku ");
                    break;
                case (int)Helpers.Helpers.ComparisionType.Account:
                    selectClauseBuilder.Append(" , account");
                    selectClauseBuilder2.Append(" , account");
                    groupClauseBuilder.Append(" account");
                    orderClauseBuilder.Append(" account ");
                    break;
                case (int)Helpers.Helpers.ComparisionType.State:
                    selectClauseBuilder.Append("  , state");
                    selectClauseBuilder2.Append(" , state");
                    groupClauseBuilder.Append(" state");
                    orderClauseBuilder.Append(" state ");
                    break;
            }

            sqlBuilder.Append(" select");
            sqlBuilder.Append(selectClauseBuilder);
            sqlBuilder.Append(" FROM tblEbayOrder ");
            sqlBuilder2.Append(" select");
            sqlBuilder2.Append(selectClauseBuilder2);
            sqlBuilder2.Append(" FROM tblEbayOrder ");

            whereClauseBuilder.Append("  date>='" + start_date + "' and date<='" + end_date + "'");
            whereClauseBuilder2.Append("  date>='" + recentStartDate + "' and date<='" + end_date + "'");

            // Define the base SQL query 
            if (skus != null && skus.Count() > 0 && !string.IsNullOrEmpty(skus[0]))
            {
                whereClauseBuilder.Append(" AND sku in ('" + String.Join("','", skus) + "')");
                whereClauseBuilder2.Append(" AND sku in ('" + String.Join("','", skus) + "')");
            }

            // Add the procedure code(s) to the where clause if appropriate
            if (accounts != null && accounts.Count() > 0 && !string.IsNullOrEmpty(accounts[0]))
            {
                whereClauseBuilder.Append(" AND account in (" + String.Join(",", accounts) + ")");
                whereClauseBuilder2.Append(" AND sku in ('" + String.Join("','", skus) + "')");
            }

            // Add site id to the where clause if appropriate
            if (warehouses != null && warehouses.Count() > 0 && !string.IsNullOrEmpty(warehouses[0]))
            {
                whereClauseBuilder.Append(" AND warehouse in (" + String.Join(",", warehouses) + ")");
                whereClauseBuilder2.Append(" AND sku in ('" + String.Join("','", skus) + "')");
            }

            // Add radiologist id to the where clause if appropriate
            if (states != null && states.Count() > 0 && !string.IsNullOrEmpty(states[0]))
            {
                whereClauseBuilder.Append(" AND state in ('" + String.Join("','", states) + "')");
                whereClauseBuilder2.Append(" AND sku in ('" + String.Join("','", skus) + "')");
            }
            // Add Hour of the Day to the where clause if appropriate
            if (vendors != null && vendors.Count() > 0 && !string.IsNullOrEmpty(vendors[0]))
            {
                whereClauseBuilder.Append(" AND vendor in (" + String.Join(",", vendors) + ")");
                whereClauseBuilder2.Append(" AND sku in ('" + String.Join("','", skus) + "')");
            }

            sqlBuilder.Append(" where " + whereClauseBuilder);
            sqlBuilder.Append(" group by " + groupClauseBuilder);
            sqlBuilder.Append(" order by " + orderClauseBuilder);
            sqlBuilder2.Append(" where " + whereClauseBuilder2);
            sqlBuilder2.Append(" group by " + groupClauseBuilder);
            sqlBuilder2.Append(" order by " + orderClauseBuilder);

            List<Dictionary<string, object>> dataResult = new List<Dictionary<string, object>>();
            DataTable dataTable1 = new DataTable();
            DataTable dataTable2 = new DataTable();

            //// Execute the SQL query and get the results
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["SunriseConnectionString"].ConnectionString))
            {
                SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection);
                try
                {
                    connection.Open();
                    SqlDataAdapter da = new SqlDataAdapter(command);
                    da.Fill(dataTable1);
                    command.CommandText = sqlBuilder2.ToString();
                    da.Fill(dataTable2);
                    connection.Close();
                }
                catch (Exception ex)
                {
                    string exms = ex.Message;
                }
            }

            ViewData["chartTitle"] = "Sales Forcasting";
            List<object[]> xaisList = new List<object[]>();
            switch (comparisonVariable)
            {
                //group by radiologist
                case (int)Helpers.Helpers.ComparisionType.Sku:
                    List<dropdown_list> skuList = GetDropdownSKU();
                    if (skus != null && !string.IsNullOrEmpty(skus[0]))
                    {
                        skuList = GetList((int)Helpers.Helpers.ListType.allSku, skus);
                    }
                    xaisList = getAxisLabel(skuList, Helpers.Helpers.AxisType.xAxis);
                    break;
                //group by procedure
                case (int)Helpers.Helpers.ComparisionType.State:
                    List<dropdown_list> stateList = GetDropdownStates();
                    if (states != null && !string.IsNullOrEmpty(states[0]))
                    {
                        stateList = GetList((int)Helpers.Helpers.ListType.allState, states);
                    }
                    xaisList = getAxisLabel(stateList, Helpers.Helpers.AxisType.xAxis);
                    break;
                //group by sites
                case (int)Helpers.Helpers.ComparisionType.Account:
                    List<dropdown_list> accountList = GetDropdownAccounts();
                    if (accounts != null && !string.IsNullOrEmpty(accounts[0]))
                    {
                        accountList = GetList((int)Helpers.Helpers.ListType.allEbayAccount, accounts);
                    }
                    xaisList = getAxisLabel(accountList, Helpers.Helpers.AxisType.xAxis);
                    break;
                case (int)Helpers.Helpers.ComparisionType.Warehouse:
                    List<dropdown_list> warehouseList = GetDropdownWarehouse();
                    if (warehouses != null && !string.IsNullOrEmpty(warehouses[0]))
                    {
                        warehouseList = GetList((int)Helpers.Helpers.ListType.allWarehouse, warehouses);
                    }
                    xaisList = getAxisLabel(warehouseList, Helpers.Helpers.AxisType.xAxis);
                    break;
                default:
                    break;
            }


            Dictionary<string, object> aSeries1 = new Dictionary<string, object>();
            Dictionary<string, object> aSeries2 = new Dictionary<string, object>();
            ViewData["xaxislabels"] = xaisList.Select(x => x[0]).ToArray();
            aSeries1["name"] = "Forcasting";
            aSeries1["data"] = signValueforSerie(dataTable1, xaisList, displayGreater10);
            dataResult.Add(aSeries1);
            aSeries2["name"] = "Recent sale";
            aSeries2["data"] = signValueforSerie(dataTable2, xaisList, displayGreater10);
            dataResult.Add(aSeries2);
            ViewData["chartTitle"] = "SKU Sales Comparision";

            if (displayGreater10 )
            {
                dataResult = displayGreaterThan10(dataResult);
            }

            ViewData["yaxisdatatat"] = dataResult;


            ViewData["chartType"] = ((Helpers.Helpers.ChartType)(chartType)).ToString().ToLower();

            ViewData["chartTitle"] = "Sales forcasting";

            ViewBag.TableCaption = reporttitle + ": ";
            return PartialView("ChartView"); // Data is returned in the ViewData objects!
        }

        public List<object[]> signValueforSerie(DataTable dataTable, List<object[]> aSeries, bool displayNegative)
        {
            List<object[]> newAseries = new List<object[]>();
            // Print column 0 of each returned row.

            foreach (object[] defaultValue in aSeries)
            {
                object[] newValue = new object[2];
                newValue[0] = defaultValue[0].ToString ();
                string valueName = defaultValue[0].ToString ();
                newValue[1] = 0;
                foreach (DataRow dr in dataTable.Rows)
                {
                    if (dr[1].ToString().Equals(valueName))
                    {
                        newValue[1] = double.Parse(dr[0].ToString());
                        break;

                    }
                }
                newAseries.Add(newValue);
            }
            return newAseries;
        }
        public List<object[]> getAxisLabel(List<dropdown_list> skus, Helpers.Helpers.AxisType axisType)
        {
            List<object[]> listData = new List<object[]>();
            foreach  (dropdown_list s in skus)
            {
                object[] values = new object[2];
                values[0] = s.description;
                values[1] = 0;
                listData.Add(values);
            }
            return listData;
        }

        public List<Dictionary<string, object>> displayGreaterThan10(List<Dictionary<string, object>> dataResult)
        {
            List<object[]> list1 = (List<object[]>)(dataResult[0]["data"]);
            List< object[] > list2 = (List<object[]> )dataResult[1]["data"];
            for (int i = 0; i < list1.Count; i++) 
            {
                object[] defaultValue = list1[i];
                for (int j = 0;  j < list2.Count; j++)
                {
                    object[] defaultValue2 = list2[j];
                    if (defaultValue[0].ToString().Equals(defaultValue2[0]))
                    {
                        double value1 = double.Parse(defaultValue[1].ToString());
                        double value2 = double.Parse(defaultValue2[1].ToString());
                        if (Math.Min(value1, value2) != 0 && (Math.Min(value1, value2) / Math.Max(value1, value2)) >= 0.9)
                        {
                            list1.RemoveAt(i);
                            list2.RemoveAt(j);
                            i--;
                            j--;
                            break;
                        }

                    }
                }
            }
            dataResult[0]["data"] = list1;
            dataResult[1]["data"] = list2;
            return dataResult;
        }

    }
}
