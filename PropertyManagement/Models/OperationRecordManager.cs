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
    public static class OperationRecordManager
    {

        public static OperationRecord GetExpenseByID(int id)
        {
            string SQLString = "select * from tblUnitOperation where ID =  " + id;
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
                    if (dr["LinkedRentID"] != DBNull.Value)
                    {
                        role.LinkedRentID = dr["LinkedRentID"].ToString();
                    }
                    role.Memo = dr["Notes"].ToString();
                    role.BankTracking = dr["BankTracking"].ToString();
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

        public static OperationRecord GetExpenseByRentID(int id)
        {
            string SQLString = "select tblUnitOperation.ID, tblUnitOperation.IsCredit, tblUnitOperation.StatusID, tblUnitOperation.CategoryID, InvoiceLink, tblUnitOperation.ContractorID, Amount, tblUnitOperation.DueDate, tblUnitOperation.FinishDate, tblUnitOperation.UnitID, Notes, tblUnitOperation.FinancialAccountID , DueAmount, tblUnitOperation.LinkedRentID, tblUnitOperation.BankTracking, TenantID from tblUnitOperation, tblRent where tblUnitOperation.LinkedRentID= tblRent.RentID and LinkedRentID =  " + id;
            using (SqlDataAdapter adapter = new SqlDataAdapter(SQLString, Helpers.Helpers.GetAppConnectionString()))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                OperationRecord role = new OperationRecord();
                if (tb == null || tb.Rows.Count == 0)
                {
                    SQLString = "1 as IsCredit, tblRent.StatusID, 36 as CategoryID, tblTenant.UserID as ContractorID, tblTenant.UnitID AS UnitID, PaidAmount as Amount, DueDate, PaymentDate as FinishDate, tblRent.Note as Notes, FinancialAccountID , RentAmount as DueAmount, tblRent.RentID AS LinkedRentID from tblRent, tblTenant where tblTenant.TenantID=tblRent.TenantID AND tblRent.RentID =" + id;
                }
                if (tb != null && tb.Rows.Count > 0)
                {
                    DataRow dr = tb.Rows[0];
                    role.ID = int.Parse(dr["ID"].ToString());

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
                    if (dr["TenantID"] != DBNull.Value)
                    {
                        role.TenantID  = (int)dr["TenantID"];
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
                    if (dr["FinishDate"] != DBNull.Value)
                    {
                        role.CompleteDate = DateTime.Parse(dr["FinishDate"].ToString());
                    }
                    if (dr["UnitID"] != DBNull.Value)
                    {
                        role.UnitID = (int)dr["UnitID"];
                    }
                    role.Memo = dr["Notes"].ToString();
                    role.BankTracking = dr["BankTracking"].ToString();
                    role.FinancialBankAccountID = dr["FinancialAccountID"].ToString();
                    if (dr["DueAmount"] != DBNull.Value)
                    {
                        role.DueAmount = double.Parse(dr["DueAmount"].ToString());
                    }
                    if (dr["InvoiceLink"] != DBNull.Value)
                    {
                        role.InvoiceLink = dr["InvoiceLink"].ToString();
                    }
                    if (dr["LinkedRentID"] != DBNull.Value)
                    {
                        role.LinkedRentID = dr["LinkedRentID"].ToString();
                    }
                    role.IsCredit = true;
                    role.IsEmailReceipt = true;

                }
                return role;
            }
        }

        public static void Add(OperationRecord model)
        {
            CreateOperationRecord(model);
        }

        public static void Edit(OperationRecord model)
        {
            UpdateOperationRecord(model);

            if (model.IsEmailReceipt)
            {
                Email.EmailPayment(model.ID, model.ContractorID, model.UnitID, model.CompleteDate, model.FinancialBankAccountID, model.DueAmount, model.Payment, model.CategoryID);
            }
        }

        public static void EditRent(OperationRecord model)
        { 
            //setup deposit type
            if (model.IsSecurityDeposit)
            {
                model.CategoryID = (int)Helpers.Helpers.EmailType.SecurityDeposit;
            }
            else
            {
                model.CategoryID = (int)Helpers.Helpers.EmailType.Rent;
            }
            if (string.IsNullOrEmpty(model.FinancialBankAccountID))
            {
                model.FinancialBankAccountID = "0";
            }
            if (model.LinkedRentID != null)
            {
                UpdateOperationRecord(model);
            }

            if (model.IsEmailReceipt)
            {
                Email.EmailPayment(model.ID, model.ContractorID, model.UnitID, model.CompleteDate, model.FinancialBankAccountID, model.DueAmount, model.Payment, model.CategoryID);
            }

            int tenantid = GetTenantID(model.ContractorID);

            UpdateRent(model, tenantid);
        }

        public static void ReceiveRent(OperationRecord model)
        {
            if(model.LinkedRentID == null)
            {
                //create linked operation
                CreateOperationRecord(model);
            }
            UpdateOperationRecord(model);
            UpdateRent(model, GetTenantID(model.ContractorID));
            if (model.IsEmailReceipt)
            {
                Email.EmailPayment(model.ID, model.ContractorID, model.UnitID, model.CompleteDate, model.FinancialBankAccountID, model.DueAmount, model.Payment, model.CategoryID);
            }
        }

        public static void TransferFund(OperationRecord model)
        {
            int LinkedReimburseExpenseID = CreateOperationRecord(model);

            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the Command and Parameter objects.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();

                    if (LinkedReimburseExpenseID > 0)
                    {
                        //update the reimbursed expense link id
                        cmd.CommandText = "UPDATE tblUnitOperation set LinkedExpenseID=" + LinkedReimburseExpenseID + " where id=" + model.ID;
                        cmd.ExecuteNonQuery();
                    }
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

        public static void AddAndTransferFund(OperationRecord model)
        {
            string TransferedFinancialBankAccountID = model.TransferedFinancialBankAccountID;
            string financialBankAccount = model.FinancialBankAccountID;

            //create record the actually payment paid by the real owner
            int expenseID = CreateOperationRecord(model);
            //create record that the payment recevied by the owner who paid the bill on behalf of the real owner
            model.IsCredit = model.IsCredit ? false:true;
            int LinkedReimburseExpenseID = CreateOperationRecord(model);

            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the Command and Parameter objects.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();

                    //update the reimbursed expense link id
                    cmd.CommandText = "UPDATE tblUnitOperation set LinkedReimburseExpenseID=" + LinkedReimburseExpenseID + " where id=" + model.ID;
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

        public static void Reimburse(OperationRecord model)
        {
            string TransferedFinancialBankAccountID = model.TransferedFinancialBankAccountID;
            string financialBankAccount = model.FinancialBankAccountID;
            double originalDueAmount = model.DueAmount;
            double originalPaymentAmoutn = model.Payment;

            //create record the actually payment paid by the real owner
            //it will be the exact process as the guy who pay/receive the bill
            model.FinancialBankAccountID = TransferedFinancialBankAccountID;
            int linkedExpenseID = CreateOperationRecord(model);
            //create record that the payment recevied by the owner who paid the bill on behalf of the real owner
            model.IsCredit = model.IsCredit ? false : true;
            model.DueAmount = -originalDueAmount;
            model.Payment = originalPaymentAmoutn; 
            model.FinancialBankAccountID = financialBankAccount;
            int LinkedReimburseExpenseID = CreateOperationRecord(model);

            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the Command and Parameter objects.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();
                    
                    //update the reimbursed expense link id
                    cmd.CommandText = "UPDATE tblUnitOperation set linkedExpenseID=" + linkedExpenseID + ", LinkedReimburseExpenseID=" + LinkedReimburseExpenseID + ", StatusID=" + model.StatusID+ " where id=" + model.ID;
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

        public static void Delete(int ID)
        {
            OperationRecord op = GetExpenseByID(ID);
            DeleteOperationRecord(op);
        }

        public static void DeleteRent(int rentID)
        {
            OperationRecord op = GetExpenseByRentID(rentID);
            DeleteOperationRecord(op);
        }

        public static int GetTenantID(int contractorID)
        {
            string SQLString = "select  tblTenant.TenantID from tblTenant WHERE UserID=  " + contractorID + " Order by TenantID desc";
            using (SqlDataAdapter adapter = new SqlDataAdapter(SQLString, Helpers.Helpers.GetAppConnectionString()))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                if (tb != null && tb.Rows.Count > 0)
                {
                    DataRow dr = tb.Rows[0];
                    if (dr["TenantID"] != DBNull.Value)
                    {
                         return  Int32.Parse(dr["TenantID"].ToString());
                    }
                }
            }
            return 0;
        }

        public static bool IsAccountFrozen(OperationRecord model)
        {
            if (!string.IsNullOrEmpty(model.FinancialBankAccountID))
            {
                string SQLString = "SELECT * FROM tblAccount where FinancialAccountID = " + model.FinancialBankAccountID;
                using (SqlDataAdapter adapter = new SqlDataAdapter(SQLString, Helpers.Helpers.GetAppConnectionString()))
                {
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);

                    DataTable tb = (DataTable)ds.Tables[0];
                    if (tb != null && tb.Rows.Count > 0)
                    {
                        DataRow dr = tb.Rows[0];
                        if (dr["FrozenDateTime"] != DBNull.Value)
                        {
                            DateTime frozenDate = DateTime .Parse(dr["FrozenDateTime"].ToString());
                            if(frozenDate > model.CompleteDate )
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static void UpdateRent(OperationRecord model, int tenantID)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                // Create the Command and Parameter objects.
                SqlCommand cmd = new SqlCommand("", connection);
                connection.Open();
                if (!model.IsCredit)
                {
                    model.Payment = -model.Payment;
                }
                int i = (model.IsCredit == true) ? 1 : 0;
                cmd.CommandText = "Update tblRent set TenantID=" + tenantID + ", DueDate='" + model.DueDate;
                cmd.CommandText += "', RentAmount=" + model.DueAmount + ", CategoryID=" + model.CategoryID + ", BankTracking='" + model.BankTracking + "'" + ", Note='" + model.Memo + "'";
                //only unfrozen bank record can be changed for the finish date, financialaccount and amount
                if (!IsAccountFrozen(model))
                {
                    cmd.CommandText += ", StatusID=" + model.StatusID + ", PaymentDate='" + model.CompleteDate + "', FinancialAccountID= " + model.FinancialBankAccountID + ", PaidAmount=" + model.Payment;
                }
                cmd.CommandText += " where RentID=" + model.LinkedRentID;
                cmd.ExecuteNonQuery();
                connection.Close();
            }
        }

        public static void UpdateOperationRecord(OperationRecord model)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                // Create the Command and Parameter objects.
                SqlCommand cmd = new SqlCommand("", connection);
                connection.Open();
                if (!model.IsCredit)
                {
                    model.Payment = -model.Payment;
                }
                int i = (model.IsCredit == true) ? 1 : 0;
                cmd.CommandText = "Update tblUnitOperation set CategoryID = " + model.CategoryID + " , IsCredit=" + Convert.ToInt32(model.IsCredit) + ", ContractorID=";
                cmd.CommandText += model.ContractorID + ", UnitID=" + model.UnitID + ", DueDate='" + model.DueDate.ToShortDateString() + "', DueAmount=" + model.DueAmount + ", Notes='" + model.Memo;
                cmd.CommandText += "' , InvoiceLink='" + model.InvoiceLink + "'" + ", BankTracking='" + model.BankTracking + "'";
                //only unfrozen bank record can be changed for the finish date, financialaccount and amount
                if (!IsAccountFrozen(model))
                {
                    cmd.CommandText += ", StatusID=" + model.StatusID + ", FinishDate='" + model.CompleteDate + "', FinancialAccountID= " + model.FinancialBankAccountID + ", Amount=" + model.Payment;
                }
                cmd.CommandText += " where ID=" + model.ID;
                cmd.ExecuteNonQuery();

                //update rent record
                if (!string.IsNullOrEmpty(model.LinkedRentID) && !model.LinkedRentID.Equals("0"))
                {
                    //update tenant id also if needed
                    cmd.CommandText = "select  tblTenant.TenantID from tblTenant WHERE UserID=  " + model.ContractorID + " Order by TenantID desc";
                    int tenantID = Int32.Parse(cmd.ExecuteScalar().ToString());

                    cmd.CommandText = "Update tblRent set StatusID= " + model.StatusID + ", PaymentDate ='" + model.CompleteDate + "', FinancialAccountID= ";
                    cmd.CommandText += model.FinancialBankAccountID + ", PaidAmount=" + model.Payment + ", TenantID=" + tenantID + ", DueDate='" + model.DueDate;
                    cmd.CommandText += "', RentAmount=" + model.DueAmount + ", CategoryID=" + model.CategoryID + ", BankTracking='" + model.BankTracking + "'" + ", Note='" + model.Memo + "'";
                    cmd.CommandText += " where RentID=" + model.LinkedRentID;
                    cmd.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        public static int CreateOperationRecord(OperationRecord model)
        {
            int expenseID = 0;
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                // Create the Command and Parameter objects.
                SqlCommand cmd = new SqlCommand("", connection);
                connection.Open();
                //only unfrozen bank record can be changed for the finish date, financialaccount and amount
                if (!IsAccountFrozen(model))
                {
                    int i = (model.IsCredit == true) ? 1 : 0;
                    if (!model.IsCredit)
                    {
                        model.Payment = -model.Payment;
                    }
                    if(string.IsNullOrEmpty (model.LinkedRentID ))
                    {
                        model.LinkedRentID = "0";
                    }
                    //create record that the payment recevied by the owner who paid the bill on behalf of the real owner
                    cmd.CommandText = "insert into tblUnitOperation(UploadedBy, UploadDate,StatusID, CategoryID, IsCredit, ContractorID, UnitID, DueDate, DueAmount, Notes, BankTracking,FinancialAccountID, Amount, FinishDate, InvoiceLink, LinkedRentID) values (";
                    cmd.CommandText += model.UploadBy + ", '" + DateTime.Now + "'," + model.StatusID + ", " + model.CategoryID + ", " + i + "," + model.ContractorID + ", " + model.UnitID + ",'" + model.DueDate + "', " + model.DueAmount + ", '" + model.Memo + "', '" + model.BankTracking;
                    cmd.CommandText += "'," + model.FinancialBankAccountID + ", " + model.Payment + ", '" + model.CompleteDate + "','" + model.InvoiceLink + "' ," + model.LinkedRentID + "); SELECT SCOPE_IDENTITY();";
                    expenseID = int.Parse(cmd.ExecuteScalar().ToString());
                }
                connection.Close();
            }
            return expenseID;
        }

        public static int CreateRent(OperationRecord model, int tenantID)
        {
            int rentID = 0;
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                // Create the Command and Parameter objects.
                SqlCommand cmd = new SqlCommand("", connection);
                connection.Open();
                //only unfrozen bank record can be changed for the finish date, financialaccount and amount
                if (!IsAccountFrozen(model))
                {
                    int i = (model.IsCredit == true) ? 1 : 0;
                    if (!model.IsCredit)
                    {
                        model.Payment = -model.Payment;
                    }
                    model.IsSecurityDeposit = false;
                    if (model.CategoryID == (int)Helpers .Helpers .ExpenseCategory .SecurityDeposit )
                    {
                        model.IsSecurityDeposit = true;
                    }
                    int r = (model.IsSecurityDeposit == false) ? 1 : 0;
                    //create record that the payment recevied by the owner who paid the bill on behalf of the real owner
                    cmd.CommandText = "INSERT INTO  tblRent (TenantID, RentAmount, PaidAmount, DueDate, IsRent, Note, StatusID, FinancialAccountID, PaymentDate ) VALUES ( " + tenantID + "," + model.DueAmount + ","+model.Payment +",'" + model.DueDate.ToShortDateString() + "'," + r+",'" + model.Memo  + "',"+model.StatusID +"," + model.FinancialBankAccountID +",'" +model.CompleteDate +"'); SELECT SCOPE_IDENTITY();";
                    rentID =  int.Parse(cmd.ExecuteScalar().ToString());
                }
                connection.Close();
            }
            return rentID;
        }

        public static void DeleteOperationRecord(OperationRecord model)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                // Create the Command and Parameter objects.
                SqlCommand cmd = new SqlCommand("", connection);
                connection.Open();
                //only unfrozen bank record can be changed for the finish date, financialaccount and amount
                if (!IsAccountFrozen(model))
                {
                    cmd.CommandText = "DELETE FROM tblRent where RENTID= (select LinkedRentID from tblUnitOperation where ID=" + model.ID + ")";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "DELETE FROM tblUnitOperation where ID=" + model.ID;
                    cmd.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        public static List<OperationRecord> GetExpense(string startDate, string endDate, string[] companyIDs, string[] propertyIDs, string[] unitIDs, string[] bankAccountIDs, string[] statusIDs, string[] contractorIDs, string[] categoryIDs, string expense, int loggedinUser)
        {
            DateTime start = DateTime.Parse(startDate);
            DateTime end = DateTime.Parse(endDate);
            StringBuilder sbOperation = new StringBuilder();
            sbOperation.Append("Select distinct ID, [tblUnitOperation].DueDate, FinishDate, PaidBy.FirstName +' ' + PaidBy.LastName as PaidBy, ");
            sbOperation.Append("tblUnitOperation.Notes,[tblUnitOperation].LinkedExpenseID, Amount, tblAccount.AccountName, tblProperty.Address, tblPropertyUnit.UnitName, IsCredit, ");
            sbOperation.Append(" DueAmount, [tblUnitOperation].StatusID, cStatusType.Name  as StatusName, cExpenseCategory.CategoryName,tblUnitOperation.BankTracking  from tblUnitOperation ");
            sbOperation.Append(" inner join  tblPropertyUnit on tblPropertyUnit.UnitID =  tblUnitOperation.UnitID ");
            sbOperation.Append(" INNER JOIN  tblProperty ON tblProperty.PropertyID = tblPropertyUnit.PropertyID ");
            sbOperation.Append(" INNER JOIN mCompanyProperty on mCompanyProperty.PropertyID = tblProperty.PropertyID ");
            sbOperation.Append(" LEFT OUTER JOIN cUser as PaidBy on PaidBy.UserID = tblUnitOperation.ContractorID ");
            sbOperation.Append(" LEFT OUTER JOIN cStatusType on cStatusType.StatusTypeID = tblUnitOperation.StatusID ");
            sbOperation.Append(" LEFT OUTER JOIN cExpenseCategory on cExpenseCategory.CategoryID = tblUnitOperation.CategoryID ");
            sbOperation.Append(" LEFT OUTER JOIN tblAccount as tblAccount on tblAccount.FinancialAccountID = tblUnitOperation.FinancialAccountID ");

            StringBuilder whereClause = new StringBuilder();

            if (!String.IsNullOrEmpty(startDate))
            {
                start = DateTime.Parse(startDate);
                whereClause.Append(" and [tblUnitOperation].FinishDate>='" + start.ToShortDateString() + "' ");
            }
            if (!String.IsNullOrEmpty(endDate))
            {
                end = DateTime.Parse(endDate);
                whereClause.Append(" and [tblUnitOperation].FinishDate<='" + end.ToShortDateString() + "'");
            }
            // Add modality id to the where clause if appropriate
            if (bankAccountIDs != null && bankAccountIDs.Count() > 0 && !string.IsNullOrEmpty(bankAccountIDs[0]))
            {
                whereClause.Append(" AND tblUnitOperation.FinancialAccountID IN (" + String.Join(",", bankAccountIDs) + ")");
            }
            // Add modality id to the where clause if appropriate
            if (companyIDs != null && companyIDs.Count() > 0 && !string.IsNullOrEmpty(companyIDs[0]))
            {
                whereClause.Append(" AND mCompanyProperty.CompanyID IN (" + String.Join(",", companyIDs) + ")");
            }
            else
            {
                //get the companys only the owner can access
                whereClause.Append(" AND mCompanyProperty.CompanyID IN (" + Helpers.Helpers .GetUserManagedCompanyString(loggedinUser.ToString ()) + ")");
            }
            // Add modality id to the where clause if appropriate
            if (propertyIDs != null && propertyIDs.Count() > 0 && !string.IsNullOrEmpty(propertyIDs[0]))
            {
                whereClause.Append(" AND tblProperty.PropertyID IN (" + String.Join(",", propertyIDs) + ")");
            }
            // Add modality id to the where clause if appropriate
            if (unitIDs != null && unitIDs.Count() > 0 && !string.IsNullOrEmpty(unitIDs[0]))
            {
                whereClause.Append(" AND tblPropertyUnit.UnitID IN (" + String.Join(",", unitIDs) + ")");
            }
            if (statusIDs != null && statusIDs.Count() > 0 && !string.IsNullOrEmpty(statusIDs[0]))
            {
                whereClause.Append(" AND [tblUnitOperation].StatusID IN (" + String.Join(",", statusIDs) + ")");
            }
            if (contractorIDs != null && contractorIDs.Count() > 0 && !string.IsNullOrEmpty(contractorIDs[0]))
            {
                whereClause.Append(" AND [tblUnitOperation].ContractorID IN (" + String.Join(",", contractorIDs) + ")");
            }
            if (categoryIDs != null && categoryIDs.Count() > 0 && !string.IsNullOrEmpty(categoryIDs[0]))
            {
                whereClause.Append(" AND [tblUnitOperation].CategoryID IN (" + String.Join(",", categoryIDs) + ")");
            }
            if (!string.IsNullOrEmpty(expense))
            {
                whereClause.Append(" AND ([tblUnitOperation].Amount IN (" + expense + ", -" + expense + ") OR [tblUnitOperation].DueAmount IN (" + expense + ", -" + expense + "))");
            }

            sbOperation.Append(whereClause.Remove(0, 4).Insert(0, " where "));

            sbOperation.Append(" Order by DueDate");

            // Create a list of our result class to hold the data from the query
            // Please ensure you instatiate the class for this controller and not a different controller
            List<OperationRecord> result = new List<OperationRecord>();
            // Execute the SQL query and get the results

            using (SqlDataAdapter adapter = new SqlDataAdapter(sbOperation.ToString(), Helpers.Helpers.GetAppConnectionString()))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                if (tb != null && tb.Rows.Count > 0)
                {
                    for (int i = 0; i < tb.Rows.Count; i++)
                    {
                        DataRow dr = tb.Rows[i];
                        OperationRecord row = new OperationRecord();
                        if (dr["DueDate"] != DBNull.Value)
                        {
                            row.DueDate = DateTime.Parse(dr["DueDate"].ToString());
                        }
                        if (dr["FinishDate"] != DBNull.Value) { row.CompleteDate = DateTime.Parse(dr["FinishDate"].ToString()); }
                        row.PaidBy = dr["PaidBy"].ToString();
                        if (dr["BankTracking"] != DBNull.Value)
                        {
                            row.Memo = dr["Notes"].ToString() + " " + dr["BankTracking"].ToString();
                        }
                        else
                        {
                            row.Memo = dr["Notes"].ToString();
                        }
                        if (dr["Amount"] != DBNull.Value)
                        {
                            row.Payment = double.Parse(dr["Amount"].ToString());
                        }
                        row.BankAccountName = dr["AccountName"].ToString();
                        row.Address = dr["Address"].ToString() + " -- " + dr["UnitName"].ToString();
                        if (dr["DueAmount"] != DBNull.Value)
                        {
                            row.DueAmount = double.Parse(dr["DueAmount"].ToString());
                        }
                        row.ID = int.Parse(dr["ID"].ToString());
                        if (dr["StatusID"] != DBNull.Value)
                        {
                            row.StatusID = short.Parse(dr["StatusID"].ToString());
                            row.StatusName = dr["StatusName"].ToString();
                        }
                        if (dr["LinkedExpenseID"] != DBNull.Value)
                        {
                            row.LinkedExpenseID = Int32.Parse(dr["LinkedExpenseID"].ToString());
                        }
                        row.CategoryName = dr["CategoryName"].ToString();
                        result.Add(row);
                    }
                }
            }

            return result;
        }
    }
}