using System;
using System.Web.Mvc;
using System.Collections.Generic;
using System.Data.SqlClient;
using PropertyManagement.Models;
using PropertyManagement.ViewModels.Property;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;

namespace PropertyManagement.Controllers
{
    [Authorize]
    public class ECommerceAccountController : BaseController
    {
        //
        // GET: /ManageUser/

        private string reporttitle = "E-Commerce Account";

        [AllowAnonymous]
        public ActionResult Index()
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = reporttitle;

            var companies = GetList((short)Helpers.Helpers.ListType.company);
            ViewBag.companies = new MultiSelectList(companies, "id", "description");
            return View();
        }

        [AllowAnonymous]
        public PartialViewResult ReportView(string[] companyIDs)
        {
            TempData["companyIDs"] = companyIDs;
            return PartialView("ReportView", GetEcommerceAccountByCompanyIDs(companyIDs, ((int)Session["UserID"])));
        }
        

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Add()
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Add Ecommerce account";
            ECommerceAccount model = new ECommerceAccount();
            model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);
            model.AllCompany = GetSelectListItems((short)Helpers.Helpers.ListType.company);
            model.AllAdmin = GetSelectListItems((short)Helpers.Helpers.ListType.allUser);
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Add(ECommerceAccount model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Add Ecommerce account";
            
            AddECommerceAccount(model);
            return RedirectToAction("Index");
        }


        [AllowAnonymous]
        public ActionResult Edit(int id)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Edit Ecommerce account";

            ECommerceAccount model = GetEcommerceAccountByID(id);

            model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);
            model.AllCompany = GetSelectListItems((short)Helpers.Helpers.ListType.company);
            model.AllAdmin = GetSelectListItems((short)Helpers.Helpers.ListType.allUser);

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Edit(ECommerceAccount model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Edit ECommerce Account";

            EditECommerceAccount(model);
            return RedirectToAction("Index");
        }

        public List<ECommerceAccount> GetEcommerceAccountByCompanyIDs(string[] companyIDs, int userID)
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
                List<ECommerceAccount> allECommerceAccounts = new List<ECommerceAccount>();
                if (tb != null && tb.Rows.Count > 0)
                {
                    for (int i = 0; i < tb.Rows.Count; i++)
                    {
                        DataRow dr = tb.Rows[i];
                        allECommerceAccounts.Add(fillInObject(dr));
                    }
                }
                return allECommerceAccounts;
            }
        }

        public ECommerceAccount GetEcommerceAccountByID(int ID)
        {
            string SQLString = "select ebay_account.*, tblCompanyebayaccount.companyebayaccountid,tblCompanyebayaccount.AdminID,tblCompanyebayaccount.StatusID, tblCompany.companyname, c_User.FirstName, c_User.LastName from ebay_account,tblCompanyebayaccount,tblCompany, c_User ";
            SQLString += " where  ebay_account.id=tblCompanyebayaccount.ebayAccountID and tblcompany.companyid=tblCompanyebayaccount.companyid and c_User.UserID=tblCompanyebayaccount.AdminId";
            SQLString += " AND ebay_account.id IN (" + ID + ")";

            using (MySqlDataAdapter adapter = new MySqlDataAdapter(SQLString, new MySqlConnection(Helpers.Helpers.GetERPConnectionString())))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                DataTable tb = (DataTable)ds.Tables[0];
                List<ECommerceAccount> allECommerceAccounts = new List<ECommerceAccount>();
                if (tb != null && tb.Rows.Count > 0)
                {
                    DataRow dr = tb.Rows[0];
                    return fillInObject(dr);
                }
                return null;
            }
        }

        public void EditECommerceAccount(ECommerceAccount model)
        {
            using (MySqlConnection connection = new MySqlConnection(Helpers.Helpers.GetERPConnectionString()))
            {
                try
                {
                    // Create the user record.
                    MySqlCommand cmd = new MySqlCommand("", connection);
                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append(" update ebay_account set ");
                    sb.Append(" ebay_account ='" + model.ebay_account + "',");
                    sb.Append(" site ='" + model.site + "',");
                    sb.Append(" ebay_token ='" + model.ebay_token + "',");
                    sb.Append(" ebay_user ='" + model.ebay_user + "',");
                    sb.Append(" mail ='" + model.mail + "',");
                    sb.Append(" storeid =" + model.storeid + ",");
                    sb.Append(" serviceUrl ='" + model.serviceUrl + "',");
                    sb.Append(" where id = " + model.id);
                    cmd.CommandText = sb.ToString();
                    cmd.ExecuteNonQuery();

                    //update company id for property
                    sb.Clear();
                    sb.Append("update tblcompanyebayaccount set CompanyID = " + model.companyId + " where CompanyEbayAccountID=" + model.CompanyEbayAccountID);
                    cmd.CommandText = sb.ToString();
                    cmd.ExecuteNonQuery();

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

        public void AddECommerceAccount(ECommerceAccount model)
        {
            using (MySqlConnection connection = new MySqlConnection(Helpers.Helpers.GetERPConnectionString()))
            {
                try
                {
                    // Create the user record.
                    MySqlCommand cmd = new MySqlCommand("", connection);
                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append(" insert ebay_account (ebay_account, site, ebay_token, ebay_user,mail, storeid,serviceUrl) ");
                    sb.Append(" value ( ");
                    sb.Append(" '" + model.ebay_account + "',");
                    sb.Append(" '" + model.site + "',");
                    sb.Append(" '" + model.ebay_token + "',");
                    sb.Append(" '" + model.ebay_user + "',");
                    sb.Append(" '" + model.mail + "',");
                    sb.Append(" " + model.storeid + ",");
                    sb.Append(" '" + model.serviceUrl + "'");
                    sb.Append(" ) " );
                    cmd.CommandText = sb.ToString();
                    int id = cmd.ExecuteNonQuery();

                    //insert account 
                    sb.Clear();
                    sb.Append("insert tblcompanyebayaccount (CompanyID, startDate, ebayaccountid, statusid, adminid) value ");
                    sb.Append ( model.companyId + ",'" +DateTime .Now .ToShortDateString ()+"',"+ id+"1"+model.adminID);
                    cmd.CommandText = sb.ToString();
                    cmd.ExecuteNonQuery();
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

        private ECommerceAccount fillInObject(DataRow dr)
        {
            ECommerceAccount account = new ECommerceAccount();
            account.id = (int)dr["id"];
            account.ebay_account = (string)dr["ebay_account"];
            account.ebay_user = (string)dr["ebay_user"];
            account.site = (string)dr["site"];
            account.admin = new User();
            if (dr["AdminID"] != DBNull.Value)
            {
                account.StatusID = Int32.Parse(dr["AdminID"].ToString());
                account.admin.FirstName = dr["FirstName"].ToString();
                account.admin.LastName = dr["LastName"].ToString();
            }
            if (dr["StatusID"] != DBNull.Value) { account.StatusID = Int32.Parse(dr["StatusID"].ToString()); }
            if (dr["storeid"] != DBNull.Value) { account.storeid = Int32.Parse(dr["storeid"].ToString()); }
            if (dr["serviceUrl"] != DBNull.Value) { account.serviceUrl = dr["serviceUrl"].ToString(); }
            if (dr["companyName"] != DBNull.Value) { account.companyName = dr["companyName"].ToString(); }
            if (dr["site"] != DBNull.Value) { account.site = (string)dr["site"]; }
            if (dr["lastSyncTime"] != DBNull.Value) { account.lastSyncTime = (DateTime)dr["lastSyncTime"]; }
            return account;
        }
    }
    public class ECommerceAccount
    {
        public int id;
        public string ebay_account;
        public string site;
        public string ebay_token;
        public string ebay_user;
        public string appname;
        public string mail;
        public int storeid;
        public string serviceUrl;
        public string companyName;
        public DateTime lastSyncTime;
        public int companyId;
        public int CompanyEbayAccountID;
        public int StatusID;
        public int adminID;
        public User admin;
        public IEnumerable<SelectListItem> AllStatus { get; set; }
        public IEnumerable<SelectListItem> AllCompany { get; set; }
        public IEnumerable<SelectListItem> AllAdmin { get; set; }
    }

}


