using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Configuration;
using System.Data.SqlClient;
using PropertyManagement.Models;
using System.Text;
using System.Data;
using System.IO;
using LumenWorks.Framework.IO.Csv;
using PropertyManagement.ViewModels.BankAccount;
using PropertyManagement.ViewModels.Property;

namespace PropertyManagement.Controllers
{
    [Authorize]
    public class BankAccountController : BaseController
    {
        private string reporttitle = "Bank Statement"; // Specify the report title here
        private string errorMessage = ""; // Specify the error message

        [AllowAnonymous]
        public ActionResult Index()
        {
            // Enable security to redirect to login page if user is not logged in or we are not running in the VS IDE
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }

            // Save the report title to the ViewBag
            ViewBag.ReportTitle = reporttitle;

            var bankAccounts = GetList((short)Helpers .Helpers .ListType .bankaccount);
            ViewBag.bankAccounts = new MultiSelectList(bankAccounts, "id", "description");

            //setup default value of the start date and end date
            if (Session["startDate"] == null)
            {
                DateTime oneMonth = DateTime.Now.AddMonths(-1);
                Session["startDate"] = new DateTime(oneMonth.Year, oneMonth.Month, 1).ToString("MM/dd/yyyy");
                Session["endDate"] = DateTime.Now.ToString("MM/dd/yyyy");
            }

            // Save error message to the ViewBag
            ViewBag.ErrorMessage = errorMessage;


            return View();
        }

        [AllowAnonymous]
        public PartialViewResult ReportViewDetail(string startDate, string endDate, string[] bankAccounts)
        {
            // Manage the datetimes
            DateTime start = DateTime.Today.AddDays(-1); // Default start datetime
            DateTime end = DateTime.Today; // Default end datetime
            if (!String.IsNullOrEmpty(startDate))
            {
                start = Convert.ToDateTime(startDate); // Start of day is 7am
            }
            if (!String.IsNullOrEmpty(endDate))
            {
                end = Convert.ToDateTime(endDate); // Start of day is 7am
            }
            Session["startDate"] = startDate;
            Session["endDate"] = endDate;

            StringBuilder sbOperation = new StringBuilder();
            sbOperation.Append(" Select DISTINCT.[tblUnitOperation].ID, [tblUnitOperation].DueDate, FinishDate, CategoryID, PaidTo.FirstName +' ' + PaidTo.LastName as PaidTo, ");
            sbOperation.Append (" tblUnitOperation.Notes,Amount, tblAccount.AccountName, tblProperty.Address, tblPropertyUnit.UnitName, IsCredit, tblUnitOperation.BankTracking, tblUnitOperation.InvoiceLink from tblUnitOperation ");
            sbOperation.Append(" inner join  tblPropertyUnit on tblPropertyUnit.UnitID =  tblUnitOperation.UnitID ");
            sbOperation.Append(" INNER JOIN  tblProperty ON tblProperty.PropertyID = tblPropertyUnit.PropertyID ");
            sbOperation.Append(" INNER JOIN mCompanyProperty on mCompanyProperty.PropertyID = tblProperty.PropertyID ");
            sbOperation.Append(" INNER JOIN cUser as PaidTo on PaidTo.UserID = tblUnitOperation.ContractorID ");
            sbOperation.Append(" INNER JOIN tblAccount as tblAccount on tblAccount.FinancialAccountID = tblUnitOperation.FinancialAccountID ");
            sbOperation.Append(" where tblUnitOperation.StatusID =  " + (short)Helpers .Helpers .StatusType .Close);
            if (start != DateTime.MinValue)
            {
                sbOperation.Append(" and [tblUnitOperation].FinishDate>='" + start + "' ");
            }
            if (end.Date != DateTime.MinValue)
            {
                sbOperation.Append(" and [tblUnitOperation].FinishDate<='" + end + "'");
            }
            // Add modality id to the where clause if appropriate
            if (bankAccounts != null && bankAccounts.Count() > 0 && !string.IsNullOrEmpty(bankAccounts[0]))
            {
                sbOperation.Append(" AND tblUnitOperation.FinancialAccountID IN (" + String.Join(",", bankAccounts) + ")");
            }
            else
            {
                //get the companys only the owner can access
                sbOperation.Append(" AND mCompanyProperty.CompanyID IN (" + GetUserManagedCompanyString() + ")");
            }

            // Add the order by clause
            sbOperation.Append(" order by [tblUnitOperation].FinishDate asc");

            // Create a list of our result class to hold the data from the query
            // Please ensure you instatiate the class for this controller and not a different controller
            List<OperationRecord> result = new List<OperationRecord>();
            double accountHistoryBalance = GetBankAccountBlance(start, bankAccounts);
            double totalPayment = 0;
            double totalDeposit = 0;

            // Execute the SQL query and get the results
            string connectionString = ConfigurationManager.ConnectionStrings["SunriseConnectionString"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(sbOperation.ToString(), connection); // Create the Command and Parameter objects
                // Open the connection in a try/catch block. 
                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read()) // Read each result row and extract the data
                    {
                        OperationRecord row = new OperationRecord();
                        row.DueDate = DateTime.Parse(reader[1].ToString());
                        row.CompleteDate = DateTime.Parse(reader[2].ToString());
                        short categoryID = short.Parse(reader[3].ToString());

                        row.Memo = reader[5].ToString() + reader[11].ToString ();
                        double payment = double.Parse(reader[6].ToString());
                        row.BankAccountName = reader[7].ToString();
                        row.Address = reader[8].ToString() +"--"+ reader[9].ToString();
                        bool isCredit = false;
                        if (reader[10] != DBNull.Value)
                        {
                            isCredit = bool.Parse(reader[10].ToString());
                        }
                        if (isCredit)
                        {
                            if (reader[4] != DBNull.Value)
                            {
                                row.PaidBy = reader[4].ToString();
                            }
                            row.Deposit = payment;
                            row.Balance = accountHistoryBalance + payment;
                            totalDeposit += payment;
                        }
                        else
                        {
                            if (reader[4] != DBNull.Value)
                            {
                                row.PaidToPerson = reader[4].ToString ();
                            }
                            row.Payment = -payment;
                            row.Balance = accountHistoryBalance + payment;
                            totalPayment += row.Payment;
                        }
                        row.ID = int.Parse (reader[0].ToString());
                        row.InvoiceLink = reader[11].ToString();
                        accountHistoryBalance = row.Balance;
                        result.Add(row);
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    //display the exception message in viewbag
                    ViewBag.MyExeption = ex.Message;
                    ViewBag.MyExeptionCSS = "errorMessage";
                }
            }

