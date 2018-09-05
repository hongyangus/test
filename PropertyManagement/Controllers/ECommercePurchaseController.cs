using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.Data;
using MySql.Data.MySqlClient;
using OfficeOpenXml;
using ship;

namespace PropertyManagement.Controllers
{
    [Authorize]
    public class ECommercePurchaseController : BaseController
    {
        private string reporttitle = "E-Commerce Purchase";
        private double oneContainerCost = 5500;

        [AllowAnonymous]
        public ActionResult Index()
        {
            // Enable security to redirect to login page if user is not logged in or we are not running in the VS IDE
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            // Save the report title to the ViewBag
            ViewBag.ReportTitle = reporttitle;

            var companies = GetList((short)Helpers.Helpers.ListType.allECommerceCompany);
            ViewBag.companies = new MultiSelectList(companies, "id", "description");

            var warehouses = GetList((short)Helpers.Helpers.ListType.allWarehouse);
            ViewBag.warehouses = new MultiSelectList(warehouses, "id", "description");

            var statusList = GetList((short)Helpers.Helpers.ListType.allECommercePurchaseStatus);
            ViewBag.statusList = new MultiSelectList(statusList, "id", "description");

            var purchasePlanList = GetList((short)Helpers.Helpers.ListType.allECommercePurchasePlan);
            ViewBag.purchasePlanList = new MultiSelectList(purchasePlanList, "id", "description");

            //setup default value of the start date and end date
            if (Session["startDate"] == null)
            {
                DateTime oneMonth = DateTime.Now.AddMonths(-1);
                Session["startDate"] = new DateTime(oneMonth.Year, oneMonth.Month, 1).ToString("MM/dd/yyyy");
                Session["endDate"] = DateTime.Now.ToString("MM/dd/yyyy");
                string[] statusIDs = new string[] { ((int)Helpers.Helpers.StatusType.Open).ToString() };
                Session["selectedStatusIDs"] = statusIDs;
            }

            return View();
        }


        [AllowAnonymous]
        public PartialViewResult ReportView(string startDate, string endDate, string[] companyIDs, string[] WarehouseIDs, string[] purchasePlanIDs, string[] statusIDs)
        {
            Session["startDate"] = startDate;
            Session["endDate"] = endDate;
            Session["selectedCompanyIDs"] = companyIDs;
            Session["selectedWarehouseIDs"] = WarehouseIDs;
            Session["selectedECommerceStatusIDs"] = statusIDs;
            Session["selectedECommercePlanIDs"] = purchasePlanIDs;

            DateTime start = DateTime.Parse(startDate);
            DateTime end = DateTime.Parse(endDate);
            StringBuilder sbOperation = new StringBuilder();
            sbOperation.Append("Select distinct purchase_order.*, tblCompany.CompanyName, c_status.name, ebay_store.store_name  from purchase_order ");
            sbOperation.Append(" left outer JOIN tblCompany on tblCompany.CompanyID =  purchase_order.PurchaseCompanyID ");
            sbOperation.Append(" INNER JOIN c_status on c_status.statusID = purchase_order.statusID ");
            sbOperation.Append(" LEFT OUTER JOIN ebay_store on ebay_store.id = purchase_order.io_warehouse ");
            sbOperation.Append(" where updateDate>='" + start.ToString("yyyy-MM-dd") + "' ");
            sbOperation.Append(" and updateDate<='" + end.ToString("yyyy-MM-dd") + "'");

            // Add modality id to the where clause if appropriate
            if (companyIDs != null && companyIDs.Count() > 0 && !string.IsNullOrEmpty(companyIDs[0]))
            {
                sbOperation.Append(" AND tblCompany.CompanyID IN (" + String.Join(",", companyIDs) + ")");
            }
            else
            {
                //get the companys only the owner can access
                sbOperation.Append(" AND tblCompany.CompanyID IN (" + GetUserManagedCompanyString() + ")");
            }
            // Add modality id to the where clause if appropriate
            if (WarehouseIDs != null && WarehouseIDs.Count() > 0 && !string.IsNullOrEmpty(WarehouseIDs[0]))
            {
                sbOperation.Append(" AND ebay_store.id IN (" + String.Join(",", WarehouseIDs) + ")");
            }
            if (statusIDs != null && statusIDs.Count() > 0 && !string.IsNullOrEmpty(statusIDs[0]))
            {
                sbOperation.Append(" AND c_status.StatusID IN (" + String.Join(",", statusIDs) + ")");
            }
            //sbOperation.Append(" Order by tblRent.DueDate, tblRent.PaymentDate");

            List<OrderPurchase> result = new List<OrderPurchase>();

            using (MySqlDataAdapter adapter = new MySqlDataAdapter(sbOperation.ToString(), new MySqlConnection(Helpers.Helpers.GetERPConnectionString())))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                if (tb != null && tb.Rows.Count > 0)
                {
                    for (int i = 0; i < tb.Rows.Count; i++)
                    {
                        DataRow dr = tb.Rows[i];
                        OrderPurchase row = new OrderPurchase();
                        row = OrderPurchase.fillInObject(dr);
                        result.Add(row);
                    }
                }
            }

