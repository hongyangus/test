using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using CCHMC.Radiology.Library.Authentication;
using CCHMC.AD.Library.Extensions;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using PropertyManagement.Models;
using PropertyManagement.ViewModels;
using System.Data;
using System.Text;
using PropertyManagement.Helpers;

namespace PropertyManagement.Models
{
    public class CompanyManager
    {
        public static PropertyManagement.Models.Company GetByID(int id)
        {
            string SQLString = "select tblCompany.*, cStatusType.*, cUser.FirstName +' ' +cUser.LastName as AdminName from tblCompany ";
            SQLString += " LEFT OUTER JOIN cUser ON cUser.UserID= tblCompany.AdminID ";
            SQLString += " LEFT OUTER JOIN cStatusType ON cStatusType.StatusTypeID= tblCompany.StatusID WHERE CompanyID =  " + id;
            using (SqlDataAdapter adapter = new SqlDataAdapter(SQLString, Helpers.Helpers.GetAppConnectionString()))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                Company role = new Company();
                if (tb != null && tb.Rows.Count > 0)
                {
                    DataRow dr = tb.Rows[0];
                    role.CompanyID = Int32.Parse(dr["CompanyID"].ToString());
                    role.CompanyName = dr["CompanyName"].ToString();
                    if (dr["StartDate"] != DBNull.Value) { role.StartDate = DateTime.Parse(dr["StartDate"].ToString()); }
                    role.AgentFirstName = dr["AgentFirstName"].ToString();
                    role.AgentLastName = dr["AgentLastName"].ToString();
                    role.CompanyCellPhone = dr["CompanyCellPhone"].ToString();
                    role.CompanyPhone = dr["CompanyPhone"].ToString();
                    role.Address = dr["Address"].ToString();
                    role.City = dr["City"].ToString();
                    role.State = dr["State"].ToString();
                    role.Zip = dr["Zip"].ToString();
                    role.EIN = dr["EIN"].ToString();
                    role.BankAccount = dr["BankAccount"].ToString();
                    role.RountingNo = dr["RountingNo"].ToString();
                    if (dr["Name"] != DBNull.Value)
                    {
                        role.Status = dr["Name"].ToString();
                    }
                    if (dr["StatusTypeID"] != DBNull.Value)
                    {
                        role.StatusID = Int32.Parse(dr["StatusTypeID"].ToString());
                    }
                    if (dr["AdminName"] != DBNull.Value)
                    {
                        role.AdminName = dr["AdminName"].ToString();
                    }
                    if (dr["AdminID"] != DBNull.Value)
                    {
                        role.AdminID = Int32.Parse(dr["AdminID"].ToString());
                    }
                }
                return role;
            }
        }