            //
            //display preselect bankAccount
            Session["selectedBankAccount"] = bankAccounts;
            ViewBag.TableCaption = reporttitle + " Bank statement: " + start.ToString("g") + " thru " + end.ToString("g");
            ViewBag.TotalPayment = totalPayment;
            ViewBag.TotalDeposit = totalDeposit;
            ViewBag.TotalBalace = accountHistoryBalance;
            return PartialView("ReportViewDetail", result);
        }

        
        [AllowAnonymous]
        public ActionResult Add()
        {
            // add new user
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Create new expense";
            // Get the users from the DB for the drop-down listbox
            var allUsers = GetList((short)Helpers.Helpers.ListType.allUser);
            //ViewBag.AllTenants = new MultiSelectList(allUsers, "id", "description");

            var allBankAccount = GetList((short)Helpers.Helpers.ListType.bankaccount);
            // ViewBag.bankAccounts = new MultiSelectList(allBankAccount, "id", "description");


            var allUnits = GetList((short)Helpers.Helpers.ListType.unit);
            // ViewBag.allUnits = new MultiSelectList(allUnits, "id", "description");

            OperationRecord model = new OperationRecord();

            model.AllTenant = GetSelectListItems((short)Helpers.Helpers.ListType.allUser);
            model.AllUnits = GetSelectListItems((short)Helpers.Helpers.ListType.unit);
            model.AllBankAccount = GetSelectListItems((short)Helpers.Helpers.ListType.bankaccount);
            model.AllCategory = GetSelectListItems((short)Helpers.Helpers.ListType.allExpenseCategory);
            model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);

            return View(model);
        }
        

        [AllowAnonymous]
        public ActionResult LoadBatch()
        {
            // add new user
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Create new bank record";
            LoadBatchVM loadBatch = new LoadBatchVM();
            loadBatch .AllBankAccount = GetSelectListItems((short)Helpers.Helpers.ListType.bankaccount);
            loadBatch.AllAccountType = GetSelectListItems((short)Helpers.Helpers.ListType.allAccountType);

            return View(loadBatch);
        }


        [HttpPost]
        [AllowAnonymous]
        public ActionResult Upload(string FinancialBankAccountID, string FinancialAccountTypeID, HttpPostedFileBase upload)
        {
            if (ModelState.IsValid)
            {
                LoadBatchVM loadBatch = new LoadBatchVM();
                if (upload != null && upload.ContentLength > 0)
                {
                    if (upload.FileName.ToLower().EndsWith(".csv"))
                    {
                        Stream stream = upload.InputStream;
                        DataTable csvTable = new DataTable();
                        using (CsvReader csvReader =
                            new CsvReader(new StreamReader(stream), true))
                        {
                            csvTable.Load(csvReader);
                        }

                        //load the data to unit operation

                        SqlConnection sqlConn = new SqlConnection(ConfigurationManager.ConnectionStrings["SunriseConnectionString"].ConnectionString);
                        SqlCommand cmd = sqlConn.CreateCommand();
                        DataTable dtSearchResult = new DataTable();
                        SqlDataAdapter daSearchResult = new SqlDataAdapter();
                        List<OperationRecord> operations = new List<OperationRecord>();
                        try
                        {
                            sqlConn.Open();
                            //Get accountid by account number
                            int accountid = Int32.Parse(FinancialBankAccountID);
                            int accoutypeid = Int32.Parse(FinancialAccountTypeID);
                            foreach (DataRow dr in csvTable.Rows)
                            {
                                OperationRecord model = new OperationRecord();
                                model.IsCredit = false;

                                //load from chase credit card
                                if (accoutypeid == 3)
                                {
                                    model.DueDate = DateTime.Parse(dr[1].ToString());
                                    model.CompleteDate = DateTime.Parse(dr[2].ToString());
                                    model.Memo = dr[3].ToString().Replace("'", "");
                                    model.Payment = double.Parse(dr[4].ToString());
                                    if(model.Payment <0)
                                    {
                                        model.DueAmount = -model.Payment;
                                        //set all expense as repair by default.
                                        model.CategoryID = 17;
                                    }
                                    else
                                    {
                                        model.IsCredit = true;
                                        model.DueAmount = model.Payment;
                                        //set all income as owner contribution
                                        model.CategoryID = 23;
                                    }
                                    model.BankTracking = "Chase credit card";
                                }

                                //load data from quickbook
                                else if (accoutypeid == 4)
                                {
                                    model.UnitID = 18;
                                    model.ContractorID = 0;
                                    if (!string.IsNullOrEmpty(dr[6].ToString()))
                                    {
                                        model.IsCredit = true;
                                        model.Payment = double.Parse(dr[6].ToString());
                                        model.DueAmount = model.Payment;
                                        model.CategoryID = 36;
                                    }
                                     else
                                    {
                                        model.Payment = -double.Parse(dr[7].ToString());
                                        model.DueAmount = -model.Payment;
                                        model.CategoryID = 17;
                                    }
                                    if (dr[5] != DBNull.Value)
                                    {
                                        cmd.CommandText = "select CategoryID from cExpenseCategory where CategoryName like + '%" + (dr[5].ToString().Split (' '))[0] + "%'";
                                        object result = cmd.ExecuteScalar();
                                        if (result != null)
                                        {
                                            model.CategoryID = (short)result;
                                        }
                                    }
                                    if (dr[3] != null)
                                    {
                                        string[] names = dr[3].ToString().Split(' ').ToArray();
                                        string firstname = names[0];
                                        string lastname = "";
                                        if (names.Length > 1)
                                        {
                                            lastname = names[1];
                                        }
                                        cmd.CommandText = "select UserID from cUser where lastname like '%" + lastname + "%'";
                                        object result = cmd.ExecuteScalar();
                                        if (result != null)
                                        {
                                            model.ContractorID = ((Int32)cmd.ExecuteScalar());
                                            cmd.CommandText = "select UnitID from tblTenant where UserID = " + model.ContractorID + " order by StartDate Desc";
                                            result = cmd.ExecuteScalar();
                                            if (result != null)
                                            {
                                                model.UnitID = ((Int32)cmd.ExecuteScalar());
                                            }
                                        }
                                        else
                                        {
                                            //create user direct because this user does not exist in the system
                                            cmd.CommandText = "INSERT INTO cuser (username, firstname, lastname, password, statusid) values ('"+(firstname +lastname ) +"', '"+firstname +"', '"+lastname +"','sunrise', 1);  SELECT SCOPE_IDENTITY()";
                                            model.ContractorID  = int.Parse (cmd.ExecuteScalar().ToString ());
                                            cmd.CommandText = "insert into tblCompanyUser (CompanyID, StartDate, RoleID, UserID, note) values (1,'"+DateTime .Now +"',5,"+ model.ContractorID + ",'create from quickbook loading')";
                                            cmd.ExecuteNonQuery();
                                            //setup default unitid as ownerland realty
                                            model.UnitID = 18;
                                        }
                                    }
                                    model.FinancialBankAccountID = FinancialBankAccountID;
                                    model.DueDate = DateTime.Parse(dr[0].ToString());
                                    model.CompleteDate = model.DueDate;
                                    model.Memo = (dr[2].ToString() + '-' + dr[3].ToString() + '-' + dr[4].ToString()).Replace("'", "");
                                    model.BankTracking = FinancialBankAccountID;

                                }
                                else
                                {
                                    model.IsCredit = (dr[6] != null && !String.IsNullOrEmpty(dr[6].ToString()));
                                    if (model.IsCredit)
                                    {
                                        model.Payment = double.Parse(dr[6].ToString());
                                        model.DueAmount = model.Payment;
                                        //set all deposit as rent by default.
                                        model.CategoryID = 36;
                                    }
                                    else
                                    {
                                        model.Payment = -double.Parse(dr[5].ToString());
                                        model.DueAmount = -model.Payment;
                                        //set all expense as repair by default.
                                        model.CategoryID = 17;
                                    }
                                    model.CompleteDate = DateTime.Parse(dr[2].ToString());
                                    model.DueDate = model.CompleteDate;
                                    model.Memo = (dr[4].ToString() + "-" + dr[10].ToString()).Replace("'", "") ;
                                    model.BankTracking = ("53 bank"+dr[3].ToString().Replace ("'","") + "-" + dr[8].ToString().Replace("'", "") + "-" + dr[9].ToString().Replace("'", "")).Replace("'", ""); ;
                                }
                                int i = (model.IsCredit == true) ? 1 : 0;
                                model.StatusID = 1;
                                model.UploadBy = ((int)Session["UserID"]);
                                model.FinancialBankAccountID = accountid.ToString ();
                                if(model.Memo.IndexOf("'") > 0)
                                {
                                    model.Memo = model.Memo.Replace("'", "");
                                }
                                cmd.CommandText = "insert into tblFinancialAccountOperation(UploadedBy, StatusID, CategoryID, IsCredit,  DueDate, DueAmount, Description,FinancialAccountID, Amount, FinishDate, BankTracking, FoundInExpense, HasConsolidated, ContractorID, UnitID) values (";
                                cmd.CommandText += model.UploadBy+","+model.StatusID + ", " + model.CategoryID + ", " + i + ",'" + model.DueDate + "', " + model.DueAmount + ", '" + model.Memo;
                                cmd.CommandText += "'," + model.FinancialBankAccountID + ", " + model.Payment + ", '" + model.CompleteDate + "', '" + model.BankTracking + "', 0,0, " + model.ContractorID +","+ model.UnitID + ")";

                                try
                                {
                                    cmd.ExecuteNonQuery();
                                    operations.Add(model);
                                }
                                catch(Exception ex)
                                {
                                    LogException(ex.Message + cmd.CommandText.Replace ("'",""));
                                }
                            }
                            //loadBatch.operations = operations;
                        }
                        catch (Exception ex)
                        {
                            LogException(ex.Message);
                        }
                        finally
                        {
                            sqlConn.Close();
                        }
                        ViewBag.TotalPayment = 0;
                        ViewBag.TotalDeposit = 0;
                        ViewBag.TotalBalace = 0;
                        return View(operations);
                    }
                    else
                    {
                        ModelState.AddModelError("File", "This file format is not supported");
                        LogException("This file format is not supported");
                        return View();
                    }
                }
                else
                {
                    ModelState.AddModelError("File", "Please Upload Your file");
                    LogException("This file format is not supported");
                }
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Add(OperationRecord model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Add new bank record";

            int invoiceID = OperationRecordManager.CreateOperationRecord(model);
            return RedirectToAction("Index");
        }
      

        [AllowAnonymous]
        public ActionResult Edit(int id)
        {
            try
            {
                if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
                ViewBag.ReportTitle = "Edit Bank Record";

                var model = OperationRecordManager.GetExpenseByID(id);

                model.AllUnits = GetSelectListItems((short)Helpers.Helpers.ListType.unit);
                model.AllBankAccount = GetSelectListItems((short)Helpers.Helpers.ListType.bankaccount);
                model.AllTenant = GetSelectListItems((short)Helpers.Helpers.ListType.allUser);
                model.AllCategory = GetSelectListItems((short)Helpers.Helpers.ListType.allExpenseCategory);
                model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);

                return View(model);
            }
            catch(Exception ex)
            {
                LogException(ex.Message);
                return View();
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Edit(OperationRecord model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Edit Bank Record";
            
            OperationRecordManager.Edit(model);
            return RedirectToAction("Index");
        }


        [AllowAnonymous]
        public ActionResult Transfer(int id)
        {
            try
            {
                if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
                ViewBag.ReportTitle = "Transfer Fund To Other Bank";

                OperationRecord model = OperationRecordManager.GetExpenseByID(id);
               
                model.AllUnits = GetSelectListItems((short)Helpers.Helpers.ListType.unit);
                model.AllBankAccount = GetSelectListItems((short)Helpers.Helpers.ListType.bankaccount);
                model.AllTenant = GetSelectListItems((short)Helpers.Helpers.ListType.allUser);
                model.AllCategory = GetSelectListItems((short)Helpers.Helpers.ListType.allExpenseCategory);
                model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);

                return View(model);
            }
            catch (Exception ex)
            {
                LogException(ex.Message);
                return View();
            }
        }

        [AllowAnonymous]
        public ActionResult AddAndTransferFund()
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Add and Transfer Fund To Other Bank";

            OperationRecord model = new OperationRecord();

            model.AllTenant = GetSelectListItems((short)Helpers.Helpers.ListType.allUser);
            model.AllUnits = GetSelectListItems((short)Helpers.Helpers.ListType.unit);
            model.AllBankAccount = GetSelectListItems((short)Helpers.Helpers.ListType.bankaccount);
            model.AllCategory = GetSelectListItems((short)Helpers.Helpers.ListType.allExpenseCategory);
            model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Transfer(OperationRecord model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Transfer Fund To Other Bank";

            OperationRecordManager.TransferFund(model);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult AddAndTransferFund(OperationRecord model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Transfer Fund To Other Bank";

            OperationRecordManager.AddAndTransferFund(model);
            return RedirectToAction("Index");
        }

        public double GetBankAccountBlance(DateTime currentDate, string[] bankAccounts)
        {
            double accountHistoryBalance = 0;
            StringBuilder sbOperation = new StringBuilder();
            sbOperation.Append("Select sum(Amount) from tblUnitOperation ");
            sbOperation.Append(" where tblUnitOperation.StatusID =  " + (short)Helpers .Helpers .StatusType .Close +" and UnitID<> 0 and contractorid <>0 ");
            if (bankAccounts != null && bankAccounts.Count() > 0 && !string.IsNullOrEmpty(bankAccounts[0]))
            {
                sbOperation.Append(" AND tblUnitOperation.FinancialAccountID IN (" + String.Join(",", bankAccounts) + ")");
            }
            sbOperation.Append(" and tblUnitOperation.FinishDate <'" + currentDate +"'");
            // sbOperation.Append(" group by isCredit order by isCredit");


            // Execute the SQL query and get the results
            string connectionString = ConfigurationManager.ConnectionStrings["SunriseConnectionString"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(sbOperation.ToString(), connection); // Create the Command and Parameter objects

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                List<double> amount = new List<double>();
                while (reader.Read()) // Read each result row and extract the data
                {
                    // amount.Add(double.Parse(reader[0].ToString()));
                    if (reader[0] != DBNull.Value)
                    {
                        accountHistoryBalance = double.Parse(reader[0].ToString());
                    }
                }
                reader.Close();
                //if (amount.Count > 1)
                //{ 
                //    //credit minus debit
                //    accountHistoryBalance = amount[1] + amount[0];
                //}
            }
            return accountHistoryBalance;
        }
    }

}