            return PartialView("ReportView", result);
        }


        [AllowAnonymous]
        public PartialViewResult DetailTableView(string tableid, string id)
        {
            // Define the base SQL query 
            string SQLString = "SELECT tblorder_event_queue.*, c_status.name FROM tblorder_event_queue, c_status WHERE tblorder_event_queue.EventID = c_status.StatusID and tblorder_event_queue.OrderID IN (" + id + ") and tblorder_event_queue.CategoryID=2";

            // Add the group by clause
            SQLString += " ORDER BY tblorder_event_queue.UpdateDate";

            // Create a list of our result class to hold the data from the query
            // Please ensure you instatiate the class for this controller and not a different controller
            List<PurchaseEventQuene> result = new List<PurchaseEventQuene>();

            // Execute the SQL query and get the results
            using (MySqlDataAdapter adapter = new MySqlDataAdapter(SQLString, new MySqlConnection(Helpers.Helpers.GetERPConnectionString())))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                if (tb != null && tb.Rows.Count > 0)
                {
                    for (int i = 0; i < tb.Rows.Count; i++)
                    {
                        DataRow dr = tb.Rows[i];
                        result.Add(PurchaseEventQuene.fillInObject(dr));
                    }
                }

            }
            ViewBag.tableid = tableid;
            return PartialView("DetailTableView", result);
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult UploadForMarketAnalysis()
        {
            // add new user
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Product Analysis";

            OrderPurchase model = new OrderPurchase();
            model.AllWarehouse = GetSelectListItems((short)Helpers.Helpers.ListType.allWarehouse);
            model.AllCompany = GetSelectListItems((short)Helpers.Helpers.ListType.allECommerceCompany);
            var warehouses = GetList((short)Helpers.Helpers.ListType.allWarehouse);
            ViewBag.warehouses = new MultiSelectList(warehouses, "id", "description");

            return View(model);
        }
        
