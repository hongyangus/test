using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Text;
using System.Data.SqlClient;
using PropertyManagement.ViewModels.Task;
using System.Text.RegularExpressions;

namespace PropertyManagement.Models
{
    public class TaskManager
    {
        public static List<AddTaskVM> GetAllActiveTaskForUser(string[] statusIDs, string[] contractorIDs)
        {
            StringBuilder sbOperation = new StringBuilder();
            sbOperation.Append("Select distinct TaskID,  TaskDetail , cUser.UserID, (cUser.FirstName + ' ' + cUser.LastName) as FullName, tblTasks.StatusID, UpdateDate, WorkHours , "
                + " Milage, MaterialCost, TotalCost, PaymentBankAccountID , tblTasks.UnitID, cStatusType.name, Labor, tblProperty.Address + '-' + tblPropertyUnit.UnitName");
            sbOperation.Append(" from tblTasks ");
            sbOperation.Append(" INNER JOIN cUser on cUser.UserID = tblTasks.contractorid ");
            sbOperation.Append(" INNER JOIN cStatusType on cStatusType.StatusTypeID = tblTasks.StatusID ");
            sbOperation.Append(" left outer join  tblPropertyUnit on tblPropertyUnit.UnitID =  tblTasks.UnitID ");
            sbOperation.Append(" left outer join  tblProperty ON tblProperty.PropertyID = tblPropertyUnit.PropertyID ");
            if (statusIDs != null && statusIDs.Count() > 0 && !string.IsNullOrEmpty(statusIDs[0]))
            {
                sbOperation.Append(" where tblTasks.StatusID IN (" + String.Join(",", statusIDs) + ")");
            }
            if (contractorIDs != null && contractorIDs.Count() > 0 && !string.IsNullOrEmpty(contractorIDs[0]))
            {
                sbOperation.Append(" AND tblTasks.ContractorID IN (" + String.Join(",", contractorIDs) + ")");
            }
            sbOperation.Append(" order by tblTaskS.StatusID asc, UpdateDate desc");
            using (SqlDataAdapter adapter = new SqlDataAdapter(sbOperation.ToString(), Helpers.Helpers.GetAppConnectionString()))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                List<AddTaskVM> allActiveTask = new List<AddTaskVM>();

                DataTable tb = (DataTable)ds.Tables[0];
                if (tb != null && tb.Rows.Count > 0)
                {
                    for (int i = 0; i < tb.Rows.Count; i++)
                    {
                        DataRow dr = tb.Rows[i];
                        AddTaskVM task = new AddTaskVM();
                        task.TaskID = Int32.Parse(dr[0].ToString());
                        task.TaskDetail = dr[1].ToString();                        
                        task.TaskDetailHtml = GenerateMailtoLink(dr[1].ToString ());
                        task.UserName = dr[3].ToString();
                        if (dr[2] != DBNull.Value)
                        {
                            task.ContractorID = Int32.Parse(dr[2].ToString());
                        }
                        if (dr[4] != DBNull.Value)
                        {
                            task.StatusID = Int32.Parse(dr[4].ToString());
                        }
                        if (dr[5] != DBNull.Value)
                        {
                            task.UpdateDate = DateTime.Parse(dr[5].ToString());
                        }
                        if (dr[6] != DBNull.Value) { task.Hours = double.Parse(dr[6].ToString()); }
                        if (dr[7] != DBNull.Value) { task.Milage = double.Parse(dr[7].ToString()); }
                        if (dr[8] != DBNull.Value) { task.Material = double.Parse(dr[8].ToString()); }
                        if (dr[9] != DBNull.Value) { task.TotalPayment = double.Parse(dr[9].ToString()); } else { task.TotalPayment = task.Hours * 15 + task.Milage * 0.535 + task.Material; }
                        if (dr[10] != DBNull.Value) { task.BankAccountID = int.Parse(dr[10].ToString()); }
                        if (dr[11] != DBNull.Value) { task.UnitID = int.Parse(dr[11].ToString()); } 
                        task.StatusName = dr[12].ToString();
                        if (dr[13] != DBNull.Value) { task.Labor  = int.Parse(dr[13].ToString()) ; } else { task.Labor = task.Hours * 15; }
                        task.Address = dr[14].ToString();


                        allActiveTask.Add (task);
                    }
                    return allActiveTask;
                }
                return null;
            }
        }

