using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Data.SqlClient;
using System.Text;
using System.Data;
using System.Net.Mail;
using System.Net;
using PropertyManagement.Helpers;


namespace PropertyManagement.Models
{
    public class BankAccountManager
    {
        public static BankAccount GetBankAccountByID(int id)
        {
            string SQLString = "SELECT distinct tblAccount.*, cAccountType.name, cUser.FirstName +' ' +cUser.LastName as OwnerName, tblCompanyFinancialAccount.CompanyID ";
            SQLString += " FROM tblAccount INNER JOIN tblCompanyFinancialAccount on tblCompanyFinancialAccount.FinancialAccountID = tblAccount.FinancialAccountID";
            SQLString += " left outer join cAccountType on cAccountType.AccountTypeID = tblAccount.AccountType ";
            SQLString += " left outer join cUser on cUser.UserID = tblAccount.AccountOwner ";
            SQLString += " where tblAccount.FinancialAccountID= " + id;

            using (SqlDataAdapter adapter = new SqlDataAdapter(SQLString, Helpers.Helpers.GetAppConnectionString()))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                BankAccount account = new BankAccount();
                if (tb != null && tb.Rows.Count > 0)
                {
                    DataRow dr = tb.Rows[0];
                    account.FinancialAccountID = int.Parse(dr["FinancialAccountID"].ToString());

                    if (dr["PaymentDueDate"] != DBNull.Value)
                    {
                        account.PaymentDueDate = DateTime.Parse(dr["PaymentDueDate"].ToString());
                    }

                    if (dr["StartDate"] != DBNull.Value)
                    {
                        account.StartDate = DateTime.Parse(dr["StartDate"].ToString());
                    }
                    account.AccountOwner = dr["OwnerName"].ToString();

                    if (dr["FrozenDateTime"] != DBNull.Value)
                    {
                        account.FrozenDateTime = DateTime.Parse(dr["FrozenDateTime"].ToString());
                    }
                    if (dr["AccountType"] != DBNull.Value)
                    {
                        account.AccountType = Int32.Parse(dr["AccountType"].ToString());
                    }
                    if (dr["AccountOwner"] != DBNull.Value)
                    {
                        account.AccountOwnerID = Int32.Parse(dr["AccountOwner"].ToString());
                    }
                    if (dr["StatusID"] != DBNull.Value)
                    {
                        account.StatusID = Int32.Parse(dr["StatusID"].ToString());
                    }
                    if (dr["CompanyID"] != DBNull.Value)
                    {
                        account.CompanyID = Int32.Parse(dr["CompanyID"].ToString());
                    }

                    account.LinkWebsite = dr["LinkWebsite"].ToString();
                    account.UserName = dr["UserName"].ToString();
                    account.AccountName = dr["AccountName"].ToString();
                    account.AccountNumber = dr["AccountNumber"].ToString();
                    account.RoutingNumber = dr["RoutingNumber"].ToString();
                    account.MailingAddress = dr["MailingAddress"].ToString();
                    account.Password = dr["Password"].ToString();
                    account.AccountTypeName = dr["name"].ToString();
                }
                return account;
            }
        }