        [AllowAnonymous]
        public PartialViewResult MarketAnalyze(string startDate, string endDate, string[] companyIDs, string[] WarehouseIDs, string[] purchasePlanIDs, string[] statusIDs)
        {
            Session["startDate"] = startDate;
            Session["endDate"] = endDate;
            Session["selectedCompanyIDs"] = companyIDs;
            Session["selectedWarehouseIDs"] = WarehouseIDs;
            Session["selectedECommerceStatusIDs"] = statusIDs;
            Session["selectedECommercePlanIDs"] = purchasePlanIDs;

            DateTime start = DateTime.Parse(startDate);
            DateTime end = DateTime.Parse(endDate);
            StringBuilder sbOperation = new StringBuilder();
            sbOperation.Append("Select distinct purchase_order.*, tblCompany.CompanyName, c_status.name, ebay_store.store_name  from purchase_order ");
            sbOperation.Append(" left outer JOIN tblCompany on tblCompany.CompanyID =  purchase_order.PurchaseCompanyID ");
            sbOperation.Append(" INNER JOIN c_status on c_status.statusID = purchase_order.statusID ");
            sbOperation.Append(" LEFT OUTER JOIN ebay_store on ebay_store.id = purchase_order.io_warehouse ");
            sbOperation.Append(" where updateDate>='" + start.ToString("yyyy-MM-dd") + "' ");
            sbOperation.Append(" and updateDate<='" + end.ToString("yyyy-MM-dd") + "'");

            // Add modality id to the where clause if appropriate
            if (companyIDs != null && companyIDs.Count() > 0 && !string.IsNullOrEmpty(companyIDs[0]))
            {
                sbOperation.Append(" AND tblCompany.CompanyID IN (" + String.Join(",", companyIDs) + ")");
            }
            else
            {
                //get the companys only the owner can access
                sbOperation.Append(" AND tblCompany.CompanyID IN (" + GetUserManagedCompanyString() + ")");
            }
            // Add modality id to the where clause if appropriate
            if (WarehouseIDs != null && WarehouseIDs.Count() > 0 && !string.IsNullOrEmpty(WarehouseIDs[0]))
            {
                sbOperation.Append(" AND ebay_store.id IN (" + String.Join(",", WarehouseIDs) + ")");
            }
            if (statusIDs != null && statusIDs.Count() > 0 && !string.IsNullOrEmpty(statusIDs[0]))
            {
                sbOperation.Append(" AND c_status.StatusID IN (" + String.Join(",", statusIDs) + ")");
            }
            //sbOperation.Append(" Order by tblRent.DueDate, tblRent.PaymentDate");

            List<OrderPurchase> result = new List<OrderPurchase>();

            using (MySqlDataAdapter adapter = new MySqlDataAdapter(sbOperation.ToString(), new MySqlConnection(Helpers.Helpers.GetERPConnectionString())))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                if (tb != null && tb.Rows.Count > 0)
                {
                    for (int i = 0; i < tb.Rows.Count; i++)
                    {
                        DataRow dr = tb.Rows[i];
                        OrderPurchase row = new OrderPurchase();
                        row = OrderPurchase.fillInObject(dr);
                        result.Add(row);
                    }
                }
            }
            result = FillInPurchaseOrderMarketAnalysis(result);
            return PartialView("MarketAnalyze", result);
        }


        [AllowAnonymous]
        public ActionResult Upload(FormCollection formCollection)
        {
            var companies = GetList((short)Helpers.Helpers.ListType.allECommerceCompany);
            ViewBag.companies = new MultiSelectList(companies, "id", "description");
            long compnayID = companies[0].id;
            List<OrderPurchase> orderList = new List<OrderPurchase>();
            if (Request != null)
            {
                HttpPostedFileBase file = Request.Files["UploadedFile"];
                if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
                {
                    string fileName = file.FileName;
                    string fileContentType = file.ContentType;
                    byte[] fileBytes = new byte[file.ContentLength];
                    var data = file.InputStream.Read(fileBytes, 0, Convert.ToInt32(file.ContentLength));
                    int warehouseID = Int32.Parse(formCollection["warehouseID"]);
                    using (var package = new ExcelPackage(file.InputStream))
                    {
                        ExcelWorksheets currentSheet = package.Workbook.Worksheets;
                        for (int i = 1; i < currentSheet.Count +1; i++)
                        {
                            ExcelWorksheet workSheet = currentSheet[i];
                            var noOfCol = workSheet.Dimension.End.Column;
                            var noOfRow = workSheet.Dimension.End.Row;
                            for (int rowIterator = 2; rowIterator <= noOfRow; rowIterator++)
                            {
                                var order = new OrderPurchase();
                                order.sku = (string)workSheet.Cells[rowIterator, 1].Value;
                                order.io_note = (string)workSheet.Cells[rowIterator, 2].Value;
                                order.UnitPurchasePrice = (double)workSheet.Cells[rowIterator, 3].Value;
                                order.io_partner = (string)workSheet.Cells[rowIterator, 4].Value;
                                order.length = ((double)workSheet.Cells[rowIterator, 5].Value)* 0.393701;
                                order.width = ((double)workSheet.Cells[rowIterator, 6].Value)*0.393701;
                                order.height = ((double)workSheet.Cells[rowIterator, 7].Value)*0.393701;
                                order.weight = ((double)workSheet.Cells[rowIterator, 8].Value)*35.274;
                                order.customRate = (double)workSheet.Cells[rowIterator, 10].Value;
                                order.statusID = 1;
                                order.PurchaseCompanyID = (int)compnayID;
                                order.order_date = DateTime.Now;
                                order.io_warehouse = warehouseID.ToString ();
                                order.io_status = "1";
                                order.updateDate = DateTime.Now;
                                orderList.Add(order);
                            }
                        }
                    }

                    MySqlConnection conn = new MySqlConnection(Helpers.Helpers.GetERPConnectionString());
                    try
                    {
                        conn.Open();
                        MySqlCommand cmd = new MySqlCommand("", conn);
                        MySqlDataReader rdr;
                        //loop through and insert new shipping rate
                        for (int i = 0; i < orderList.Count; i++)
                        {
                            OrderPurchase order = orderList[i];
                            cmd.CommandText = GetAddOrderPurchaseString(order);
                            rdr = cmd.ExecuteReader();
                            rdr.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                    conn.Close();
                }

            }

            var warehouses = GetList((short)Helpers.Helpers.ListType.allWarehouse);
            ViewBag.warehouses = new MultiSelectList(warehouses, "id", "description");

            var statusList = GetList((short)Helpers.Helpers.ListType.allECommercePurchaseStatus);
            ViewBag.statusList = new MultiSelectList(statusList, "id", "description");

            var purchasePlanList = GetList((short)Helpers.Helpers.ListType.allECommercePurchasePlan);
            ViewBag.purchasePlanList = new MultiSelectList(purchasePlanList, "id", "description");
            return View("Index");
        }
        private List<OrderPurchase> FillInPurchaseOrderMarketAnalysis(List<OrderPurchase> orderList)
        {
            //do the estimate
            for (int i = 0; i < orderList.Count; i++)
            {
                OrderPurchase order = orderList[i];
                order.domesticShipping = getDomesticShippingCost(order);
                order.internationalShipping = order.length * order.width * order.height / (2714 * 12 * 12 * 12) * oneContainerCost;
                order.custom = order.UnitPurchasePrice * order.customRate;
                order.handlingfee = 1.2;
                order.packingfee = 1.2;
                if (!string.IsNullOrEmpty(order.sku) && order.sku.ToLower().IndexOf("02rsa") > 0)
                {
                    order.handlingfee += 4.0;
                }
                orderList[i] = order;
            }
            return orderList;
        }


        [HttpGet]
        [AllowAnonymous]
        public ActionResult AddNewPurchase()
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Add Purchase Order";
            OrderPurchase model = new OrderPurchase();
            model.order_date = DateTime.Now;
            model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allECommercePurchaseStatus);
            model.AllSku = GetSelectListItems((short)Helpers.Helpers.ListType.allSku);
            model.AllVendor = GetSelectListItems((short)Helpers.Helpers.ListType.allECommerceVendor);
            model.AllWarehouse = GetSelectListItems((short)Helpers.Helpers.ListType.allWarehouse);
            model.AllCompany = GetSelectListItems((short)Helpers.Helpers.ListType.allECommerceCompany);
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult AddNewPurchase(OrderPurchase model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Add Purchase Order";

            AddOrderPurchase(model);
            return RedirectToAction("Index");
        }


        [AllowAnonymous]
        public ActionResult Edit(int id)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Edit Ecommerce account";

            OrderPurchase model = GetPurchaseOrderByID(id);

            //model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);
            //model.AllCompany = GetSelectListItems((short)Helpers.Helpers.ListType.company);
            //model.AllAdmin = GetSelectListItems((short)Helpers.Helpers.ListType.allUser);

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Edit(OrderPurchase model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Edit Purchase Order";

            EditPurchaseOrder(model);
            return RedirectToAction("Index");
        }

        public List<OrderPurchase> GetEcommerceAccountByCompanyIDs(string[] companyIDs, int userID)
        {
            string SQLString = "select ebay_account.*, tblCompanyebayaccount.companyebayaccountid,tblCompanyebayaccount.AdminID,tblCompanyebayaccount.StatusID, tblCompany.companyname, c_User.FirstName, c_User.LastName from ebay_account,tblCompanyebayaccount,tblCompany,c_user ";
            SQLString += " where  ebay_account.id=tblCompanyebayaccount.ebayAccountID and tblcompany.companyid=tblCompanyebayaccount.companyid and c_user.UserID=tblCompanyebayaccount.AdminId";
            if (companyIDs != null && companyIDs.Length > 0 && !string.IsNullOrEmpty(companyIDs[0]))
            {
                SQLString += " AND tblCompanyebayaccount.CompanyID in  (" + String.Join(",", companyIDs) + ")";
            }
            else
            {
                //get the companys only the owner can access
                SQLString += " AND tblCompanyebayaccount.CompanyID IN (" + Helpers.Helpers.GetUserManagedCompanyString(userID.ToString()) + ")";
            }

            using (MySqlDataAdapter adapter = new MySqlDataAdapter(SQLString, new MySqlConnection(Helpers.Helpers.GetERPConnectionString())))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                DataTable tb = (DataTable)ds.Tables[0];
                List<OrderPurchase> allECommerceAccounts = new List<OrderPurchase>();
                if (tb != null && tb.Rows.Count > 0)
                {
                    for (int i = 0; i < tb.Rows.Count; i++)
                    {
                        DataRow dr = tb.Rows[i];
                        allECommerceAccounts.Add(OrderPurchase.fillInObject(dr));
                    }
                }
                return allECommerceAccounts;
            }
        }

        public OrderPurchase GetPurchaseOrderByID(int ID)
        {
            string SQLString = "select ebay_account.*, tblCompanyebayaccount.companyebayaccountid,tblCompanyebayaccount.AdminID,tblCompanyebayaccount.StatusID, tblCompany.companyname, c_User.FirstName, c_User.LastName from ebay_account,tblCompanyebayaccount,tblCompany, c_User ";
            SQLString += " where  ebay_account.id=tblCompanyebayaccount.ebayAccountID and tblcompany.companyid=tblCompanyebayaccount.companyid and c_User.UserID=tblCompanyebayaccount.AdminId";
            SQLString += " AND ebay_account.id IN (" + ID + ")";

            using (MySqlDataAdapter adapter = new MySqlDataAdapter(SQLString, new MySqlConnection(Helpers.Helpers.GetERPConnectionString())))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                DataTable tb = (DataTable)ds.Tables[0];
                List<OrderPurchase> allECommerceAccounts = new List<OrderPurchase>();
                if (tb != null && tb.Rows.Count > 0)
                {
                    DataRow dr = tb.Rows[0];
                    return OrderPurchase.fillInObject(dr);
                }
                return null;
            }
        }

        public void EditPurchaseOrder(OrderPurchase model)
        {
            using (MySqlConnection connection = new MySqlConnection(Helpers.Helpers.GetERPConnectionString()))
            {
                try
                {
                    // Create the user record.
                    MySqlCommand cmd = new MySqlCommand("", connection);
                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append(" update purchase_order set ");
                    sb.Append(" order_date ='" + model.order_date + "',");
                    sb.Append(" statusID ='" + model.statusID + "',");
                    sb.Append(" sku ='" + model.sku + "',");
                    sb.Append(" UnitPurchasePrice ='" + model.UnitPurchasePrice + "',");
                    sb.Append(" updateDate ='" + model.updateDate + "',");
                    sb.Append(" io_warehouse =" + model.io_warehouse + ",");
                    sb.Append(" io_partner ='" + model.io_partner + "',");
                    sb.Append(" where id = " + model.id);
                    cmd.CommandText = sb.ToString();
                    cmd.ExecuteNonQuery();

                    //record the purchase order event queue

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

        public void AddOrderPurchase(OrderPurchase model)
        {
            using (MySqlConnection connection = new MySqlConnection(Helpers.Helpers.GetERPConnectionString()))
            {
                try
                {
                    // Create the user record.
                    MySqlCommand cmd = new MySqlCommand("", connection);
                    connection.Open();                    
                    cmd.CommandText = GetAddOrderPurchaseString(model);
                    int id = cmd.ExecuteNonQuery();

                    //added event queue

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

        private string GetAddOrderPurchaseString(OrderPurchase model)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(" insert purchase_order (PurchaseCompanyID,order_date, statusID, sku, io_partner, io_warehouse,quantity, io_user, UnitPurchasePrice, weight, length, width, height, updateDate, customRate) ");
            sb.Append(" values ( ");
            sb.Append(" " + model.PurchaseCompanyID + ",");
            sb.Append(" '" + model.order_date.ToString("yyyy-MM-dd") + "',");
            sb.Append(" '" + model.statusID + "',");
            sb.Append(" " + model.sku + ",");
            sb.Append(" '" + model.io_partner + "',");
            sb.Append(" '" + model.io_warehouse + "',");
            sb.Append(" " + model.quantity + ",");
            sb.Append(" '" + Session["UserID"] + "',");
            sb.Append(" '" + model.UnitPurchasePrice + "',");
            sb.Append(" '" + model.weight + "',");
            sb.Append(" '" + model.length + "',");
            sb.Append(" '" + model.width + "',");
            sb.Append(" " + model.height + ",");
            sb.Append(" '" + model.updateDate.ToString("yyyy-MM-dd") + "',");
            sb.Append(" " + model.customRate + "");
            sb.Append(" ) ");
            return sb.ToString();

        }

        //estimate the domestic cshippping cost based on zone 3
        private double getDomesticShippingCost(OrderPurchase model)
        {
            List<string> zips = new List<string>();
            //zone2
            zips.Add("43001");
            //zone3
            zips.Add("48001");
            //zone4
            zips.Add("49601");
            //zone5
            zips.Add("34901");
            //zone6
            zips.Add("59001");
            //zone7
            zips.Add("85501");
            //zone8
            zips.Add("89401");
            EbayCustomer customer = new EbayCustomer();
            customer.weight = model.weight;
            customer.length = model.length;
            customer.width = model.width;
            customer.height = model.height ;
            customer.country = "US";
            customer.residential = true;
            customer.city = "mason";
            ship.FedexShipper fedexShipper = new FedexShipper();
            double sumfedexGroundRate = 0;
            double sumfedexSmartPostRate = 0;
            for (int i = 0; i < zips.Count; i++)
            {
                customer.postCode = zips[i];
                sumfedexGroundRate += fedexShipper.getRate(customer, true, false);
                sumfedexSmartPostRate += fedexShipper.getRate(customer, false, false);
            }
            double miniShipping = Math.Min(sumfedexGroundRate, sumfedexSmartPostRate)/7.0;

            double weight = model.weight;
            if(weight> 16)
            {
                weight = Math.Ceiling(weight / 16) * 16;
            }
            String sql = " select shipRateID, name, (zone1 + zone2 + zone3 + zone4 + zone5 + zone6 + zone7 + zone8 + zone9 + zone10 + zone11 + zone12 + zone13)/ 13 "
                + " from c_shiprate where statusid = 1 and weight = " + weight;
            using (MySqlDataAdapter adapter = new MySqlDataAdapter(sql, new MySqlConnection(Helpers.Helpers.GetERPConnectionString())))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                DataTable tb = (DataTable)ds.Tables[0];
                double dhladdition = 2.0;
                if (tb != null && tb.Rows.Count > 0)
                {
                    for (int i = 0; i < tb.Rows.Count; i++)
                    {
                        DataRow dr = tb.Rows[i];
                        string name = dr[1].ToString().ToLower ();
                        double mostLength = Math.Max(Math.Max(model.height, model.width), model.length);
                        double secondLength = Math.Min(Math.Max(model.height, model.width), Math.Max(model.length, model.width));
                        double thirdLength = Math.Min(Math.Min(model.height, model.width), model.length);
                        double size = mostLength + (secondLength + thirdLength) * 2;
                        if (name.Equals("dhl"))
                        {
                            if (mostLength> 27 || (secondLength >17 && thirdLength >17) 
                                || size > 84 )
                            {
                                break;
                            }
                            else if (size > 50)
                            {
                                miniShipping = (double)dr[2] + dhladdition;
                            }
                            else
                            {
                                miniShipping = Math.Min((double)dr[2], miniShipping);
                            }
                        }
                        else if (name.Equals("dhl 2"))
                        {
                            if (mostLength > 27 || (secondLength > 17 && thirdLength > 17)
                                || size > 84)
                            {
                                break;
                            }
                            else if (size > 50)
                            {
                                miniShipping = (double)dr[2] + dhladdition;
                            }
                            else
                            {
                                miniShipping = Math.Min((double)dr[2], miniShipping);
                            }
                        }
                        if (name.Equals("dhl express"))
                        {
                            if (mostLength > 27 || (secondLength > 17 && thirdLength > 17)
                                || size > 84)
                            {
                                break;
                            }
                            else if (size > 50)
                            {
                                miniShipping = (double)dr[2] + dhladdition;
                            }
                            else
                            {
                                miniShipping = Math.Min((double)dr[2], miniShipping);
                            }
                        }
                        //else if (name.Equals("fedex smartpost"))
                        //{
                        //    if(size <= 130 && size >= 107)
                        //    {
                        //        miniShipping = Math.Min ((double)dr[2], miniShipping );
                        //    }
                        //    else if(size <108 && size>84)
                        //    {
                        //        if(model.weight > 20)
                        //        {
                        //            miniShipping = Math.Min(fedex20LB, miniShipping);
                        //        }
                        //        else
                        //        {
                        //            miniShipping = Math.Min((double)dr[2]*fedexDiscount , miniShipping);
                        //        }
                        //    }
                        //    else if (mostLength > 34 || (secondLength > 17 && thirdLength > 17)
                        //        || model.weight > 35*16)
                        //    {
                        //        miniShipping = (double)dr[2]* fedexDiscount + 2.75;
                        //    }
                        //    else
                        //    {
                        //        miniShipping = Math.Min((double)dr[2], miniShipping);
                        //    }
                        //}
                        //else if (name.Equals("fedex resident"))
                        //{
                        //          if(size <= 130 && size >= 107)
                        //    {
                        //        miniShipping = Math.Min ((double)dr[2], miniShipping );
                        //    }
                        //    else if(size <108 && size>84)
                        //    {
                        //        if(model.weight > 20)
                        //        {
                        //            miniShipping = Math.Min(fedex20LB, miniShipping);
                        //        }
                        //        else
                        //        {
                        //            miniShipping = Math.Min((double)dr[2]*fedexDiscount , miniShipping);
                        //        }
                        //    }
                        //    else if (mostLength > 34 || (secondLength > 17 && thirdLength > 17)
                        //        || model.weight > 35*16)
                        //    {
                        //        miniShipping = (double)dr[2]* fedexDiscount + 2.75;
                        //    }
                        //    else
                        //    {
                        //        miniShipping = Math.Min((double)dr[2], miniShipping);
                        //    }
                        //}
                    }
                }
                return miniShipping;
            }
        }

    }
    public class OrderPurchase
    {
        public int id { get; set; }
        public string purchaseNumber { get; set; }
        public string io_ordersn { get; set; }
        public string io_shipfee { get; set; }
        public string io_user { get; set; }
        public int io_addtime { get; set; }
        public int io_audittime { get; set; }
        public string io_warehouse { get; set; }
        public string io_status { get; set; }
        public string io_note { get; set; }
        public string io_paymentmethod { get; set; }
        public string io_partner { get; set; }
        public int sourceorder { get; set; }
        public DateTime order_date { get; set; }
        public DateTime in_warehouseto { get; set; }
        public int qc_user { get; set; }
        public string sku { get; set; }
        public int quantity { get; set; }
        public int PurchaseCompanyID { get; set; }
        public int ContainerID { get; set; }
        public double Size { get; set; }
        public double weight { get; set; }
        public double height { get; set; }
        public double length { get; set; }
        public double width { get; set; }
        public double UnitPurchasePrice { get; set; }
        public string customNo { get; set; }
        public string palletNo { get; set; }
        public string boxNo { get; set; }
        public int statusID { get; set; }
        public string StatusName { get; set; }
        public string PurchaseCompanyName { get; set; }
        public string warehouse { get; set; }
        public DateTime updateDate { get; set; }
        public double internationalShipping { get; set; }
        public double processfee { get; set; }
        public double saleCost { get; set; }
        public double domesticShipping { get; set; }
        public double custom { get; set; }
        public double customRate { get; set; }
        public double packingfee { get; set; }
        public double handlingfee { get; set; }




        public IEnumerable<SelectListItem> AllSku { get; set; }
        public IEnumerable<SelectListItem> AllBankAccount { get; set; }
        public IEnumerable<SelectListItem> AllStatus { get; set; }
        public IEnumerable<SelectListItem> AllVendor { get; set; }
        public IEnumerable<SelectListItem> AllWarehouse { get; set; }
        public IEnumerable<SelectListItem> AllCompany { get; set; }

        public static OrderPurchase fillInObject(DataRow dr)
        {
            OrderPurchase account = new OrderPurchase();
            account.id = Int32.Parse(dr["id"].ToString());
            account.purchaseNumber = dr["purchaseNumber"].ToString();
            account.io_ordersn = dr["io_ordersn"].ToString();
            account.io_ordersn = dr["io_ordersn"].ToString();
            account.io_user = dr["io_user"].ToString();
            if (dr["io_addtime"] != DBNull.Value) { account.io_addtime = Int32.Parse(dr["io_addtime"].ToString()); }
            if (dr["io_audittime"] != DBNull.Value) { account.io_audittime = Int32.Parse(dr["io_audittime"].ToString()); }
            account.io_warehouse = dr["io_warehouse"].ToString();
            account.io_status = dr["io_status"].ToString();
            account.io_note = dr["io_note"].ToString();
            account.io_paymentmethod = dr["io_paymentmethod"].ToString();
            account.io_partner = dr["io_partner"].ToString();
            if (dr["store_name"] != DBNull.Value) { account.warehouse = dr["store_name"].ToString(); }
            if (dr["sourceorder"] != DBNull.Value) { account.sourceorder = Int32.Parse(dr["sourceorder"].ToString()); }
            if (dr["qc_user"] != DBNull.Value) { account.qc_user = Int32.Parse(dr["qc_user"].ToString()); }
            account.sku = dr["sku"].ToString();
            if (dr["weight"] != DBNull.Value) { account.weight = double.Parse(dr["weight"].ToString()); }
            if (dr["width"] != DBNull.Value) { account.width = double.Parse(dr["width"].ToString()); }
            if (dr["length"] != DBNull.Value) { account.length = double.Parse(dr["length"].ToString()); }
            if (dr["height"] != DBNull.Value) { account.height = double.Parse(dr["height"].ToString()); }
            if (dr["quantity"] != DBNull.Value) { account.quantity = Int32.Parse(dr["quantity"].ToString()); }
            if (dr["PurchaseCompanyID"] != DBNull.Value) { account.PurchaseCompanyID = Int32.Parse(dr["PurchaseCompanyID"].ToString()); }
            if (dr["ContainerID"] != DBNull.Value) { account.ContainerID = Int32.Parse(dr["ContainerID"].ToString()); }
            if (dr["Size"] != DBNull.Value) { account.Size = double.Parse(dr["Size"].ToString()); }
            if (dr["UnitPurchasePrice"] != DBNull.Value) { account.UnitPurchasePrice = double.Parse(dr["UnitPurchasePrice"].ToString()); }
            if (dr["customRate"] != DBNull.Value) { account.customRate = double.Parse(dr["customRate"].ToString()); }
            account.customNo = dr["customNo"].ToString();
            account.palletNo = dr["palletNo"].ToString();
            account.boxNo = dr["boxNo"].ToString();
            account.PurchaseCompanyName = dr["CompanyName"].ToString();
            account.StatusName = dr["name"].ToString();
            if (dr["statusID"] != DBNull.Value) { account.statusID = Int32.Parse(dr["statusID"].ToString()); }
            if(account.Size ==0)
            {
                double mostLength = Math.Max(Math.Max(account.height, account.width), account.length);
                double secondLength = Math.Min(Math.Max(account.height, account.width), Math.Max(account.length, account.width));
                double thirdLength = Math.Min(Math.Min(account.height, account.width), account.length);
                account.Size = mostLength + (secondLength + thirdLength) * 2;

            }
            return account;
        }
    }


    public class PurchaseEventQuene
    {
        public int OrderEventQueueID { get; set; }
        public int OrderID { get; set; }
        public string Note { get; set; }
        public int EventID;
        public DateTime UpdateDate { get; set; }
        public int CategoryID { get; set; }
        public string name { get; set; }

        public static PurchaseEventQuene fillInObject(DataRow dr)
        {
            PurchaseEventQuene account = new PurchaseEventQuene();
            account.OrderEventQueueID = Int32.Parse(dr["OrderEventQueueID"].ToString());
            if (dr["OrderID"] != DBNull.Value) { account.OrderID = Int32.Parse(dr["OrderID"].ToString()); }
            if (dr["EventID"] != DBNull.Value) { account.EventID = Int32.Parse(dr["EventID"].ToString()); }
            account.Note = dr["Note"].ToString();
            if (dr["UpdateDate"] != DBNull.Value) { account.UpdateDate = DateTime.Parse(dr["UpdateDate"].ToString()); }
            account.name = dr["name"].ToString();
            return account;
        }
    }
}


