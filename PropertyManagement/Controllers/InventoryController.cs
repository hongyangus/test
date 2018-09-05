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
    public class InventoryController : BaseController
    {

        [AllowAnonymous]
        public ActionResult Index()
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "缺货分析";

            var companies = GetList((short)Helpers.Helpers.ListType.allECommerceCompany);
            ViewBag.companies = new MultiSelectList(companies, "id", "description");
            var warehouse = GetList((short)Helpers.Helpers.ListType.allWarehouse);
            ViewBag.warehouse = new MultiSelectList(warehouse, "id", "description");
            return View();
        }


        [AllowAnonymous]
        public PartialViewResult ReportView(string[] companyIDs, string[] warehouseIDs, bool current, bool oneMonth, bool twoMonth, bool threeMonth)
        {
            List<Inventory> result = new List<Inventory>();
            StringBuilder sbOperation = new StringBuilder();
            if (current)
            {
                sbOperation.Append("select ebay_onhandle.goods_id,ebay_onhandle.goods_name, ebay_onhandle.goods_count as quantity, ebay_store.store_name, ebay_onhandle.store_id,avg_sale_volume.quantity as dailyAvgSale ");
                sbOperation.Append(" from ebay_onhandle, ebay_store,avg_sale_volume");
                sbOperation.Append (" where ebay_onhandle.goods_count>0 and ebay_onhandle.store_id = ebay_store.id and ebay_onhandle.store_id = avg_sale_volume.ebay_warehouse");

                // Add modality id to the where clause if appropriate
                //if (companyIDs != null && companyIDs.Count() > 0 && !string.IsNullOrEmpty(companyIDs[0]))
                //{
                //    sbOperation.Append(" AND tblCompany.CompanyID IN (" + String.Join(",", companyIDs) + ")");
                //}
                //else
                //{
                //    //get the companys only the owner can access
                //    sbOperation.Append(" AND tblCompany.CompanyID IN (" + GetUserManagedCompanyString() + ")");
                //}
                // Add modality id to the where clause if appropriate
                if (warehouseIDs != null && warehouseIDs.Count() > 0 && !string.IsNullOrEmpty(warehouseIDs[0]))
                {
                    sbOperation.Append(" AND ebay_store.id IN (" + String.Join(",", warehouseIDs) + ")");
                }

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
                            Inventory row = new Inventory();
                            row.quantity = Int32.Parse (dr["quantity"].ToString ());
                            row.skuName = dr["goods_name"].ToString();
                            row.warehouseID = Int32.Parse(dr["store_id"].ToString());
                            row.warehouseName = dr["store_name"].ToString();
                            row.dailyAvgSale = Int32.Parse(dr["dailyAvgSale"].ToString());
                            if (threeMonth)
                            {
                                if(row.quantity > row.dailyAvgSale *90)
                                {
                                    break;
                                }
                                else
                                {
                                    row.threeMonthPurchase = row.dailyAvgSale * 90 - row.quantity;
                                }
                            }
                            if(twoMonth)
                            {
                                if (row.quantity > row.dailyAvgSale * 60)
                                {
                                    break;
                                }
                                else
                                {
                                    row.twoMonthPurchase = row.dailyAvgSale * 60 - row.quantity;
                                }
                            }
                            result.Add(row);
                        }
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
           // result = FillInPurchaseOrderMarketAnalysis(result);
            return PartialView("MarketAnalyze", result);
        }


    }
    public class Inventory
    {
        public int warehouseID { get; set; }
        public int quantity { get; set; }
        public string warehouseName { get; set; }
        public int skuID { get; set; }
        public string skuName { get; set; }
        public string sku { get; set; }
        public int dailyAvgSale { get; set; }
        public int threeMonthPurchase { get; set; }
        public int twoMonthPurchase { get; set; }

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