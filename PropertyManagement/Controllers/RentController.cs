using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.Data.SqlClient;
using System.Data.Odbc;
using System.Text;
using PropertyManagement.Models;
using System.Data;

namespace PropertyManagement.Controllers
{
    [Authorize]
    public class RentController : BaseController
    {
        private string reporttitle = "Rent"; // Specify the report title here

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Index()
        {
            // Enable security to redirect to login page if user is not logged in or we are not running in the VS IDE
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            // Save the report title to the ViewBag
            ViewBag.ReportTitle = reporttitle;

            var companies = GetList((short)Helpers.Helpers.ListType.company);
            ViewBag.companies = new MultiSelectList(companies, "id", "description");

            var properties = GetList((short)Helpers.Helpers.ListType.property);
            ViewBag.properties = new MultiSelectList(properties, "id", "description");

            var units = GetList((short)Helpers.Helpers.ListType.unit);
            ViewBag.units = new MultiSelectList(units, "id", "description");

            var bankAccounts = GetList((short)Helpers.Helpers.ListType.bankaccount);
            ViewBag.bankAccounts = new MultiSelectList(bankAccounts, "id", "description");

            var statusList = GetList((short)Helpers.Helpers.ListType.allStatus);
            ViewBag.statusList = new MultiSelectList(statusList, "id", "description");

            var categoryList = GetList((short)Helpers.Helpers.ListType.allExpenseCategory);
            ViewBag.categories = new MultiSelectList(categoryList, "id", "description");

            var contractors = GetList((short)Helpers.Helpers.ListType.allTenant);
            ViewBag.contractors = new MultiSelectList(contractors, "id", "description");

            ViewBag.rentMonth = DateTime.Now;


            //setup default value of the start date and end date
            if (Session["startDate"] == null)
            {
                DateTime oneMonth = DateTime.Now.AddMonths(-1);
                Session["startDate"] = new DateTime(oneMonth.Year, oneMonth.Month, 1).ToString("MM/dd/yyyy");
                Session["endDate"] = DateTime.Now.ToString("MM/dd/yyyy");
                string[] statusIDs = new string[] { ((int)Helpers.Helpers.StatusType.Open).ToString ()};
                Session["selectedStatusIDs"] = statusIDs;
            }

            return View();
        }

