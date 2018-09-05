using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Diagnostics;
using System.Data.Entity;
using System.Threading;
using System.Configuration;
using System.Data.SqlClient;
using PropertyManagement.Helpers;
using PropertyManagement.Models;
using MySql.Data.MySqlClient;
using System.Data;

public class dropdown_list
{
    public long id { get; set; }
    public string description { get; set; }
    public string abbre { get; set; }

    public dropdown_list ()
    {

    }
    public dropdown_list(string m_abbre, string m_state)
    {
        abbre = m_abbre;
        description = m_state;
    }
}


namespace PropertyManagement.Controllers
{

    // TJO: Class to create a filter for all Actions which will check the DB to see if it is undergoing maintenance.
    // Note: See the Global.asax.cs file for code where the filter is actually added.
    public class OfflineActionFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
           
        }
    }

    [Authorize]
    public class BaseController : Controller
    {
        public void SetIndex()
        {
            var companies = GetList((short)Helpers.Helpers.ListType.company);
            ViewBag.companies = new MultiSelectList(companies, "id", "description");

            var properties = GetList((short)Helpers.Helpers.ListType.property);
            ViewBag.properties = new MultiSelectList(properties, "id", "description");

            var units = GetList((short)Helpers.Helpers.ListType.unit);
            ViewBag.units = new MultiSelectList(units, "id", "description");

            var bankAccounts = GetList((short)Helpers.Helpers.ListType.bankaccount);
            ViewBag.bankAccounts = new MultiSelectList(bankAccounts, "id", "description");


            //setup default value of the start date and end date
            ViewBag.startDate = DateTime.Now.AddDays(-30).ToString("MM/dd/yyyy");
            ViewBag.endDate = DateTime.Now.ToString("MM/dd/yyyy");
        }
       

        public string GetUserManagedCompanyString()
        {
            if ((int)Session["UserID"] == 1)
            {
                return " (select companyID from tblCompanyUser)";
            }
            else
            {
                return " (select companyID from tblCompanyUser where tblCompanyUser.RoleID <5 and tblCompanyUser.UserID = " + Session["UserID"] + ")";
            }
        }

        public void LogException(string errorMessage)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                string query = "insert into tblException (ExceptionMessage) values ('" + errorMessage.Replace("'", "") + "')";
                SqlCommand command = new SqlCommand(query, connection); // Create the Command and Parameter objects
                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
            }
        }
        public List<dropdown_list> GetList(short type)
        {
            string SQLString = null;
            switch (type)
            {
                case (short)Helpers.Helpers.ListType.company:
                    SQLString = "SELECT distinct CompanyID, CompanyName FROM tblCompany ";
                    SQLString += " where CompanyID IN " +  GetUserManagedCompanyString();
                    SQLString += " order by CompanyName";
                    return QueryList(SQLString);

                case (short)Helpers.Helpers.ListType.property:
                    SQLString = "SELECT distinct [tblProperty].[PropertyID] , [tblProperty].[Address] FROM [tblProperty],mCompanyProperty, tblCompanyUser WHERE tblProperty.PropertyID =mCompanyProperty.PropertyID and  ";
                    SQLString += " mCompanyProperty.CompanyID in " + GetUserManagedCompanyString()+ " Order by Address";
                    return QueryList(SQLString);

                case (short)Helpers.Helpers.ListType.unit:
                    SQLString = "SELECT distinct [UnitID] ,([tblProperty].Address +'-' +[tblPropertyUnit].UnitName) as UnitName  FROM [tblPropertyUnit], [tblProperty],mCompanyProperty, tblCompanyUser WHERE [tblPropertyUnit].PropertyID = [tblProperty].PropertyID AND tblProperty.PropertyID= mCompanyProperty.PropertyID AND ";
                    SQLString += " mCompanyProperty.CompanyID in " + GetUserManagedCompanyString() + " Order by UnitName";
                    return QueryList(SQLString);

                case (short)Helpers.Helpers.ListType.allTenant:
                    SQLString = "select distinct cUser.UserID, (cUser.LastName + ', ' + cuser.FirstName + ' -- ' + tblProperty.Address + '-' + tblPropertyUnit.UnitName) as description";
                    SQLString += " from cUser, tblTenant, tblProperty, tblPropertyUnit, tblCompanyUser,mCompanyProperty ";
                    SQLString += " where cUser.UserID = tblTenant.UserID AND tblTenant.UnitID = tblPropertyUnit.UnitID ";
                    SQLString += " and tblPropertyUnit.PropertyID = mCompanyProperty.PropertyID ";
                    SQLString += " and tblPropertyUnit.PropertyID = tblProperty.PropertyID ";
                    SQLString += " AND mCompanyProperty.PropertyID = tblProperty.PropertyID ";
                    SQLString += " and mCompanyProperty.CompanyID = tblCompanyUser.CompanyID";
                    SQLString += " and tblTenant.StatusID  <3 ";
                    SQLString += " and tblCompanyUser.CompanyID IN "+ GetUserManagedCompanyString();
                    SQLString += " ORDER BY description";
                    return QueryList(SQLString);

                case (short)Helpers.Helpers.ListType.bankaccount:
                    SQLString = "SELECT distinct tblCompanyFinancialAccount.FinancialAccountID, AccountName FROM tblAccount INNER JOIN tblCompanyFinancialAccount on tblCompanyFinancialAccount.FinancialAccountID = tblAccount.FinancialAccountID";
                    SQLString += " and tblCompanyFinancialAccount.CompanyID in " + GetUserManagedCompanyString();
                    SQLString += " order by AccountName";
                    return QueryList(SQLString);
                case (short)Helpers.Helpers.ListType.contractor:
                    SQLString = "SELECT distinct cUser.UserID, (cUser.FirstName + ' ' + cUser.LastName) as FullName FROM cUser INNER JOIN tblCompanyUser on tblCompanyUser.UserID = cUser.UserID";
                    SQLString += " WHERE tblCompanyUser.CompanyID in " + GetUserManagedCompanyString();
                    SQLString += " AND tblCompanyUser.RoleID = 5";
                    SQLString += " order by FullName";
                    return QueryList(SQLString);

                case (short)Helpers.Helpers.ListType.allUser:
                    SQLString = "SELECT distinct cUser.UserID, (cUser.FirstName + ' ' + cUser.LastName) as FullName FROM cUser INNER JOIN tblCompanyUser on tblCompanyUser.UserID = cUser.UserID";
                    SQLString += " WHERE tblCompanyUser.CompanyID in " + GetUserManagedCompanyString ();
                    SQLString += " order by FullName";
                    return QueryList(SQLString);

                case (short)Helpers.Helpers.ListType.allStatus:
                    SQLString = "SELECT distinct StatusTypeID, name FROM cStatusType order by name";
                    return QueryList(SQLString);

                case (short)Helpers.Helpers.ListType.allExpenseCategory :
                    SQLString = "SELECT distinct CategoryID, CategoryName FROM cExpenseCategory order by CategoryName";
                    return QueryList(SQLString);

                case (short)Helpers.Helpers.ListType.allRoles :
                    SQLString = "SELECT distinct RoleID, RoleName FROM cRole order by RoleName";
                    return QueryList(SQLString);

                case (short)Helpers.Helpers.ListType.allTemplate:
                    SQLString = "SELECT distinct TemplateID, Name FROM tblEmailTemplate order by Name";
                    return QueryList(SQLString);

                case (short)Helpers.Helpers.ListType.allWarehouse:
                    SQLString = "SELECT distinct id, store_name FROM ebay_store, tblcompanywarehouse where tblcompanywarehouse.WarehouseID = ebay_store.id ";
                    SQLString += " and tblcompanywarehouse.CompanyID IN " + GetUserManagedCompanyString();
                    SQLString += " order by store_name";
                    return QueryECommerceList(SQLString);

                case (short)Helpers.Helpers.ListType.allECommercePurchaseStatus:
                    SQLString = "SELECT distinct STATUSID, name FROM c_status WHERE categoryID = 1 order by STATUSID";
                    return QueryECommerceList(SQLString);

                case (short)Helpers.Helpers.ListType.allECommercePurchasePlan:
                    SQLString = "SELECT distinct STATUSID, name FROM c_status WHERE categoryID = 2 order by STATUSID";
                    return QueryECommerceList(SQLString);

                case (short)Helpers.Helpers.ListType.allECommerceCompany:
                    SQLString = "SELECT distinct CompanyID, CompanyName FROM tblCompany ";
                    SQLString += " where CompanyID IN " + GetUserManagedCompanyString();
                    SQLString += " order by CompanyName";
                    return QueryECommerceList(SQLString);

                case (short)Helpers.Helpers.ListType.allECommerceVendor:
                    SQLString = "SELECT distinct id, Company_Name FROM ebay_partner ";
                    SQLString += " where ebay_companyID IN " + GetUserManagedCompanyString();
                    SQLString += " order by Company_Name";
                    return QueryECommerceList(SQLString);

                case (short)Helpers.Helpers.ListType.allECommerceCountry:
                    SQLString = "SELECT distinct countryID, country FROM c_country  ";
                    SQLString += " where statusID = 1 ";
                    SQLString += " order by displayOrder";
                    return QueryECommerceList(SQLString);
                default:
                    return null;
            }

           
        }
        private List<dropdown_list> QueryList(string queryString)
        {
            List<dropdown_list> result = new List<dropdown_list>();
            using (SqlConnection connection = new SqlConnection(Helpers .Helpers .GetAppConnectionString()))
            {
                SqlCommand command = new SqlCommand(queryString, connection); // Create the Command and Parameter objects
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read()) // Read each result row and extract the data
                {
                    dropdown_list row = new dropdown_list();
                    row.id = long.Parse(reader[0].ToString());
                    row.description = reader[1].ToString();
                    result.Add(row);
                }
                reader.Close();
                connection.Close();
            }
            return result;
        }

        private List<dropdown_list> QueryECommerceList(string queryString)
        {
            List<dropdown_list> result = new List<dropdown_list>();
            using (MySqlDataAdapter adapter = new MySqlDataAdapter(queryString, new MySqlConnection(Helpers.Helpers.GetERPConnectionString())))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                DataTable tb = (DataTable)ds.Tables[0];
                if (tb != null && tb.Rows.Count > 0)
                {
                    for (int i = 0; i < tb.Rows.Count; i++)
                    {
                        dropdown_list row = new dropdown_list();
                        row.id = long.Parse(tb.Rows[i][0].ToString());
                        row.description = tb.Rows[i][1].ToString();
                        result.Add(row);
                    }
                }
            }
            return result;
        }

        private IEnumerable<SelectListItem> QueryMultipleList(string queryString)
        {
            List<SelectListItem> result = new List<SelectListItem>();
            string connectionString = ConfigurationManager.ConnectionStrings["SunriseConnectionString"].ConnectionString; // Get the connection string parameters
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection); // Create the Command and Parameter objects
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read()) // Read each result row and extract the data
                {
                    var selectList = new SelectListItem();
                    selectList.Value = reader[0].ToString();
                    selectList.Text = reader[1].ToString();
                    result.Add(selectList);
                }
                reader.Close();
            }
            return result;

        }

        private IEnumerable<SelectListItem> QueryECommerceMultipleList(string queryString)
        {
            List<SelectListItem> result = new List<SelectListItem>();
            string connectionString = Helpers.Helpers.GetERPConnectionString();
            using (MySqlDataAdapter adapter = new MySqlDataAdapter(queryString, new MySqlConnection(Helpers.Helpers.GetERPConnectionString())))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                DataTable tb = (DataTable)ds.Tables[0];
                if (tb != null && tb.Rows.Count > 0)
                {
                    for (int i = 0; i < tb.Rows.Count; i++)
                    {
                        var selectList = new SelectListItem();
                        selectList.Value = tb.Rows [i][0].ToString();
                        selectList.Text = tb.Rows[i][1].ToString();
                        result.Add(selectList);
                    }
                }
            }
            
            return result;
        }

        private List<CheckBoxListItem> QueryCheckList(string queryString)
        {
            List<CheckBoxListItem> result = new List<CheckBoxListItem>();
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                SqlCommand command = new SqlCommand(queryString, connection); // Create the Command and Parameter objects
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read()) // Read each result row and extract the data
                {
                    CheckBoxListItem row = new CheckBoxListItem();
                    row.ID = int.Parse(reader[0].ToString());
                    row.Display = reader[1].ToString();
                    result.Add(row);
                }
                reader.Close();
                connection.Close();
            }
            return result;
        }

        public IEnumerable<SelectListItem> GetSelectListItems(short type)
        {
            string SQLString = null;
            switch (type)
            {
                case (short)Helpers .Helpers .ListType .company:
                    SQLString = "SELECT distinct CompanyID, CompanyName FROM tblCompany ";
                    SQLString += " where CompanyID IN " + GetUserManagedCompanyString();
                    SQLString += " order by CompanyName";
                    return QueryMultipleList(SQLString );

                case (short)Helpers.Helpers.ListType.property:
                    SQLString = "SELECT [tblProperty].[PropertyID] , [tblProperty].[Address] FROM [tblProperty],mCompanyProperty ";
                    SQLString += " WHERE tblProperty.PropertyID =mCompanyProperty.PropertyID and  mCompanyProperty.CompanyID IN " + GetUserManagedCompanyString() + " Order by Address";
                    return QueryMultipleList(SQLString);

                case (short)Helpers.Helpers.ListType.unit:
                    SQLString = "SELECT [UnitID] ,([tblProperty].Address +'-' +[tblPropertyUnit].UnitName) as UnitName  FROM [tblPropertyUnit], [tblProperty],mCompanyProperty WHERE [tblPropertyUnit].PropertyID = [tblProperty].PropertyID AND tblProperty.PropertyID= mCompanyProperty.PropertyID ";
                    SQLString += " AND mCompanyProperty.CompanyID in " + GetUserManagedCompanyString() + " Order by UnitName";
                    return QueryMultipleList(SQLString);

                case (short)Helpers.Helpers.ListType.allTenant:
                    SQLString = "SELECT distinct cUser.UserID, cUser.FirstName + ' ' + cUser.LastName as UserName FROM cUser INNER JOIN tblCompanyUser on tblCompanyUser.UserID = cUser.UserID";
                    SQLString += " WHERE tblCompanyUser.CompanyID in "+ GetUserManagedCompanyString() ;
                    SQLString += " AND tblCompanyUser.RoleID = 6 order by UserName";
                    return QueryMultipleList(SQLString);

                case (short)Helpers.Helpers.ListType.allTenantWithUnit:
                    SQLString = "select distinct tblTenant.TenantID, (cUser.LastName + ', ' + cuser.FirstName + ' -- ' + tblProperty.Address + '-' + tblPropertyUnit.UnitName) as description";
                    SQLString += " from cUser, tblTenant, tblProperty, tblPropertyUnit, tblCompanyUser,mCompanyProperty ";
                    SQLString += " where cUser.UserID = tblTenant.UserID AND tblTenant.UnitID = tblPropertyUnit.UnitID ";
                    SQLString += " and tblPropertyUnit.PropertyID = mCompanyProperty.PropertyID ";
                    SQLString += " and tblPropertyUnit.PropertyID = tblProperty.PropertyID ";
                    SQLString += " AND mCompanyProperty.PropertyID = tblProperty.PropertyID ";
                    SQLString += " and mCompanyProperty.CompanyID = tblCompanyUser.CompanyID";
                    SQLString += " and tblTenant.StatusID  <3 ";
                    SQLString += " and tblCompanyUser.CompanyID IN " + GetUserManagedCompanyString();
                    SQLString += " ORDER BY description";
                    return QueryMultipleList(SQLString);

                case (short)Helpers.Helpers.ListType.bankaccount:
                    SQLString = "SELECT distinct tblCompanyFinancialAccount.FinancialAccountID, AccountName FROM tblAccount INNER JOIN tblCompanyFinancialAccount on tblCompanyFinancialAccount.FinancialAccountID = tblAccount.FinancialAccountID";
                    SQLString += " and tblCompanyFinancialAccount.CompanyID in "+GetUserManagedCompanyString () + " order by AccountName";
                    return QueryMultipleList(SQLString);
                case (short)Helpers.Helpers.ListType.contractor:
                    SQLString = "SELECT distinct cUser.UserID, cUser.FirstName + ' ' + cUser.LastName as UserName FROM cUser INNER JOIN tblCompanyUser on tblCompanyUser.UserID = cUser.UserID";
                    SQLString += " WHERE tblCompanyUser.CompanyID in " + GetUserManagedCompanyString() + " order by UserName";
                    SQLString += " AND tblCompanyUser.RoleID = 5";
                    return QueryMultipleList(SQLString);
                case (short)Helpers.Helpers.ListType.allUser:
                    SQLString = "SELECT distinct cUser.UserID, cUser.FirstName + ' ' + cUser.LastName as UserName FROM cUser INNER JOIN tblCompanyUser on tblCompanyUser.UserID = cUser.UserID";
                    SQLString += " WHERE tblCompanyUser.CompanyID in " + GetUserManagedCompanyString() + " order by UserName";
                    return QueryMultipleList(SQLString);
                case (short)Helpers.Helpers.ListType.allExpenseCategory:
                    SQLString = "SELECT distinct CategoryID, CategoryName FROM cExpenseCategory order by CategoryName";
                    return QueryMultipleList(SQLString);
                case (short)Helpers.Helpers.ListType.allStatus :
                    SQLString = "SELECT distinct StatusTypeID, name FROM cStatusType order by name";
                    return QueryMultipleList(SQLString);
                case (short)Helpers.Helpers.ListType.allAccountType:
                    SQLString = "SELECT distinct AccountTypeID, name FROM cAccountType order by name";
                    return  QueryECommerceMultipleList(SQLString);
                case (short)Helpers.Helpers.ListType.allSku:
                    SQLString = "SELECT ID,sku FROM cSku where Active = 1 ORDER BY sku ";
                    return QueryMultipleList(SQLString);
                case (short)Helpers.Helpers.ListType.allECommerceVendor:
                    SQLString = "SELECT distinct id, Company_Name FROM ebay_partner ";
                    SQLString += " where ebay_companyID IN " + GetUserManagedCompanyString();
                    SQLString += " order by Company_Name";
                    return QueryECommerceMultipleList(SQLString);
                case (short)Helpers.Helpers.ListType.allWarehouse:
                    SQLString = "SELECT distinct id, store_name FROM ebay_store order by store_name";
                    return QueryECommerceMultipleList(SQLString);
                case (short)Helpers.Helpers.ListType.allECommerceCompany:
                    SQLString = "SELECT distinct CompanyID, CompanyName FROM tblCompany ";
                    SQLString += " where CompanyID IN " + GetUserManagedCompanyString();
                    SQLString += " order by CompanyName";
                    return QueryECommerceMultipleList(SQLString);
                case (short)Helpers.Helpers.ListType.allECommercePurchaseStatus:
                    SQLString = "SELECT distinct STATUSID, name FROM c_status WHERE categoryID = 1 order by STATUSID";
                    return QueryECommerceMultipleList(SQLString);
                default:
                    return null;
            }


        }

        public List<CheckBoxListItem> GetCheckBoxList(short type)
        {
            string SQLString = null;
            switch (type)
            {
                case (short)Helpers.Helpers.ListType.company:
                    SQLString = "SELECT distinct CompanyID, CompanyName FROM tblCompany ";
                    SQLString += " where CompanyID IN " + GetUserManagedCompanyString();
                    SQLString += " order by CompanyName";
                    return QueryCheckList(SQLString);

                case (short)Helpers.Helpers.ListType.property:
                    SQLString = "SELECT [tblProperty].[PropertyID] , [tblProperty].[Address] FROM [tblProperty],mCompanyProperty, tblCompanyUser WHERE tblProperty.PropertyID =mCompanyProperty.PropertyID and  mCompanyProperty.CompanyID = tblCompanyUser.CompanyID and tblCompanyUser.RoleID <5 and tblCompanyUser.UserID = " + Session["UserID"] + " Order by Address";
                    return QueryCheckList(SQLString);

                case (short)Helpers.Helpers.ListType.unit:
                    SQLString = "SELECT [UnitID] ,([tblProperty].Address +'-' +[tblPropertyUnit].UnitName) as UnitName  FROM [tblPropertyUnit], [tblProperty],mCompanyProperty, tblCompanyUser WHERE [tblPropertyUnit].PropertyID = [tblProperty].PropertyID AND tblProperty.PropertyID= mCompanyProperty.PropertyID AND mCompanyProperty.CompanyID = tblCompanyUser.CompanyID and tblCompanyUser.RoleID <5 and tblCompanyUser.UserID =" + Session["UserID"];
                    SQLString += " order by UnitName";
                    return QueryCheckList(SQLString);

                case (short)Helpers.Helpers.ListType.allTenant:
                    SQLString = "SELECT distinct cUser.UserID, (cUser.FirstName + ' ' + cUser.LastName) as FullName FROM cUser INNER JOIN tblCompanyUser on tblCompanyUser.UserID = cUser.UserID";
                    SQLString += " WHERE tblCompanyUser.CompanyID in " + GetUserManagedCompanyString();
                    SQLString += " AND tblCompanyUser.RoleID = 6 order by FullName";
                    return QueryCheckList(SQLString);

                case (short)Helpers.Helpers.ListType.bankaccount:
                    SQLString = "SELECT distinct tblCompanyFinancialAccount.FinancialAccountID, AccountName FROM tblAccount INNER JOIN tblCompanyFinancialAccount on tblCompanyFinancialAccount.FinancialAccountID = tblAccount.FinancialAccountID";
                    SQLString += " and tblCompanyFinancialAccount.CompanyID in " + GetUserManagedCompanyString();
                    SQLString += " order by AccountName";
                    return QueryCheckList(SQLString);
                case (short)Helpers.Helpers.ListType.contractor:
                    SQLString = "SELECT distinct cUser.UserID, (cUser.FirstName + ' ' + cUser.LastName) as FullName FROM cUser INNER JOIN tblCompanyUser on tblCompanyUser.UserID = cUser.UserID";
                    SQLString += " WHERE tblCompanyUser.CompanyID in " + GetUserManagedCompanyString();
                    SQLString += " AND tblCompanyUser.RoleID = 5";
                    SQLString += " order by FullName";
                    return QueryCheckList(SQLString);

                case (short)Helpers.Helpers.ListType.allUser:
                    SQLString = "SELECT distinct cUser.UserID, (cUser.FirstName + ' ' + cUser.LastName) as FullName FROM cUser INNER JOIN tblCompanyUser on tblCompanyUser.UserID = cUser.UserID";
                    SQLString += " WHERE tblCompanyUser.CompanyID in " + GetUserManagedCompanyString();
                    SQLString += " order by FullName";
                    return QueryCheckList(SQLString);

                case (short)Helpers.Helpers.ListType.allStatus:
                    SQLString = "SELECT distinct StatusTypeID, name FROM cStatusType order by name";
                    return QueryCheckList(SQLString);

                case (short)Helpers.Helpers.ListType.allExpenseCategory:
                    SQLString = "SELECT distinct CategoryID, CategoryName FROM cExpenseCategory order by CategoryName";
                    return QueryCheckList(SQLString);

                case (short)Helpers.Helpers.ListType.allRoles:
                    SQLString = "SELECT distinct RoleID, RoleName FROM cRole order by RoleName";
                    return QueryCheckList(SQLString);
                default:
                    return null;
            }


        }
  
        public EmptyResult ExecutionError(string message)
        {
            Response.StatusCode = 550;
            Response.Write(message);
            return new EmptyResult();
        }


        public List<dropdown_list> GetDropdownDrillDownLevel()
        {
            List<dropdown_list> result = new List<dropdown_list>();
            foreach (Helpers.Helpers.DrilldownLevel enumValue in Enum.GetValues(typeof(Helpers.Helpers.DrilldownLevel)))
            {
                dropdown_list row0 = new dropdown_list();
                row0.id = (int)enumValue;
                row0.description = enumValue.ToString();
                result.Add(row0);
            }
            return result;
        }
        

        // A drop-down listbox for comparision variable of the report
        public List<dropdown_list> GetDropdownReportList()
        {
            List<dropdown_list> result = new List<dropdown_list>();
            foreach (Helpers.Helpers.ReportList enumValue in Enum.GetValues(typeof(Helpers.Helpers.ReportList)))
            {
                dropdown_list row0 = new dropdown_list();
                row0.id = (int)enumValue;
                row0.description = enumValue.ToString();
                result.Add(row0);
            }
            return result;
        }

        // A drop-down listbox for comparision variable of the report
        public List<dropdown_list> GetDropdownChartType()
        {
            List<dropdown_list> result = new List<dropdown_list>();
            foreach (Helpers.Helpers.ChartType enumValue in Enum.GetValues(typeof(Helpers.Helpers.ChartType)))
            {
                dropdown_list row0 = new dropdown_list();
                row0.id = (int)enumValue;
                row0.description = enumValue.ToString();
                result.Add(row0);
            }
            return result;
        }
        // A drop-down listbox for comparision variable of the report
        public List<dropdown_list> GetDropdownValueType()
        {
            List<dropdown_list> result = new List<dropdown_list>();
            foreach (Helpers.Helpers.ValueType enumValue in Enum.GetValues(typeof(Helpers.Helpers.ValueType)))
            {
                dropdown_list row0 = new dropdown_list();
                row0.id = (int)enumValue;
                row0.description = enumValue.ToString();
                result.Add(row0);
            }
            return result;
        }

        // Execute a SQL query against the RadiologyCentral database and return a SQLDataReader object
        public static SqlDataReader ExecuteSQLQuery(String SQLString)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["SunriseConnectionString"].ConnectionString; // Get the connection string
            SqlConnection conn = new SqlConnection(connectionString); // Create a connection
            using (SqlCommand cmd = new SqlCommand(SQLString, conn))
            {
                conn.Open(); // Open the connection
                SqlDataReader reader = cmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection); // Perform the SQL query. Using CommandBehavior.CloseConnection, the connection will be closed when the IDataReader is closed
                return reader;
            }
        }


        // Get the procedures from the DB for the drop-down listbox
        public List<dropdown_list> GetDropdownSKU()
        {
            List<dropdown_list> result = new List<dropdown_list>();
            string SQLString = "SELECT sku FROM cSku where Active = 1 ORDER BY sku ";
            SqlDataReader reader = ExecuteSQLQuery(SQLString);
            while (reader.Read()) // Read each result row and extract the data
            {
                dropdown_list row = new dropdown_list();
                string sku = reader[0].ToString();
                row.abbre = sku;
                row.description = sku;
                result.Add(row);
            }
            reader.Close();
            return result;
        }
        // Get the procedures from the DB for the drop-down listbox
        public List<dropdown_list> GetDropdownAccount()
        {
            List<dropdown_list> result = new List<dropdown_list>();
            string SQLString = "SELECT p.id, p.description, p.code FROM procedures as p ORDER BY p.description";
            SqlDataReader reader = ExecuteSQLQuery(SQLString);
            while (reader.Read()) // Read each result row and extract the data
            {
                dropdown_list row = new dropdown_list();
                row.id = long.Parse(reader[0].ToString());
                row.description = reader[1].ToString() + " [" + reader[2].ToString() + "]";
                result.Add(row);
            }
            reader.Close();
            return result;
        }

        // A drop-down listbox for comparision variable of the report
        public List<dropdown_list> GetDropdownComparisonVariable()
        {
            List<dropdown_list> result = new List<dropdown_list>();
            foreach (Helpers.Helpers.ComparisionType enumValue in Enum.GetValues(typeof(Helpers.Helpers.ComparisionType)))
            {
                dropdown_list row0 = new dropdown_list();
                row0.id = (int)enumValue;
                row0.description = enumValue.ToString();
                result.Add(row0);
            }
            return result;
        }

        // Get the procedures from the DB for the drop-down listbox
        public List<dropdown_list> GetDropdownVendors()
        {
            List<dropdown_list> result = new List<dropdown_list>();
            foreach (Helpers.Helpers.ComparisionType enumValue in Enum.GetValues(typeof(Helpers.Helpers.VendorType)))
            {
                dropdown_list row0 = new dropdown_list();
                row0.id = (int)enumValue;
                row0.description = enumValue.ToString();
                result.Add(row0);
            }
            return result;
        }

        // Get the procedures from the DB for the drop-down listbox
        public List<dropdown_list> GetDropdownAccounts()
        {
            List<dropdown_list> result = new List<dropdown_list>();
            foreach (Helpers.Helpers.AccountType  enumValue in Enum.GetValues(typeof(Helpers.Helpers.AccountType)))
            {
                dropdown_list row0 = new dropdown_list();
                row0.id = (int)enumValue;
                row0.description = enumValue.ToString();
                result.Add(row0);
            }
            return result;
        }
        // Get the procedures from the DB for the drop-down listbox
        public List<dropdown_list> GetDropdownWarehouse()
        {

            List<dropdown_list> result = new List<dropdown_list>();
            foreach (Helpers.Helpers.WarehouseType  enumValue in Enum.GetValues(typeof(Helpers.Helpers.WarehouseType)))
            {
                dropdown_list row0 = new dropdown_list();
                row0.id = (int)enumValue;
                row0.description = enumValue.ToString();
                result.Add(row0);
            }
            return result;
        }

        //// Get the procedures from the DB for the drop-down listbox
        //public List<dropdown_list> GetDropdownSku()
        //{
        //    List<dropdown_list> result = get;
        //    result.Add(new dropdown_list("1", "02RSA1209ABK"));
        //    result.Add(new dropdown_list("2", "02RSA4508ABK"));
        //    result.Add(new dropdown_list("3", "02HLA1202ABK"));
        //    return result;
        //}


        // Get the procedures from the DB for the drop-down listbox
        public List<dropdown_list> GetDropdownStates()
        {
            List<dropdown_list> result = new List<dropdown_list>();
            result.Add(new dropdown_list("AL", "Alabama"));
            result.Add(new dropdown_list("AK", "Alaska"));
            result.Add(new dropdown_list("AZ", "Arizona"));
            result.Add(new dropdown_list("AR", "Arkansas"));
            result.Add(new dropdown_list("CA", "California"));
            result.Add(new dropdown_list("CO", "Colorado"));
            result.Add(new dropdown_list("CT", "Connecticut"));
            result.Add(new dropdown_list("DE", "Delaware"));
            result.Add(new dropdown_list("DC", "District Of Columbia"));
            result.Add(new dropdown_list("FL", "Florida"));
            result.Add(new dropdown_list("GA", "Georgia"));
            result.Add(new dropdown_list("HI", "Hawaii"));
            result.Add(new dropdown_list("ID", "Idaho"));
            result.Add(new dropdown_list("IL", "Illinois"));
            result.Add(new dropdown_list("IN", "Indiana"));
            result.Add(new dropdown_list("IA", "Iowa"));
            result.Add(new dropdown_list("KS", "Kansas"));
            result.Add(new dropdown_list("KY", "Kentucky"));
            result.Add(new dropdown_list("LA", "Louisiana"));
            result.Add(new dropdown_list("ME", "Maine"));
            result.Add(new dropdown_list("MD", "Maryland"));
            result.Add(new dropdown_list("MA", "Massachusetts"));
            result.Add(new dropdown_list("MI", "Michigan"));
            result.Add(new dropdown_list("MN", "Minnesota"));
            result.Add(new dropdown_list("MS", "Mississippi"));
            result.Add(new dropdown_list("MO", "Missouri"));
            result.Add(new dropdown_list("MT", "Montana"));
            result.Add(new dropdown_list("NE", "Nebraska"));
            result.Add(new dropdown_list("NV", "Nevada"));
            result.Add(new dropdown_list("NH", "New Hampshire"));
            result.Add(new dropdown_list("NJ", "New Jersey"));
            result.Add(new dropdown_list("NM", "New Mexico"));
            result.Add(new dropdown_list("NY", "New York"));
            result.Add(new dropdown_list("NC", "North Carolina"));
            result.Add(new dropdown_list("ND", "North Dakota"));
            result.Add(new dropdown_list("OH", "Ohio"));
            result.Add(new dropdown_list("OK", "Oklahoma"));
            result.Add(new dropdown_list("OR", "Oregon"));
            result.Add(new dropdown_list("PA", "Pennsylvania"));
            result.Add(new dropdown_list("RI", "Rhode Island"));
            result.Add(new dropdown_list("SC", "South Carolina"));
            result.Add(new dropdown_list("SD", "South Dakota"));
            result.Add(new dropdown_list("TN", "Tennessee"));
            result.Add(new dropdown_list("TX", "Texas"));
            result.Add(new dropdown_list("UT", "Utah"));
            result.Add(new dropdown_list("VT", "Vermont"));
            result.Add(new dropdown_list("VA", "Virginia"));
            result.Add(new dropdown_list("WA", "Washington"));
            result.Add(new dropdown_list("WV", "West Virginia"));
            result.Add(new dropdown_list("WI", "Wisconsin"));
            result.Add(new dropdown_list("WY", "Wyoming"));
            //canada
            result.Add(new dropdown_list("AB", "Alberta"));
            result.Add(new dropdown_list("BC", "British Columbia"));
            result.Add(new dropdown_list("NL", "Newfoundland and Labrador"));
            result.Add(new dropdown_list("NS", "Nova Scotia"));
            result.Add(new dropdown_list("NT", "Northwest Territories"));
            result.Add(new dropdown_list("NU", "Nunavut"));
            result.Add(new dropdown_list("ON", "Ontario"));
            result.Add(new dropdown_list("PE", "Prince Edward Island"));
            result.Add(new dropdown_list("QC", "Quebec"));
            result.Add(new dropdown_list("SK", "Saskatchewan"));
            result.Add(new dropdown_list("YT", "Yukon"));

            return result;
        }

        //HONG added this function for the comparision report page
        public List<dropdown_list> GetList(short type, string[] ids)
        {
            List<dropdown_list> result = new List<dropdown_list>();
            string SQLString = null;
            string connectionString = null;
            switch (type)
            {
                case (short)Helpers.Helpers.ListType.allSku :
                    SQLString = "SELECT DISTINCT ID, SKU FROM cSKU where sku in ('" + String.Join("','", ids) + "') ORDER BY SKU";
                    connectionString = Helpers.Helpers.GetAppConnectionString();
                    break;

                case (short)Helpers.Helpers.ListType.allStore :
                    SQLString = "SELECT ReportID, ReportName from cCentralReports where ReportID in (" + String.Join(",", ids) + ") ORDER BY ReportName";
                    connectionString = Helpers.Helpers.GetAppConnectionString();
                    break;
                case (short)Helpers.Helpers.ListType.allState:
                    List<dropdown_list> list = GetDropdownStates();
                    List<dropdown_list> newList = new List<dropdown_list>();
                    for(int i = 0; i < ids.Length; i++)
                    {
                        for(int j = 0; j < list.Count; j++)
                        {
                            if(list[j].abbre .Equals (ids[i]))
                            {
                                newList.Add(list[j]);
                                break;
                            }
                        }
                    }
                    return newList ;
            }
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(SQLString, connection); // Create the Command and Parameter objects
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read()) // Read each result row and extract the data
                {
                    dropdown_list row = new dropdown_list();
                    row.id = long.Parse(reader[0].ToString());
                    row.description = reader[1].ToString();
                    result.Add(row);
                }
                reader.Close();
                connection.Close();
            }
            return result;
        }

    }

  

    static class MyExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    public static class ThreadSafeRandom
    {
        [ThreadStatic]
        private static Random Local;

        public static Random ThisThreadsRandom
        {
            get { return Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
        }
    }
    
}
