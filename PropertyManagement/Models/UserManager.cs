using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Data.SqlClient;
using System.Text;
using System.Data;
using PropertyManagement.Helpers;
using PropertyManagement.ViewModels.User;

namespace PropertyManagement.Models
{
    public static class UserManager
    {

        public static PropertyManagement.Models.User GetByID(int id)
        {
            string SQLString = "select cUser.* from cUser where UserID =  " + id;
            using (SqlDataAdapter adapter = new SqlDataAdapter(SQLString, Helpers.Helpers.GetAppConnectionString()))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                if (tb != null && tb.Rows.Count > 0)
                {
                    DataRow dr = tb.Rows[0];
                    return FillInUserWithData(dr);
                }
                return null;
            }
        }
    

        public static void Add(AddUserVM model, int newRole, List<int> newCompanys)
        {
            using (SqlConnection connection = new SqlConnection(Helpers .Helpers .GetAppConnectionString ()))
            {
                try
                {
                    // Create the user record.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();

                    SqlParameter IDParameter = new SqlParameter("@UserID", SqlDbType.SmallInt);
                    IDParameter.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(IDParameter);

                    cmd.CommandText = "insert into cUser(UserName, FirstName, LastName, Password, CellPhone, HomePhone, EmailAddress, Address, City, State, Zip, WebUrl, SSN, StatusID) ";
                    cmd.CommandText += " values ('" + model.UserName + "', '" + model.FirstName + "', '" + model.LastName + "', '" + model.Password + "','" + model.CellPhone + "', '" + model.HomePhone + "', '";
                    cmd.CommandText += model.EmailAddress + "', '" + model.Address + "', '" + model.City + "', '" + model.State + "', '" + model.Zip + "', '" + model.WebUrl + "', '" + model.SSN + "', 1) SET @UserID=SCOPE_IDENTITY();";
                    cmd.ExecuteNonQuery();
                    model.UserID = (short)IDParameter.Value;
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

            //add in all roles and reports
            InsertUserCompany(model.UserID, newCompanys, newRole);

        }


        public static void Edit(EditUserVM model, List<int> newRoles, List<int> newReports)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the user record.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();
                    StringBuilder sb = new StringBuilder();

                    sb.Append("update cUser set UserName='" + model.UserName + "', FirstName='" + model.FirstName + "', LastName='" + model.LastName);
                    sb.Append("', Password='" + model.Password + "', CellPhone='" + model.CellPhone + "', HomePhone='" + model.HomePhone);
                    sb.Append("', EmailAddress='" + model.EmailAddress + "', Address='" + model.Address + "', City='" + model.City + "', State='" + model.State);
                    sb.Append ("', Zip='" + model.Zip +"', WebUrl='"+model.WebUrl +"', SSN='"+ model.SSN +"', StatusID="+model.StatusID );
                    sb.Append("where UserID=" + model.UserID );
                    cmd.CommandText = sb.ToString ();
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
            
            List<CheckBoxListItem> userOldReports = GetAllCompanyForUser (model.UserID);
            for (int i = 0; i <newReports.Count; i++)
            {
                for (int j = 0; j <userOldReports.Count; j++)
                {
                    if ((int)newReports[i] == userOldReports[j].ID)
                    {
                        userOldReports.RemoveAt(j);
                        newReports.RemoveAt(i);
                        i--;
                        j--;
                        break;
                    }
                }
            }

            if (newReports != null && newReports.Count > 0)
            {
                InsertUserCompany(model.UserID, newReports, newRoles[0]);
            }

            //delete remain old roles in the queue
            if (userOldReports != null && userOldReports.Count > 0)
            {
                DeleteUserReports(model.UserID, userOldReports);
            }

            //updated the user record

        }

        public static void DeleteUserRoles(int id, List<CheckBoxListItem> oldRoles)
        {
            using (SqlConnection connection = new SqlConnection(Helpers .Helpers .GetAppConnectionString ()))
            {
                try
                {
                    // Create the Command and Parameter objects.
                    SqlCommand command = new SqlCommand("", connection);
                    connection.Open();

                    //create non-exist new roles in the queue
                    foreach (var oldRole in oldRoles)
                    {
                        command.CommandText = "delete from mApplicationUserRole where UserID="+id+" and ApplicationRoleID="+ oldRole.ID;
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
                catch (Exception ex)
                {
                    string exms = ex.Message;
                }

            }
        }

        public static void InsertUserCompany(int userID, List<int> newCompanys, int newRole)
        {
            using (SqlConnection connection = new SqlConnection(Helpers .Helpers .GetAppConnectionString ()))
            {
                try
                {
                    // Create the Command and Parameter objects.
                    SqlCommand command = new SqlCommand("", connection);
                    connection.Open();

                    //create non-exist new roles in the queue
                    foreach (var newCompany in newCompanys)
                    {
                        command.CommandText = "insert into tblCompanyUser (UserID, CompanyID, roleID, StartDate) values (" + userID + "," + newCompany + ", "+ newRole +", '"+ DateTime .Now.ToString() +"')";
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
                catch (Exception ex)
                {
                    string exms = ex.Message;
                }

            }
        }

        public static void DeleteUserReports(int id, List<CheckBoxListItem> oldReports)
        {
            using (SqlConnection connection = new SqlConnection(Helpers .Helpers .GetAppConnectionString ()))
            {
                // Create the Command and Parameter objects.
                SqlCommand command = new SqlCommand("", connection);
                try
                {
                    connection.Open();

                    //create non-exist new roles in the queue
                    foreach (var oldReport in oldReports)
                    {
                        command.CommandText = "delete from tblCompanyUser where UserID=" + id + " and CompanyID=" + oldReport.ID;
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
                catch (Exception ex)
                {
                    string exms = ex.Message;
                }

            }
        }

        public static List<CheckBoxListItem> GetAllRoles()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT distinct RoleID, RoleName FROM cRole order by RoleName");
            List<CheckBoxListItem> allRoles = new List<CheckBoxListItem>();
            using (SqlConnection connection = new SqlConnection(Helpers .Helpers .GetAppConnectionString ()))
            {
                // Create the Command and Parameter objects.
                SqlCommand command = new SqlCommand(sb.ToString(), connection);

                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        CheckBoxListItem role = new CheckBoxListItem();
                        role.ID = Int32.Parse(reader[0].ToString());
                        role.Display = reader[1].ToString();
                        allRoles.Add(role);
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                finally 
                {
                    connection.Close();
                }
                return allRoles;
            }
        }

        public static List<CheckBoxListItem> GetAllRoles(int roleID)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT distinct RoleID, RoleName FROM cRole WHERE ROLEID>="+roleID +" order by RoleName");
            List<CheckBoxListItem> allRoles = new List<CheckBoxListItem>();
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                // Create the Command and Parameter objects.
                SqlCommand command = new SqlCommand(sb.ToString(), connection);

                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        CheckBoxListItem role = new CheckBoxListItem();
                        role.ID = Int32.Parse(reader[0].ToString());
                        role.Display = reader[1].ToString();
                        allRoles.Add(role);
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    connection.Close();
                }
                return allRoles;
            }
        }

        public static List<CheckBoxListItem> GetAllRolesForUser(int userid)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT distinct cRole.RoleId, cRole.RoleName FROM cUser INNER JOIN tblCompanyUser on tblCompanyUser.UserID = cUser.UserID");
            sb.Append(" INNER JOIN cRole on cRole.RoleID = tblCompanyUser.RoleID");
            sb.Append(" where tblCompanyUser.UserID ="+userid);
            List<CheckBoxListItem> allRoles = new List<CheckBoxListItem>();
            using (SqlConnection connection = new SqlConnection(Helpers .Helpers .GetAppConnectionString ()))
            {
                // Create the Command and Parameter objects.
                SqlCommand command = new SqlCommand(sb.ToString(), connection);
                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        CheckBoxListItem role = new CheckBoxListItem();
                        role.ID = Int32.Parse(reader[0].ToString());
                        role.Display = reader[1].ToString();
                        allRoles.Add(role);
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    string exms = ex.Message;
                }
                finally
                {
                    connection.Close();
                }
                return allRoles;
            }
        }

        public static int GetUserMostRightRole(int userid)
        {
            int roleID = 0;
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT TOP (1) cRole.RoleId FROM cUser INNER JOIN tblCompanyUser on tblCompanyUser.UserID = cUser.UserID");
            sb.Append(" INNER JOIN cRole on cRole.RoleID = tblCompanyUser.RoleID");
            sb.Append(" where tblCompanyUser.UserID =" + userid);
            sb.Append(" order by RoleID ASC");
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                // Create the Command and Parameter objects.
                SqlCommand command = new SqlCommand(sb.ToString(), connection);
                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        roleID = Int32.Parse(reader[0].ToString());
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    string exms = ex.Message;
                }
                finally
                {
                    connection.Close();
                }
                return roleID;
            }

        }
        public static List<CheckBoxListItem> GetAllCompanyForUser(int userid)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT distinct tblCompany.CompanyID, tblCompany.CompanyName FROM cUser INNER JOIN tblCompanyUser on tblCompanyUser.UserID = cUser.UserID");
            sb.Append(" INNER JOIN tblCompany on tblCompany.CompanyID = tblCompanyUser.CompanyID");
            sb.Append(" where tblCompanyUser.UserID =" + userid);
            List<CheckBoxListItem> allReports = new List<CheckBoxListItem>();
            using (SqlConnection connection = new SqlConnection(Helpers .Helpers .GetAppConnectionString ()))
            {
                // Create the Command and Parameter objects.
                SqlCommand command = new SqlCommand(sb.ToString(), connection);

                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        CheckBoxListItem role = new CheckBoxListItem();
                        role.ID = Int32.Parse(reader[0].ToString());
                        role.Display = reader[1].ToString();
                        allReports.Add(role);
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    string exms = ex.Message;
                }
                return allReports;
            }
        }

        public static List<User> GetByCompanyIDs(string[] companyIDs, int adminID)
        {
            //get the most right role for the user
            int roleId = GetUserMostRightRole(adminID);

            StringBuilder sb = new StringBuilder();
            sb.Append("select distinct cUser.*, cStatusType.Name as StatusName FROM cUser ");
            sb.Append (" left outer join cStatusType on cStatusType.StatusTypeID= cUser.StatusID");
            sb.Append (" INNER JOIN tblCompanyUser on tblCompanyUser.UserID = cUser.UserID AND tblCompanyUser.RoleID >= "+ roleId );
            if (companyIDs != null && companyIDs.Count() > 0 && !string.IsNullOrEmpty(companyIDs[0]))
            {
                sb.Append (" where tblCompanyUser.CompanyID in  (" + String.Join(",", companyIDs) + ")");
            }
            else
            {
                //get the companys only the owner can access
                sb.Append(" AND tblCompanyUser.CompanyID IN (" + PropertyManagement.Helpers.Helpers .GetUserManagedCompanyString(adminID.ToString ()) + ")");
            }
            using (SqlDataAdapter adapter = new SqlDataAdapter(sb.ToString (), Helpers.Helpers.GetAppConnectionString()))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                List<User> allUser = new List<User>();

                if (tb != null && tb.Rows.Count > 0)
                {
                    for (int i = 0; i <tb.Rows.Count; i++)
                    {
                        DataRow dr = tb.Rows[i];
                        allUser.Add(FillInUserWithData(dr));
                    }
                }
                return allUser;
            }
        }

        public static User FillInUserWithData(DataRow dr)
        {
            User user = new User();
            user.ID = int.Parse(dr["UserID"].ToString());
            if (dr.Table.Columns.Contains("UserName")) { user.UserName = dr["UserName"].ToString(); }
            user.FirstName = dr["FirstName"].ToString();
            user.LastName = dr["LastName"].ToString();
            if (dr.Table .Columns.Contains("Password")) { user.Password = dr["Password"].ToString(); }
            if (dr.Table .Columns.Contains("CellPhone")) { user.CellPhone = dr["CellPhone"].ToString(); }
            if (dr.Table .Columns.Contains("HomePhone")) { user.HomePhone = dr["HomePhone"].ToString(); }
            if (dr.Table .Columns.Contains("WorkPhone")) { user.WorkPhone = dr["WorkPhone"].ToString(); }
            if (dr.Table .Columns.Contains("EmailAddress")) { user.EmailAddress = dr["EmailAddress"].ToString(); }
            if (dr.Table .Columns.Contains("Address")) { user.Address = dr["Address"].ToString(); }
            if (dr.Table .Columns.Contains("City")) { user.City = dr["City"].ToString(); }
            if (dr.Table .Columns.Contains("State")) { user.State = dr["State"].ToString(); }
            if (dr.Table .Columns.Contains("Zip")) { user.Zip = dr["Zip"].ToString(); }
            if (dr.Table .Columns.Contains("WebUrl")) { user.WebUrl = dr["WebUrl"].ToString(); }
            if (dr.Table .Columns.Contains("SSN")) { user.SSN = dr["SSN"].ToString(); }
            if(dr.Table.Columns.Contains("CompanyName")) { user.Company = dr["CompanyName"].ToString(); }
            if (dr.Table .Columns.Contains("TotalAmount")) { user.Amount = double.Parse(dr["TotalAmount"].ToString()); }
            if (dr.Table .Columns.Contains("StatusID") && dr["StatusID"] != DBNull.Value  ) { user.StatusID = int.Parse(dr["StatusID"].ToString()); }
            if (dr.Table.Columns.Contains("CompanyID") && dr["CompanyID"] != DBNull.Value) { user.CompanyID = int.Parse(dr["CompanyID"].ToString()); }
            return user;
        }



    }
}