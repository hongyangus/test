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
    public class ECommerceOrderController : BaseController
    {
        private string reporttitle = "E Commerce Order"; // Specify the report title here

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Index()
        {
            // Enable security to redirect to login page if user is not logged in or we are not running in the VS IDE
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            // Save the report title to the ViewBag
            ViewBag.ReportTitle = reporttitle;

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
        public PartialViewResult LoadOrders(string startDate, string endDate)
        {
            DateTime start_date = DateTime.Parse(startDate);
            DateTime end_date = DateTime.Parse(endDate);

            string url = "http://101.95.137.138:88/api/Profit/auto/" + start_date.ToString("yyyy-MM-dd") + "to" + end_date.ToString("yyyy-MM-dd") + "/all/all";
            List<SaleOrder> orders = null;
            HttpWebRequest webRequest;
            webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.CookieContainer = new CookieContainer();
            webRequest.Method = "GET";
            webRequest.ContentType = "application/json;charset=UTF-8";
            WebResponse response;
            using (response = webRequest.GetResponse())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(SaleOrders));
                SaleOrders saleOrder = (SaleOrders)serializer.ReadObject(response.GetResponseStream());
                orders = saleOrder.orders;
            }
            if (orders != null && orders.Count > 0)
            {
                SqlConnection sqlConn = new SqlConnection(ConfigurationManager.ConnectionStrings["SunriseConnectionString"].ConnectionString);
                SqlCommand cmd = sqlConn.CreateCommand();

                try
                {
                    sqlConn.Open();
                    //loop through all tenant to create rent record for this month
                    foreach (SaleOrder order in orders)
                    {
                        try
                        {
                            cmd.CommandText = "insert into tblEbayOrder(ordernumber, account, sku, title, quantity, cost, sales, ebayFee, paypalFee, shippingFee,internationalShippingFee, date, profit, pieces, discount, name, state, storeId) "
                                + " values ("
                                + "" + order.ebay_id + ","
                                + "'" + order.account  + "',"
                                + "'" + order.sku + "',"
                                + "'" + order.title + "',"
                                + "" + order.quantity + ","
                                + "" + order.cost.ToString("0.00") + ","
                                + "" + order.sales.ToString("0.00") + ","
                                + "" + order.ebayFee.ToString("0.00") + ","
                                + "" + order.paypalFee.ToString("0.00") + ","
                                + "" + order.shippingFee.ToString("0.00") + ","
                                + "" + order.internationalShippingFee.ToString("0.00") + ","
                                + "'" + order.date + "',"
                                + "" + order.profit.ToString("0.00") + ","
                                + "" + order.pieces + ","
                                + "" + order.discount + ","
                                + "'" + order.name + "',"
                                + "'" + order.state + "',"
                                + "" + 101 + ")";

                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    sqlConn.Close();
                }
                catch (SqlException ex)
                {
                    ViewBag.MyExeption = ex.Message;
                    ViewBag.MyExeptionCSS = "errorMessage";
                }
                finally
                {
                    sqlConn.Close();
                }
            }
            return PartialView();

        }

        [AllowAnonymous]
        [HttpPost]
        public PartialViewResult ReportView(string[] vendors, string[] skus, string[] accounts, string[] warehouses, string[] states, string startDate, string endDate, bool dispalyNegative)
        {
            //get the first day of the week for the start date
            DateTime start_date = DateTime.Parse(startDate);
            DateTime end_date = DateTime.Parse(endDate);
            DateTime startOfWeek = start_date.AddDays(-(int)start_date.DayOfWeek);

            StringBuilder sqlBuilder = new StringBuilder();
            StringBuilder selectClauseBuilder = new StringBuilder();
            StringBuilder whereClauseBuilder = new StringBuilder();
            
            sqlBuilder.Append(" select *  FROM tblEbayOrder ");

            if(start_date != null)
            {
                whereClauseBuilder.Append(" date>= '" + start_date + "'");
            }
            if (end_date != null)
            {
                whereClauseBuilder.Append(" AND date<= '" + end_date + "'");
            }

            // Define the base SQL query 
            if (skus != null && skus.Count() > 0 && !string.IsNullOrEmpty(skus[0]))
            {
                whereClauseBuilder.Append(" AND sku in ('" + String.Join("','", skus) + "')");
            }

            // Add the procedure code(s) to the where clause if appropriate
            if (accounts != null && accounts.Count() > 0 && !string.IsNullOrEmpty(accounts[0]))
            {
                whereClauseBuilder.Append(" AND account in ('" + String.Join("','", accounts) + "')");
            }

            // Add site id to the where clause if appropriate
            if (warehouses != null && warehouses.Count() > 0 && !string.IsNullOrEmpty(warehouses[0]))
            {
                whereClauseBuilder.Append(" AND warehouse in ('" + String.Join("','", warehouses) + "')");
            }

            // Add radiologist id to the where clause if appropriate
            if (states != null && states.Count() > 0 && !string.IsNullOrEmpty(states[0]))
            {
                whereClauseBuilder.Append(" AND state in ('" + String.Join("','", states) + "')");
            }
            // Add Hour of the Day to the where clause if appropriate
            if (vendors != null && vendors.Count() > 0 && !string.IsNullOrEmpty(vendors[0]))
            {
                whereClauseBuilder.Append(" AND vendor in ('" + String.Join("','", vendors) + "')");
            }
            if(dispalyNegative)
            {
                whereClauseBuilder.Append(" AND profit < = 0 ");
            }

            sqlBuilder.Append(" where " + whereClauseBuilder);
            sqlBuilder.Append(" order by date, SKU" );

            List<Dictionary<string, object>> dataResult = new List<Dictionary<string, object>>();
            List<SaleOrder> result = new List<SaleOrder>();

            //// Execute the SQL query and get the results
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["SunriseConnectionString"].ConnectionString))
            {
                SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection);
                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read()) // Read each result row and extract the data
                    {
                        SaleOrder row = new SaleOrder();
                        row.ebay_id = reader[0].ToString();
                        row.sku = reader[1].ToString();
                        row.quantity = Int32.Parse(reader[3].ToString());
                        row.cost = double.Parse (reader[4].ToString());
                        row.sales = double.Parse(reader[5].ToString());
                        row.ebayFee = double.Parse(reader[6].ToString());
                        row.paypalFee = double.Parse(reader[7].ToString());
                        row.shippingFee  = double.Parse(reader[8].ToString());
                        row.internationalShippingFee  = double.Parse(reader[9].ToString());
                        row.orderDate  =DateTime .Parse ( reader[10].ToString());
                        row.profit = double.Parse(reader[11].ToString());
                        row.pieces = Int32.Parse(reader[12].ToString());
                        row.discount  = Int32.Parse(reader[13].ToString());
                        row.name = reader[14].ToString();
                        row.state = reader[15].ToString();
                        row.storeId  = reader[16].ToString();
                        result.Add(row);
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    //display the exception message in viewbag
                    ViewBag.MyExeption = ex.Message;
                    ViewBag.MyExeptionCSS = "errorMessage";
                }
            }

            

            ViewBag.TableCaption = reporttitle + ": ";
            return PartialView("ReportView", result); // Data is returned in the ViewData objects!
        }


        [AllowAnonymous]
        public ActionResult Edit(int number)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Edit Order";

            var model = getOrder(number, "");
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Edit(PropertyManagement.Controllers.SaleOrder model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            model.ebay_id  = Request.Params["ebay_id"];
            model.sku = Request.Params["sku"].ToString ().Split (',')[0];
            model.quantity = int.Parse(Request.Params["quantity"].ToString ());
            model.cost = double.Parse(Request.Params["cost"].ToString());
            model.sales = double.Parse(Request.Params["sales"].ToString());
            model.ebayFee = double.Parse(Request.Params["ebayFee"].ToString());
            model.paypalFee = double.Parse(Request.Params["paypalFee"].ToString());
            model.shippingFee = double.Parse(Request.Params["shippingFee"].ToString());
            model.internationalShippingFee = double.Parse(Request.Params["internationalShippingFee"].ToString());
            model.orderDate = DateTime .Parse(Request.Params["orderDate"].ToString());
            model.profit = double.Parse(Request.Params["profit"].ToString());
            model.pieces = int.Parse(Request.Params["pieces"].ToString());
            model.discount = int.Parse(Request.Params["discount"].ToString());
            model.name = Request.Params["name"].ToString();
            model.state = Request.Params["state"].ToString();
            model.storeId = Request.Params["storeId"].ToString();
            updateOrder(model);
            return RedirectToAction("Index");
        }

        private SaleOrder getOrder(int number, string sku)
        { 
            //// Execute the SQL query and get the results
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["SunriseConnectionString"].ConnectionString))
            {
                SaleOrder row = new SaleOrder();
                string sql = "select *  FROM tblEbayOrder where orderNumber = " + number;
                    //+ " and sku = '" + sku + "'";
                SqlCommand command = new SqlCommand(sql, connection);
                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read()) // Read each result row and extract the data
                    {
                        row.ebay_id = reader[0].ToString();
                        row.sku = reader[1].ToString();
                        row.quantity = Int32.Parse(reader[3].ToString());
                        row.cost = double.Parse(reader[4].ToString());
                        row.sales = double.Parse(reader[5].ToString());
                        row.ebayFee = double.Parse(reader[6].ToString());
                        row.paypalFee = double.Parse(reader[7].ToString());
                        row.shippingFee = double.Parse(reader[8].ToString());
                        row.internationalShippingFee = double.Parse(reader[9].ToString());
                        row.orderDate = DateTime.Parse(reader[10].ToString());
                        row.profit = double.Parse(reader[11].ToString());
                        row.pieces = Int32.Parse(reader[12].ToString());
                        row.discount = Int32.Parse(reader[13].ToString());
                        row.name = reader[14].ToString();
                        row.state = reader[15].ToString();
                        row.storeId = reader[16].ToString();
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    //display the exception message in viewbag
                    ViewBag.MyExeption = ex.Message;
                    ViewBag.MyExeptionCSS = "errorMessage";
                }
                return row;
            }

        }

        private void updateOrder(SaleOrder model)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                if(string.IsNullOrEmpty (model.storeId  ))
                {
                    model.storeId = "0";
                }
                // Create the Command and Parameter objects.
                SqlCommand cmd = new SqlCommand("", connection);
                connection.Open();
                cmd.CommandText = "Update tblEbayOrder set "
                     + " account = '" + model.account + "' , "
                     + " sku = '" + model.sku + "' , "
                     + " quantity = " + model.quantity + " , "
                     + " cost = " + model.cost + " , "
                     + " sales = " + model.sales + " , "
                     + " ebayFee = " + model.ebayFee + " , "
                     + " paypalFee = " + model.paypalFee + " , "
                     + " shippingFee = " + model.shippingFee + " , "
                     + " internationalShippingFee = " + model.internationalShippingFee + " , "
                     + " Date = '" + model.orderDate + "' , "
                     + " profit = " + model.profit + " , "
                     + " pieces = " + model.pieces + " , "
                     + " discount = " + model.discount + " , "
                     + " name = '" + model.name + "' , "
                     + " state = '" + model.state + "' , "
                     + " storeId = " + model.storeId + " "
                     + " where orderNumber = " + model.ebay_id
                     + " and sku='" + model.sku + "'";
                cmd.ExecuteNonQuery();
                connection.Close();
            }

        }


    }

    [DataContract]
    public class SaleOrder
    {
        [DataMember]
        public string ebay_id;

        [DataMember]
        public string account;

        [DataMember]
        public string sku;

        [DataMember]
        public string title;

        [DataMember]
        public int quantity;

        [DataMember]
        public double cost;

        [DataMember]
        public double sales;

        [DataMember]
        public double ebayFee;

        [DataMember]
        public double paypalFee;

        [DataMember]
        public double shippingFee;

        [DataMember]
        public double internationalShippingFee;

        [DataMember]
        public string  date;

        [DataMember]
        public double profit;

        [DataMember]
        public int pieces;

        [DataMember]
        public int discount;

        [DataMember]
        public string name;

        [DataMember]
        public string state;

        public string storeId;
        public DateTime  orderDate;

    }

    [DataContract]
    public class SaleOrders
    {

        [DataMember]
        public string filterType;

        [DataMember]
        public string account;

        [DataMember]
        public string sku;

        [DataMember]
        public List<SaleOrder> orders;




    }
}