        public static void AddTask(AddTaskVM model)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the Command and Parameter objects.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();
                    //calculate the total payment
                    if(model.TotalPayment == 0)
                    {
                        model.TotalPayment = model.Hours * 15 + model.Milage * 0.53 + model.Material;
                    }
                    cmd.CommandText = "insert into tblTasks(TaskDetail, StatusID, ContractorID, CreateDate, UpdateDate, WorkHours, Milage, MaterialCost, TotalCost, PaymentBankAccountID, UNITID ) values ('";
                    cmd.CommandText += model.TaskDetail.Replace ("'", "") + "', 1, " + model.ContractorID + ", '" + DateTime .Now + "', '" + DateTime.Now  + "',"+ model.Hours 
                        + "," + model.Milage + "," + model.Material + "," + model.TotalPayment + "," + model.BankAccountID + "," + model.UnitID + ")";
                    cmd.ExecuteNonQuery();

                    //send text message to the contractor and admin
                    Email.EmailTask(model.ContractorID, model.AdminID, model.TaskDetail, "High");
                   
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

        public static AddTaskVM GetTaskByID(int taskID)
        {
            StringBuilder sbOperation = new StringBuilder();
            sbOperation.Append("Select distinct TaskID,  TaskDetail , cUser.UserID, (cUser.FirstName + ' ' + cUser.LastName) as FullName, tblTasks.StatusID, UpdateDate, WorkHours, Milage, MaterialCost, "+
                " TotalCost, PaymentBankAccountID, LinkedExpenseID, UnitID, Labor ");
            sbOperation.Append(" from tblTasks ");
            sbOperation.Append(" INNER JOIN tblCompanyUser on tblCompanyUser.UserID = tblTasks.contractorid ");
            sbOperation.Append(" INNER JOIN cUser on cUser.UserID = tblCompanyUser.UserID ");
            sbOperation.Append(" where TaskID=" + taskID );
            using (SqlDataAdapter adapter = new SqlDataAdapter(sbOperation.ToString(), Helpers.Helpers.GetAppConnectionString()))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                if (tb != null && tb.Rows.Count > 0)
                {
                    DataRow dr = tb.Rows[0];
                    AddTaskVM task = new AddTaskVM();
                    task.TaskID = Int32.Parse(dr[0].ToString());
                    task.TaskDetail = dr[1].ToString();
                    task.ContractorID = Int32.Parse(dr[2].ToString());
                    if (dr[2] != DBNull.Value)
                    {
                        task.ContractorID = Int32.Parse(dr[2].ToString());
                    }
                    if (dr[4] != DBNull.Value)
                    {
                        task.StatusID = Int32.Parse(dr[4].ToString());
                    }
                    if (dr[5] != DBNull.Value)
                    {
                        task.UpdateDate = DateTime.Parse(dr[5].ToString());
                    }
                    if (dr[6] != DBNull.Value) { task.Hours = double.Parse(dr[6].ToString()); }
                    if (dr[7] != DBNull.Value) { task.Milage = double.Parse(dr[7].ToString()); }
                    if (dr[8] != DBNull.Value) { task.Material = double.Parse(dr[8].ToString()); }
                    if (dr[9] != DBNull.Value) { task.TotalPayment = double.Parse(dr[9].ToString()); }
                    if (dr[10] != DBNull.Value) { task.BankAccountID = int.Parse(dr[10].ToString()); } 
                    if (dr[11] != DBNull.Value) { task.LinkedExpenseID = int.Parse(dr[11].ToString()); } 
                    if (dr[12] != DBNull.Value) { task.UnitID = int.Parse(dr[12].ToString()); } 
                    if (dr[13] != DBNull.Value) { task.Labor = double.Parse(dr[13].ToString()); }
                    return task;
                }
                return null;
            }
        }

        public static void CloseTask(int ID)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the Command and Parameter objects.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();
                    cmd.CommandText = "update tblTasks set StatusID = 3, ClosedDate='" + DateTime.Now + "' where TaskID=" + ID;
                    cmd.ExecuteNonQuery();

                    connection.Close();
                }
                catch (Exception ex)
                {
                    string exms = ex.Message;
                }
            }
        }

        public static void EditTask(AddTaskVM model)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    int linkedExpenseID = GetTaskByID(model.TaskID).LinkedExpenseID;
                    //for closed task, system will auto generated expense
                    if (model.StatusID == (int)Helpers .Helpers .StatusType .Close)
                    {
                        if(model.LinkedExpenseID == 0)
                        {
                            //add expense
                            OperationRecord op = new OperationRecord();
                            op.ContractorID = model.ContractorID;
                            op.CategoryID = (int)Helpers.Helpers.ExpenseCategory.Repair;
                            op.DueAmount = model.TotalPayment;
                            op.DueDate = DateTime.Now;
                            op.CompleteDate = DateTime.Now;
                            op.UnitID = model.UnitID;
                            //op.UploadBy = Int32.Parse(Session["UserID"].ToString());
                            op.FinancialBankAccountID = model.BankAccountID.ToString ();
                            op.Payment = model.TotalPayment;
                            op.Memo = "Labor: " + model.Hours + " Milage: " + model.Milage + " Material: " + model.Material;
                            op.StatusID = (short)Helpers.Helpers.StatusType.Close;
                            model.LinkedExpenseID = OperationRecordManager.CreateOperationRecord(op);
                        }
                        else
                        {
                            //update expense
                            OperationRecord op = OperationRecordManager.GetExpenseByID(model.LinkedExpenseID );
                            op.ContractorID = model.ContractorID;
                            op.CategoryID = (int)Helpers.Helpers.ExpenseCategory.Repair;
                            op.DueAmount = model.TotalPayment;
                            op.DueDate = DateTime.Now;
                            op.CompleteDate = DateTime.Now;
                            op.UnitID = model.UnitID;
                            //op.UploadBy = Int32.Parse(Session["UserID"].ToString());
                            op.FinancialBankAccountID = model.BankAccountID.ToString();
                            op.Payment = model.TotalPayment;
                            op.Memo = "Labor: " + model.Hours + " Milage: " + model.Milage + " Material: " + model.Material;
                            OperationRecordManager.UpdateOperationRecord(op);
                        }
                    }
                    // Create the Command and Parameter objects.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();
                    model.TotalPayment = model.Hours * 15 + model.Milage * 0.53 + model.Material;
                    cmd.CommandText = "Update tblTasks set TaskDetail = '" + model.TaskDetail.Replace("'", "") + "', UpdateDate='" + DateTime.Now 
                        + "', ContractorID="+model.ContractorID 
                        +", StatusID="+ model.StatusID 
                        + ", WorkHours =" + model.Hours
                        + ", Milage =" + model.Milage
                        + ", Labor =" + model.Labor
                        + ", MaterialCost =" + model.Material 
                        + ", TotalCost =" + model.TotalPayment
                        + ", PaymentBankAccountID =" + model.BankAccountID 
                        + ", LinkedExpenseID =" + model.LinkedExpenseID
                        + ", UnitID =" + model.UnitID
                        + "  where TaskID=" + model.TaskID;
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
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the Command and Parameter objects.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();
                    cmd.CommandText = "DELETE FROM tblTasks where TaskID=" + ID;
                    cmd.ExecuteNonQuery();

                    connection.Close();
                }
                catch (Exception ex)
                {
                    string exms = ex.Message;
                }
            }
        }

        private static HtmlString GenerateMailtoLink(string text)
        {
            Regex emailRegex = new Regex(@"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*",
               RegexOptions.IgnoreCase);
            //find items that matches with our pattern
            MatchCollection emailMatches = emailRegex.Matches(text);

            StringBuilder sb = new StringBuilder();
            HtmlString textHtml = null;
            if(emailMatches != null && emailMatches.Count > 0)
            {
                Match emailMatch = emailMatches[0];
                string mailto = "<a href = \"mailto:" + emailMatch.Value + "\" > " + emailMatch.Value + "</a>";
                textHtml = new HtmlString(text.Replace(emailMatch.Value, mailto));
            }
            else
            {
                textHtml = new HtmlString(text);
            }
            return textHtml;
        }
    }
}