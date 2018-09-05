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
using PropertyManagement.ViewModels.Property;
using System.Data;
using System.Text;
using PropertyManagement.Helpers;

namespace PropertyManagement.Models
{
    public class TenantManager
    {
        public static Tenant GetByID(int id)
        {
            StringBuilder sbOperation = new StringBuilder();
            sbOperation.Append("Select distinct tblTenant.*,  tblProperty.Address,cUser.*, tblPropertyUnit.UnitName,tblPropertyUnit.UnitID, cStatusType.Name as StatusName, Balance, PaidSecurity ");
            sbOperation.Append("from tblTenant ");
            sbOperation.Append(" left outer JOIN  tblPropertyUnit on tblPropertyUnit.UnitID =  tblTenant.UnitID ");
            sbOperation.Append(" left outer JOIN  tblProperty ON tblProperty.PropertyID = tblPropertyUnit.PropertyID ");
            sbOperation.Append(" left outer JOIN mCompanyProperty on mCompanyProperty.PropertyID = tblProperty.PropertyID ");
            sbOperation.Append(" left outer JOIN cUser on cUser.UserID =  tblTenant.UserID ");
            sbOperation.Append(" left outer JOIN cStatusType on cStatusType.StatusTypeID = tblTenant.StatusID ");
            sbOperation.Append(" LEFT OUTER JOIN (select sum(DueAmount - Amount) as balance, contractorid from tblUnitOperation group by contractorid) as OperationSummary on OperationSummary.ContractorID = tblTenant.UserID ");
            sbOperation.Append(" LEFT OUTER JOIN (select sum(Amount) as PaidSecurity, contractorid from tblUnitOperation where categoryid=32 group by contractorid) as SecuritySummary on SecuritySummary.ContractorID = tblTenant.UserID ");
            sbOperation.Append(" where tblTenant.TenantID=" + id);
            using (SqlDataAdapter adapter = new SqlDataAdapter(sbOperation.ToString (), Helpers.Helpers.GetAppConnectionString()))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                if (tb != null && tb.Rows.Count > 0)
                {
                    DataRow dr = tb.Rows[0];
                    return FillInTenantWithData(dr);
                }
                return null;
            }
        }

        public static Tenant FillInTenantWithData(DataRow dr)
        {
            Tenant row = new Tenant();
            if (dr["TenantID"] != DBNull.Value)
            {
                row.TenantID = Int32.Parse(dr["TenantID"].ToString());
            }
            if (dr["UserID"] != DBNull.Value)
            {
                row.UserID = Int32.Parse(dr["UserID"].ToString());
            }
            if (dr["UnitID"] != DBNull.Value)
            {
                row.UnitId = Int32.Parse(dr["UnitID"].ToString());
            }
            if (dr["StartDate"] != DBNull .Value )
            {
                row.StartDate = DateTime.Parse(dr["StartDate"].ToString());
            }
            if (dr["FirstName"] != DBNull.Value)
            {
                row.FirstName = dr["FirstName"].ToString();
            }
            if (dr["LastName"] != DBNull.Value)
            {
                row.LastName = dr["LastName"].ToString();
            }
            if (dr["UnitName"] != DBNull.Value)
            {
                row.Address = dr["Address"].ToString() + " -- " + dr["UnitName"].ToString();
            }
            if (dr["StatusName"] != DBNull.Value)
            {
                row.StatusName = dr["StatusName"].ToString();
            }
            if (dr["StatusID"] != DBNull.Value)
            {
                row.StatusID = int.Parse (dr["StatusID"].ToString());
            }
            if (dr["MonthlyPayment"] != DBNull.Value)
            {
                row.MonthlyPayment = double.Parse(dr["MonthlyPayment"].ToString());
            }
            if (dr["SecurityDeposit"] != DBNull.Value)
            {
                row.SecurityDeposit = double.Parse(dr["SecurityDeposit"].ToString());
            }
            if (dr["PaidSecurity"] != DBNull.Value)
            {
                row.PaidSecurityDeposit = double.Parse(dr["PaidSecurity"].ToString());
            }
            if (dr["LeaseTerm"] != DBNull.Value)
            {
                row.LeaseTerm = int.Parse(dr["LeaseTerm"].ToString());
            }
            if (dr["Note"] != DBNull.Value)
            {
                row.Note = dr["Note"].ToString();
            }
            if(dr["Balance"] !=DBNull .Value )
            {
                row.Balance = double.Parse(dr["Balance"].ToString());
            }
            return row;
        }

        public static void EditLease(Tenant model)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the Command and Parameter objects.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();
                     
                    cmd.CommandText = "Update tblTenant set UserID=" + model.UserID + ", UnitID = " + model.UnitId + " , StartDate='" + model.StartDate  + "', LeaseTerm=";
                    cmd.CommandText += model.LeaseTerm + ", MonthlyPayment=" + model.MonthlyPayment + ", SecurityDeposit=" + model.SecurityDeposit + ", StatusID=" + model.StatusID + ", Note='" + model.Note+"' ";
                    cmd.CommandText += " where TenantID=" + model.TenantID;
                    cmd.ExecuteNonQuery();

                    //email reminder of the new lease
                    if (model.IsEmailReceipt)
                    {
                        Email.EmailLease(model.TenantID, (int)Helpers.Helpers.EmailType.LeaseChange);
                    }
                    connection.Close();
                }
                catch (Exception ex)
                {
                    string exms = ex.Message;
                }
            }
        }
     
        public static void TerminateLease(int id)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the Command and Parameter objects.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();
                    Tenant model = TenantManager.GetByID(id);

                    cmd.CommandText = "Update tblTenant set StatusID=" + (int)Helpers .Helpers .StatusType .Close  + ", TerminateDate='" + DateTime .Now  + "' ";
                    cmd.CommandText += " where TenantID=" + id;
                    cmd.ExecuteNonQuery();
                    Email.EmailLease(model.TenantID, (int)Helpers.Helpers.EmailType.LeaseTermination);
                    
                    connection.Close();
                }
                catch (Exception ex)
                {
                    string exms = ex.Message;
                }
            }
        }

        public static void Delete(int ID)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the Command and Parameter objects.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();
                    cmd.CommandText = "DELETE FROM tblTenant where TenantID=" + ID;
                    cmd.ExecuteNonQuery();

                    connection.Close();
                }
                catch (Exception ex)
                {
                    string exms = ex.Message;
                }

            }
        }
    }
}