        [AllowAnonymous]
        public PartialViewResult ReportView(string startDate, string endDate, string[] companyIDs, string[] propertyIDs, string[] unitIDs, string[] bankAccountIDs, string[] statusIDs, string[] contractorIDs)
        {
            Session["startDate"] = startDate;
            Session["endDate"] = endDate;
            Session["selectedCompanyIDs"] = companyIDs;
            Session["selectedPropertyIDs"] = propertyIDs;
            Session["selectedUnitIDs"] = unitIDs;
            Session["selectedAccountIDs"] = bankAccountIDs;
            Session["selectedStatusIDs"] = statusIDs;
            Session["selectedContractorIDs"] = contractorIDs;

            DateTime start = DateTime.Parse(startDate);
            DateTime end = DateTime.Parse(endDate);
            double totalPayment = 0;
            double totalDeposit = 0;
            double totalBalace = 0;
            StringBuilder sbOperation = new StringBuilder();
            sbOperation.Append("Select distinct RENTID, tblRent.DueDate, tblRent.PaymentDate, PaidBy.FirstName +' ' + PaidBy.LastName as PaidBy, tblUnitOperation.ID as OperationID,  ");
            sbOperation.Append("(tblRent.Note +' - ' + tblRent.BankTracking) as Note,PaidAmount, tblAccount.AccountName, tblProperty.Address, tblPropertyUnit.UnitName, tblRent.IsCredit, RentAmount, tblRent.StatusID, cStatusType.Name as StatusName, tblUnitOperation.LinkedExpenseID from tblRent ");
            sbOperation.Append(" inner join  tblTenant on tblTenant.TenantID =  tblRent.TenantID ");
            sbOperation.Append(" inner join  tblPropertyUnit on tblPropertyUnit.UnitID =  tblTenant.UnitID ");
            sbOperation.Append(" INNER JOIN  tblProperty ON tblProperty.PropertyID = tblPropertyUnit.PropertyID ");
            sbOperation.Append(" INNER JOIN mCompanyProperty on mCompanyProperty.PropertyID = tblProperty.PropertyID ");
            sbOperation.Append(" INNER JOIN cUser as PaidBy on PaidBy.UserID =  tblTenant.UserID ");
            sbOperation.Append(" INNER JOIN cStatusType on cStatusType.StatusTypeID = tblRent.StatusID ");
            sbOperation.Append(" LEFT OUTER JOIN tblAccount as tblAccount on tblAccount.FinancialAccountID = tblRent.FinancialAccountID ");
            sbOperation.Append(" LEFT OUTER JOIN tblUnitOperation on tblUnitOperation.LinkedRentID = tblRent.RENTID ");
            sbOperation.Append(" where [tblRent].DueDate>='" + start + "' ");
            sbOperation.Append(" and [tblRent].DueDate<='" + end + "'");

            // Add modality id to the where clause if appropriate
            if (bankAccountIDs != null && bankAccountIDs.Count() > 0 && !string.IsNullOrEmpty(bankAccountIDs[0]))
            {
                sbOperation.Append(" AND tblRent.FinancialAccountID IN (" + String.Join(",", bankAccountIDs) + ")");
            }
            // Add modality id to the where clause if appropriate
            if (companyIDs != null && companyIDs.Count() > 0 && !string.IsNullOrEmpty(companyIDs[0]))
            {
                sbOperation.Append(" AND mCompanyProperty.CompanyID IN (" + String.Join(",", companyIDs) + ")");
            }
            else
            {
                //get the companys only the owner can access
                sbOperation.Append(" AND mCompanyProperty.CompanyID IN (" + GetUserManagedCompanyString() + ")");
            }
            // Add modality id to the where clause if appropriate
            if (propertyIDs != null && propertyIDs.Count() > 0 && !string.IsNullOrEmpty(propertyIDs[0]))
            {
                sbOperation.Append(" AND tblProperty.PropertyID IN (" + String.Join(",", propertyIDs) + ")");
            }
            // Add modality id to the where clause if appropriate
            if (unitIDs != null && unitIDs.Count() > 0 && !string.IsNullOrEmpty(unitIDs[0]))
            {
                sbOperation.Append(" AND tblPropertyUnit.UnitID IN (" + String.Join(",", unitIDs) + ")");
            }
            if (statusIDs != null && statusIDs.Count() > 0 && !string.IsNullOrEmpty(statusIDs[0]))
            {
                sbOperation.Append(" AND [tblRent].StatusID IN (" + String.Join(",", statusIDs) + ")");
            }
            if (contractorIDs != null && contractorIDs.Count() > 0 && !string.IsNullOrEmpty(contractorIDs[0]))
            {
                sbOperation.Append(" AND [tblTenant].UserID IN (" + String.Join(",", contractorIDs) + ")");
            }
            sbOperation.Append(" Order by tblRent.DueDate, tblRent.PaymentDate");

            // Create a list of our result class to hold the data from the query
            // Please ensure you instatiate the class for this controller and not a different controller
            List<OperationRecord> result = new List<OperationRecord>();
            // Execute the SQL query and get the results

            using (SqlDataAdapter adapter = new SqlDataAdapter(sbOperation.ToString (), Helpers .Helpers .GetAppConnectionString ()))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                if (tb != null && tb.Rows.Count > 0)
                {
                    for (int i = 0; i <tb.Rows.Count; i++)
                    {
                        DataRow dr = tb.Rows[i];
                        OperationRecord row = new OperationRecord();
                        row.DueDate = DateTime.Parse(dr["DueDate"].ToString ());
                        if (dr["PaymentDate"] != DBNull.Value ) { row.CompleteDate = DateTime.Parse(dr["PaymentDate"].ToString()); }
                        row.PaidBy = dr["PaidBy"].ToString();
                        row.Memo = dr["Note"].ToString();
                        if (dr["PaidAmount"] != DBNull.Value)
                        {
                            row.Payment = double.Parse(dr["PaidAmount"].ToString());
                        }
                        row.BankAccountName = dr["AccountName"].ToString();
                        row.Address = dr["Address"].ToString() +"--"+ dr["UnitName"].ToString();
                        if (dr["RentAmount"] != DBNull.Value)
                        {
                            row.DueAmount = double.Parse(dr["RentAmount"].ToString());
                        }
                        if (dr["OperationID"] != DBNull.Value)
                        {
                            row.ID = int.Parse(dr["OperationID"].ToString());
                        }
                        row.LinkedRentID = dr["RentID"].ToString();
                        if (dr["StatusID"] != DBNull.Value)
                        {
                            row.StatusName = dr["StatusName"].ToString();
                            row.StatusID = short.Parse(dr["StatusID"].ToString());
                        }
                        if (dr["LinkedExpenseID"] != DBNull.Value)
                        {
                            row.LinkedExpenseID = Int32.Parse (dr["LinkedExpenseID"].ToString());
                        }
                        else
                        {
                            row.LinkedExpenseID = 0;
                        }
                        totalBalace += row.Payment - row.DueAmount;
                        row.Balance = totalBalace;

                        result.Add(row);
                        totalDeposit += row.Payment;
                        totalPayment += row.DueAmount;
                    }
                }
            }
            