        public static List<BankAccount > GetAllBankAccounts(string GetUserManagedCompanyString)
        {
            string SQLString = "SELECT distinct tblAccount.*, cAccountType.name, cUser.FirstName +' ' +cUser.LastName as OwnerName ";
            SQLString += " FROM tblAccount INNER JOIN tblCompanyFinancialAccount on tblCompanyFinancialAccount.FinancialAccountID = tblAccount.FinancialAccountID";
            SQLString += " left outer join cAccountType on cAccountType.AccountTypeID = tblAccount.AccountType " ;
            SQLString += " left outer join cUser on cUser.UserID = tblAccount.AccountOwner ";
            SQLString += " where tblCompanyFinancialAccount.CompanyID in " + GetUserManagedCompanyString;
            SQLString += " order by AccountName";

            List<BankAccount> accounts = new List<BankAccount>();
            using (SqlDataAdapter adapter = new SqlDataAdapter(SQLString, Helpers.Helpers.GetAppConnectionString()))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                if (tb != null && tb.Rows.Count > 0)
                {
                    foreach (DataRow dr in tb.Rows)
                    {
                        BankAccount account = new BankAccount();
                        account.FinancialAccountID = int.Parse(dr["FinancialAccountID"].ToString());

                        if (dr["PaymentDueDate"] != DBNull.Value)
                        {
                            account.PaymentDueDate = DateTime.Parse(dr["PaymentDueDate"].ToString());
                        }

                        if (dr["StartDate"] != DBNull.Value)
                        {
                            account.StartDate = DateTime.Parse(dr["StartDate"].ToString());
                        }
                        account.AccountOwner = dr["OwnerName"].ToString();

                        if (dr["FrozenDateTime"] != DBNull.Value)
                        {
                            account.FrozenDateTime = DateTime.Parse(dr["FrozenDateTime"].ToString());
                        }
                        if (dr["AccountType"] != DBNull.Value)
                        {
                            account.AccountType = Int32.Parse(dr["AccountType"].ToString());
                        }

                        account.LinkWebsite = dr["LinkWebsite"].ToString();
                        account.UserName = dr["UserName"].ToString();
                        account.AccountName = dr["AccountName"].ToString();
                        account.AccountNumber = dr["AccountNumber"].ToString();
                        account.Password = dr["Password"].ToString();
                        account.AccountTypeName = dr["name"].ToString();

                        accounts.Add(account);
                    }

                }
                return accounts;
            }
        }
        public static void Add(BankAccount model, int companyID)
        {
            SqlConnection sqlConn = new SqlConnection(Helpers .Helpers .GetAppConnectionString ());
            SqlCommand cmd = sqlConn.CreateCommand();
            DataTable dtSearchResult = new DataTable();
            SqlDataAdapter daSearchResult = new SqlDataAdapter();

            try
            {
                sqlConn.Open();
                cmd.CommandText = "insert into tblAccount(StatusID, AccountName, AccountNumber, AccountType, MailingAddress, StartDate, AccountOwner, FrozenDateTime, PaymentDueDate, LinkWebsite, UserName, Password, RoutingNumber ) values (";
                cmd.CommandText += model.StatusID +", '" + model.AccountName + "', '" + model.AccountNumber + "'," + model.AccountType + ", '" + model.MailingAddress + "','" + model.StartDate;
                cmd.CommandText += "'," + model.AccountOwnerID + ",'" + model.FrozenDateTime + "','" + model.PaymentDueDate + "','" + model.LinkWebsite + "','" + model.UserName + "','" + model.Password + "','" + model.RoutingNumber  + "'); ";
                cmd.CommandText += "SELECT SCOPE_IDENTITY(); ";
                int accountID = int.Parse(cmd.ExecuteScalar().ToString());

                cmd.CommandText = "insert into tblCompanyFinancialAccount(StatusID, CompanyID, FinancialAccountID, StartDate) values (";
                cmd.CommandText += model.StatusID + ", " + companyID + ", " + accountID + ",'" + model.StartDate + "'); ";
                cmd.ExecuteNonQuery();

            }
            catch (SqlException ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                sqlConn.Close();
            }
        }

        public static void Edit(BankAccount model, int companyID)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                // Create the Command and Parameter objects.
                SqlCommand cmd = new SqlCommand("", connection);
                connection.Open();
                cmd.CommandText = "Update tblAccount set AccountName = '" + model.AccountName + "' , AccountNumber='" + model.AccountNumber + "' , RoutingNumber='" + model.RoutingNumber + "', AccountType=";
                cmd.CommandText += model.AccountType + ", MailingAddress='" + model.MailingAddress + "', PaymentDueDate='" + model.PaymentDueDate.ToShortDateString();
                cmd.CommandText += "', StartDate='" + model.StartDate + "', AccountOwner=" + model.AccountOwnerID;
                cmd.CommandText += " , FrozenDateTime='" + model.FrozenDateTime + "'" + ", LinkWebsite='" + model.LinkWebsite + "'";
                cmd.CommandText += " , UserName='" + model.UserName + "'" + ", Password='" + model.Password + "', StatusID=" + model.StatusID;
                cmd.CommandText += " where FinancialAccountID=" + model.FinancialAccountID;
                cmd.ExecuteNonQuery();

                //update rent record
                if (companyID > 0)
                {
                    cmd.CommandText = "Update tblCompanyFinancialAccount set CompanyID= " + companyID  + " where FinancialAccountID= " + model.FinancialAccountID ;
                    cmd.ExecuteNonQuery();
                }

                connection.Close();
            }
        }
    }
}
