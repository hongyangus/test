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
    public class RentManager
    {
        static string appString = ConfigurationManager.ConnectionStrings["SunriseConnectionString"].ConnectionString;

        public static OperationRecord GetExpenseByID(int id)
        {
            string SQLString = "select ntID , DueAmount from tblUnitOperation where ID =  " + id;
            using (SqlDataAdapter adapter = new SqlDataAdapter(SQLString, Helpers.Helpers.GetAppConnectionString()))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                OperationRecord role = new OperationRecord();
                if (tb != null && tb.Rows.Count > 0)
                {
                    DataRow dr = tb.Rows[0];
                    if (dr["ID"] != DBNull.Value)
                    {
                        role.ID = Int32.Parse(dr["ID"].ToString());
                    }
                    if (dr["IsCredit"] != DBNull.Value)
                    {
                        role.IsCredit = Boolean.Parse(dr["IsCredit"].ToString());
                    }

                    if (dr["StatusID"] != DBNull.Value)
                    {
                        role.StatusID = short.Parse(dr["StatusID"].ToString());
                    }

                    if (dr["CategoryID"] != DBNull.Value)
                    {
                        role.CategoryID = short.Parse(dr["CategoryID"].ToString());
                    }

                    if (dr["ContractorID"] != DBNull.Value)
                    {
                        role.ContractorID = (int)dr["ContractorID"];
                    }
                    if (dr["Amount"] != DBNull.Value)
                    {
                        if (role.IsCredit)
                        {
                            role.Payment = double.Parse(dr["Amount"].ToString());
                        }
                        else
                        {
                            role.Payment = -double.Parse(dr["Amount"].ToString());
                        }
                    }
                    if (dr["DueDate"] != DBNull.Value)
                    {
                        role.DueDate = DateTime.Parse(dr["DueDate"].ToString());
                    }
                    if (dr["FinishDate"] != DBNull.Value) { role.CompleteDate = DateTime.Parse(dr["FinishDate"].ToString()); }
                    if (dr["UnitID"] != DBNull.Value)
                    {
                        role.UnitID = (int)dr["UnitID"];
                    }
                    role.Memo = dr["Notes"].ToString();
                    role.FinancialBankAccountID = dr["FinancialAccountID"].ToString();
                    if (dr["DueAmount"] != DBNull.Value)
                    {
                        role.DueAmount = double.Parse(dr["DueAmount"].ToString());
                    }
                    if (dr["InvoiceLink"] != DBNull.Value)
                    {
                        role.InvoiceLink = dr["InvoiceLink"].ToString();
                    }
                }
                return role;
            }
        }

        public static void Add(OperationRecord model)
        {
            OperationRecordManager.CreateOperationRecord(model);
            //using (SqlConnection connection = new SqlConnection(appString))
            //{
            //    try
            //    {
            //        // Create the Command and Parameter objects.
            //        SqlCommand cmd = new SqlCommand("", connection);
            //        connection.Open();
            //        if (!model.IsCredit)
            //        {
            //            model.Payment = -model.Payment;
            //        }
            //        int i = (model.IsCredit == true) ? 1 : 0;
            //        cmd.CommandText = "insert into tblUnitOperation(StatusID, CategoryID, IsCredit, ContractorID, UnitID, DueDate, DueAmount, Notes,FinancialAccountID, Amount, FinishDate, InvoiceLink) values (";
            //        cmd.CommandText += model.StatusID + ", " + model.CategoryID + ", " + i + "," + model.ContractorID + ", " + model.UnitID + ",'" + model.DueDate + "', " + model.DueAmount + ", '" + model.Memo;
            //        cmd.CommandText += "'," + model.FinancialBankAccountID + ", " + model.Payment + ", '" + model.CompleteDate + "'," + model.InvoiceLink + "' )";
            //        cmd.ExecuteNonQuery();
            //    }
            //    catch (Exception ex)
            //    {
            //        throw new Exception(ex.Message);
            //    }
            //    finally
            //    {
            //        connection.Close();
            //    }

            //}
        }

        public static void Edit(OperationRecord model)
        {
            using (SqlConnection connection = new SqlConnection(appString))
            {
                try
                {
                    // Create the Command and Parameter objects.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();
                    if (!model.IsCredit)
                    {
                        model.Payment = -model.Payment;
                    }
                    int i = (model.IsCredit == true) ? 1 : 0;
                    cmd.CommandText = "Update tblUnitOperation set StatusID=" + model.StatusID + ", CategoryID = " + model.CategoryID + " , IsCredit=" + Convert.ToInt32(model.IsCredit) + ", ContractorID=";
                    cmd.CommandText += model.ContractorID + ", UnitID=" + model.UnitID + ", DueDate='" + model.DueDate.ToShortDateString() + "', DueAmount=" + model.DueAmount + ", Notes='" + model.Memo;
                    cmd.CommandText += "' " + ", FinishDate='" + model.CompleteDate + "', FinancialAccountID= " + model.FinancialBankAccountID + ", Amount=" + model.Payment + ", InvoiceLink='" + model.InvoiceLink + "'";
                    cmd.CommandText += " where ID=" + model.ID;
                    cmd.ExecuteNonQuery();


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
            using (SqlConnection connection = new SqlConnection(appString))
            {
                try
                {
                    // Create the Command and Parameter objects.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();
                    cmd.CommandText = "DELETE FROM tblUnitOperation where ID=" + ID;
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