            ViewBag.TableCaption = reporttitle + " Unpaid rent: " + start.ToString("g") + " thru " + end.ToString("g");
            ViewBag.TotalPayment = totalPayment;
            ViewBag.TotalDeposit = totalDeposit;
            ViewBag.TotalBalace = totalBalace;
            return PartialView("ReportView", result);
        }


        [HttpGet]
        [AllowAnonymous]
        public ActionResult Add()
        {
            // add new user
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Add Rent";

            OperationRecord model = new OperationRecord();

            model.AllTenant = GetSelectListItems((short)Helpers.Helpers.ListType.allTenantWithUnit);
            model.AllBankAccount = GetSelectListItems((short)Helpers.Helpers.ListType.bankaccount);

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Add(OperationRecord model, string actionName)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Add Rent";

            SqlConnection sqlConn = new SqlConnection(ConfigurationManager.ConnectionStrings["SunriseConnectionString"].ConnectionString);
            SqlCommand cmd = sqlConn.CreateCommand();
            DataTable dtSearchResult = new DataTable();
            SqlDataAdapter daSearchResult = new SqlDataAdapter();

            try
            {
                sqlConn.Open();
                if (string.IsNullOrEmpty(model.RentMonth))
                {
                   if (model.TenantID <= 0)
                    {
                        return ExecutionError("Please select Tenant");
                    }

                    //the tenant id was stored in the contractorid
                    Tenant tenant = TenantManager .GetByID ( model.TenantID);

                    int isRent = 1;
                    if (model.IsSecurityDeposit )
                    {
                        model.CategoryID = 32;
                        isRent = 0;
                    }
                    else
                    {
                        model.CategoryID = 36;
                    }
                    if(!model.IsCredit )
                    {
                        model.Payment = -model.Payment;
                    }
                    model.StatusID = (int)Helpers.Helpers.StatusType.Open;
                    if (model.DueAmount == model.Payment || model.DueAmount == 0)
                    {
                        model.StatusID = (int)Helpers .Helpers .StatusType .Close ;
                    }
                    if (model.CompleteDate == null)
                    {
                        model.CompleteDate = model.DueDate;
                    }
                    model.ContractorID = tenant.UserID;
                    model.UnitID = tenant.UnitId;
                    model.LinkedRentID = OperationRecordManager.CreateRent(model, tenant .TenantID ).ToString();

                    model.ID = OperationRecordManager.CreateOperationRecord(model);

                    if (model.IsEmailReceipt)
                    {
                        Email.EmailPayment(model.ID, model.ContractorID, model.UnitID ,  model.CompleteDate,model.FinancialBankAccountID, model.DueAmount, model.Payment,  model.CategoryID);
                    }
                }
                sqlConn.Close();
            }
            catch (SqlException ex)
            {
                ViewBag.MyExeption = ex.Message;
                ViewBag.MyExeptionCSS = "errorMessage";
            }
            finally
            {
                sqlConn.Close();
            }
            return RedirectToAction("Index");
        }


        [HttpGet]
        [AllowAnonymous]
        public ActionResult AddMonthlyRent()
        {
            OperationRecord model = new OperationRecord();
            return View(model);
        }
        