        public static List<PropertyManagement.Models.Company> GetByIDs(string[] companyIDs)
        {
            string SQLString = "select tblCompany.*, cStatusType.*, cUser.FirstName +' ' +cUser.LastName as AdminName from tblCompany ";
            SQLString += " LEFT OUTER JOIN cUser ON cUser.UserID= tblCompany.AdminID ";
            SQLString += " LEFT OUTER JOIN cStatusType ON cStatusType.StatusTypeID= tblCompany.StatusID ";
            if (companyIDs != null && companyIDs.Count() > 0 && !string.IsNullOrEmpty(companyIDs[0]))
            {
                SQLString += " WHERE CompanyID in  (" + String.Join(",", companyIDs) + ")";
            }
            using (SqlDataAdapter adapter = new SqlDataAdapter(SQLString, Helpers.Helpers.GetAppConnectionString()))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                Company company = null;
                List<PropertyManagement.Models.Company> allCompany = new List<PropertyManagement.Models.Company>();

                if (tb != null && tb.Rows.Count > 0)
                {
                    for (int i = 0; i <tb.Rows.Count; i++)
                    {
                        DataRow dr = tb.Rows[i];
                        company = new Company();
                        company.CompanyID = Int32.Parse(dr["CompanyID"].ToString());
                        company.CompanyName = dr["CompanyName"].ToString();
                        if (dr["StartDate"] != DBNull.Value) { company.StartDate = DateTime.Parse(dr["StartDate"].ToString()); }
                        company.AgentFirstName = dr["AgentFirstName"].ToString();
                        company.AgentLastName = dr["AgentLastName"].ToString();
                        company.CompanyCellPhone = dr["CompanyCellPhone"].ToString();
                        company.CompanyPhone = dr["CompanyPhone"].ToString();
                        company.Address = dr["Address"].ToString();
                        company.City = dr["City"].ToString();
                        company.State = dr["State"].ToString();
                        company.Zip = dr["Zip"].ToString();
                        company.EIN = dr["EIN"].ToString();
                        company.BankAccount = dr["BankAccount"].ToString();
                        company.RountingNo = dr["RountingNo"].ToString();
                        if (dr["Name"] != DBNull.Value)
                        {
                            company.Status = dr["Name"].ToString();
                        }
                        if (dr["StatusTypeID"] != DBNull.Value)
                        {
                            company.StatusID = Int32.Parse(dr["StatusTypeID"].ToString());
                        }
                        if (dr["AdminName"] != DBNull.Value)
                        {
                            company.AdminName = dr["AdminName"].ToString();
                        }
                        if (dr["AdminID"] != DBNull.Value)
                        {
                            company.AdminID = Int32.Parse(dr["AdminID"].ToString());
                        }
                        allCompany.Add(company);
                    }
                }
                return allCompany;
            }
        }


        public static void Add(AddCompanyVM model)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the user record.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();

                    SqlParameter IDParameter = new SqlParameter("@CompanyID", SqlDbType.SmallInt);
                    IDParameter.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(IDParameter);

                    cmd.CommandText = "insert into tblCompany (CompanyName, StartDate, CompanyPhone, CompanyCellPhone, Address, City, Zip, EIN, BankAccount, RountingNo, AdminID, State,StatusID ) ";
                    cmd.CommandText += " values ('" + model.CompanyName + "', '" + model.StartDate.ToShortDateString () + "', '" + model.CompanyPhone + "', '" + model.CompanyCellPhone + "','" + model.Address + "', '" + model.City + "', '";
                    cmd.CommandText += model.Zip + "', '" + model.EIN + "', '" + model.BankAccount + "', '" + model.RountingNo + "', '" + model.AdminID + "', '" + model.State + "', 1) SET @CompanyID=SCOPE_IDENTITY();";
                    cmd.ExecuteNonQuery();
                    model.CompanyID = (short)IDParameter.Value;

                    //create default admin as hong yang and the default admin
                    cmd.CommandText = " insert into tblCompanyUser(CompanyID, StartDate, RoleID, UserID, Note) values ("
                        + model.CompanyID + ",'" + DateTime.Now + "', 2, 1, 'create by default when company is formed' )";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = " insert into tblCompanyUser(CompanyID, StartDate, RoleID, UserID, Note) values ("
                        + model.CompanyID + ",'" + DateTime.Now + "', 2, "+ model.AdminID+", 'create by default when company is formed' )";
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
            //INSERT ADMIN ROLE
            List<int> companyId = new List<int>();
            companyId.Add(model.CompanyID);
            UserManager.InsertUserCompany(model.AdminID, companyId, (int)Helpers.Helpers.AdminRole);
        }


        public static void Edit(EditCompanyVM model)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the user record.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();
                    StringBuilder sb = new StringBuilder();

                    sb.Append("update tblCompany set CompanyName='" + model.CompanyName + "', StartDate='" + model.StartDate .ToShortDateString () + "', CompanyPhone='" + model.CompanyPhone);
                    sb.Append("', CompanyCellPhone='" + model.CompanyCellPhone + "', EIN='" + model.EIN + "', BankAccount='" + model.BankAccount);
                    sb.Append("', RountingNo='" + model.RountingNo + "', Address='" + model.Address + "', City='" + model.City + "', State='" + model.State);
                    sb.Append("', Zip='" + model.Zip + "', AdminID=" + model.AdminID + ", StatusID=" + model.StatusID);
                    sb.Append(" where CompanyID=" + model.CompanyID);
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

       
    }
}