        [AllowAnonymous]
        [HttpPost]
        public ActionResult AddMonthlyRent(OperationRecord model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Add Rent";

            SqlConnection sqlConn = new SqlConnection(ConfigurationManager.ConnectionStrings["SunriseConnectionString"].ConnectionString);
            SqlCommand cmd = sqlConn.CreateCommand();
            DataTable dtSearchResult = new DataTable();
            SqlDataAdapter daSearchResult = new SqlDataAdapter();

            try
            {
                sqlConn.Open();
                //create monthly rents
                cmd.CommandText = "Select [tblTenant].UserID, [tblTenant].MonthlyPayment, tblPropertyUnit.UnitID, [tblTenant].TenantID from [tblTenant], tblPropertyUnit, mCompanyProperty WHERE tblTenant.StartDate <'" + DateTime.Now + "' and tblTenant.TerminateDate is null and statusid=1 "
                    + " and tblTenant.UnitID = tblPropertyUnit.UnitID and tblPropertyUnit.PropertyID = mCompanyProperty.PropertyID AND tblTenant.StatusID= 1 and mCompanyProperty.CompanyID in " + GetUserManagedCompanyString() ;

                daSearchResult.SelectCommand = cmd;
                daSearchResult.Fill(dtSearchResult);
                Int16 tenantID = 0;
                Int16 userid = 0;
                Int16 unitID = 0;
                Decimal rentAmount = 0;
                
                //loop through all tenant to create rent record for this month
                foreach (DataRow row in dtSearchResult.Rows)
                {
                    if (row[0] != DBNull.Value)
                    {
                        userid = Int16.Parse(row[0].ToString());
                        model.ContractorID = userid;
                    }
                    if (row[1] != DBNull.Value)
                    {
                        model.DueAmount  = Double.Parse(row[1].ToString());
                    }
                    if (row[2] != DBNull.Value)
                    {
                        model.UnitID = Int16.Parse(row[2].ToString());
                    }
                    if (row[3] != DBNull.Value)
                    {
                        tenantID = Int16.Parse(row[3].ToString());
                    }
                    model.CategoryID = (int)Helpers.Helpers.ExpenseCategory.Rent;
                    model.Memo = "auto generated rent for " + model.DueDate.ToShortDateString();
                    model.FinancialBankAccountID = "11";
                    model.StatusID = (int)Helpers.Helpers.StatusType.Open;
                    model.LinkedRentID = OperationRecordManager.CreateRent(model, tenantID).ToString ();

                    OperationRecordManager.CreateOperationRecord(model);
                }
                sqlConn.Close();
            }
            catch (SqlException ex)
            {
                ViewBag.MyExeption = ex.Message;
                ViewBag.MyExeptionCSS = "errorMessage";
            }
            finally
            {
                sqlConn.Close();
            }
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public ActionResult Edit(int id)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Edit Rent";

            var model = OperationRecordManager.GetExpenseByRentID(id);
            if(model.CategoryID == (int)Helpers .Helpers .EmailType .SecurityDeposit )
            {
                model.IsSecurityDeposit = true;
            }
            //stored the tenant id in the contractorid
            model.AllBankAccount = GetSelectListItems((short)Helpers.Helpers.ListType.bankaccount);
            model.AllTenant = GetSelectListItems((short)Helpers.Helpers.ListType.allTenantWithUnit);
            model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Edit(OperationRecord model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            OperationRecord oldOp = OperationRecordManager.GetExpenseByRentID(Int32.Parse (model.LinkedRentID));
            ViewBag.ReportTitle = "Edit Rent Record";
            model.UploadBy = Int32.Parse(Session["UserID"].ToString());
            model.ID = oldOp.ID;
            //the tenantid is stored in the contractorid
            Tenant tenant = TenantManager.GetByID(model.TenantID);
            model.ContractorID = tenant.UserID;
            model.UnitID = tenant.UnitId;
            OperationRecordManager.EditRent(model);
            
            return RedirectToAction("Index");
        }


        [AllowAnonymous]
        public ActionResult ReceiveRent(int id)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Receive Rent";

            OperationRecord model = OperationRecordManager.GetExpenseByRentID(id);
            model.CompleteDate = DateTime.Now;
            model.UploadBy = Int32.Parse (Session["UserID"].ToString ());
            model.StatusID = (int)Helpers.Helpers.StatusType.Close;
            model.AllUnits = GetSelectListItems((short)Helpers.Helpers.ListType.unit);
            model.AllBankAccount = GetSelectListItems((short)Helpers.Helpers.ListType.bankaccount);
            model.AllTenant = GetSelectListItems((short)Helpers.Helpers.ListType.allUser);
            model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult ReceiveRent(OperationRecord model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }

            model.IsCredit = true;
            model.UploadBy = Int32.Parse(Session["UserID"].ToString());
            OperationRecordManager.ReceiveRent(model);

            //redirect
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public ActionResult Remind(int id)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            var record = OperationRecordManager.GetExpenseByRentID(id);
            Email.EmailReminder(record.ID, record.ContractorID, record .UnitID, record.DueDate, record .DueAmount, (int)Helpers .Helpers .EmailType .RentReminder );
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public ActionResult Delete(int id)
        {
            // Enable security to redirect to login page if user is not logged in or we are not running in the VS IDE
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            OperationRecordManager.DeleteRent(id);
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public ActionResult Transfer(int id)
        {
            try
            {
                if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
                ViewBag.ReportTitle = "Transfer Fund To Landlord";

                OperationRecord model = OperationRecordManager.GetExpenseByRentID(id);
                model = OperationRecordManager.GetExpenseByID(model.ID);
                
                model.AllBankAccount = GetSelectListItems((short)Helpers.Helpers.ListType.bankaccount);
                model.AllTenant = GetSelectListItems((short)Helpers.Helpers.ListType.allTenantWithUnit);
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

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Transfer(OperationRecord model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            if (String.IsNullOrEmpty(model.TransferedFinancialBankAccountID))
            {
                return ExecutionError("Please select either Tenant or Unit");
            }
            model.UploadBy = Int32.Parse(Session["UserID"].ToString());
            Tenant tenant = TenantManager.GetByID(model.TenantID);
            model.ContractorID = tenant.UserID;
            model.UnitID = tenant.UnitId;
            OperationRecordManager.Reimburse(model);

            //send email to end user
            if (model.IsEmailReceipt)
            {
                Email.EmailPayment(model.ID, model.ContractorID, model.UnitID, model.CompleteDate, model.FinancialBankAccountID, model.DueAmount, model.Payment, (int)Helpers.Helpers.EmailType.Invoice);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [AllowAnonymous]
        public JsonResult GetTenantForUnit(string id)
        {
            SqlConnection sqlConn = new SqlConnection(ConfigurationManager.ConnectionStrings["SunriseConnectionString"].ConnectionString);
            SqlCommand cmd = sqlConn.CreateCommand();
            DataTable dtSearchResult = new DataTable();
            SqlDataAdapter daSearchResult = new SqlDataAdapter();
            int tenantID = 0;

            try
            {
                sqlConn.Open();
                if (!string.IsNullOrEmpty (id))
                {
                        //to find whether it match
                        cmd.CommandText = "select TenantID from tblTenant where UnitID = " + id + " AND StatusID=1";

                    daSearchResult.SelectCommand = cmd;
                    daSearchResult.Fill(dtSearchResult);
                    if (dtSearchResult.Rows.Count > 0)
                    {
                        tenantID = (int)dtSearchResult.Rows[0][0];
                    }
                }
               
                sqlConn.Close();
            }
            catch (SqlException ex)
            {
                ViewBag.MyExeption = ex.Message;
                ViewBag.MyExeptionCSS = "errorMessage";
            }
            finally
            {
                sqlConn.Close();
            }
            return Json(tenantID.ToString (), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [AllowAnonymous]
        public JsonResult GetUnitForTenant(string id)
        {
            SqlConnection sqlConn = new SqlConnection(ConfigurationManager.ConnectionStrings["SunriseConnectionString"].ConnectionString);
            SqlCommand cmd = sqlConn.CreateCommand();
            DataTable dtSearchResult = new DataTable();
            SqlDataAdapter daSearchResult = new SqlDataAdapter();
            int unitID = 0;

            try
            {
                sqlConn.Open();
                if (!string.IsNullOrEmpty(id))
                {
                    //to find whether it match
                    cmd.CommandText = "select UnitID from tblTenant where UserID = " + id + " AND StatusID=1";

                    daSearchResult.SelectCommand = cmd;
                    daSearchResult.Fill(dtSearchResult);
                    if (dtSearchResult.Rows.Count > 0)
                    {
                        unitID = (int)dtSearchResult.Rows[0][0];
                    }
                }

                sqlConn.Close();
            }
            catch (SqlException ex)
            {
                ViewBag.MyExeption = ex.Message;
                ViewBag.MyExeptionCSS = "errorMessage";
            }
            finally
            {
                sqlConn.Close();
            }
            return Json(unitID.ToString(), JsonRequestBehavior.AllowGet);
        }
    }
}
