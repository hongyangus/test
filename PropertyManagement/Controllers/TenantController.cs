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

namespace PropertyManagement.Controllers
{
    [Authorize]
    public class TenantController : BaseController
    {
        //
        // GET: /ManageUser/

        private string reporttitle = "Manage Tenant";

        [AllowAnonymous]
        public ActionResult Index()
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = reporttitle;

            var companies = GetList((short)Helpers.Helpers.ListType.company);
            ViewBag.companies = new MultiSelectList(companies, "id", "description");

            var properties = GetList((short)Helpers.Helpers.ListType.property);
            ViewBag.properties = new MultiSelectList(properties, "id", "description");

            var units = GetList((short)Helpers.Helpers.ListType.unit);
            ViewBag.units = new MultiSelectList(units, "id", "description");

            var statusList = GetList((short)Helpers.Helpers.ListType.allStatus);
            ViewBag.statusList = new MultiSelectList(statusList, "id", "description");

            var contractors = GetList((short)Helpers.Helpers.ListType.allTenant);
            ViewBag.contractors = new MultiSelectList(contractors, "id", "description");

            //setup default value of the start date and end date
            if (Session["startDate"] == null)
            {
                DateTime oneMonth = DateTime.Now.AddMonths(-120);
                Session["startDate"] = new DateTime(oneMonth.Year, oneMonth.Month, 1).ToString("MM/dd/yyyy");
                Session["endDate"] = DateTime.Now.ToString("MM/dd/yyyy");
                string[] statusIDs = new string[] { ((int)Helpers.Helpers.StatusType.Open).ToString() };
                Session["selectedStatusIDs"] = statusIDs;
            }

            return View();
        }

        [AllowAnonymous]
        public PartialViewResult ReportView(string startDate, string endDate, string[] companyIDs, string[] propertyIDs, string[] unitIDs, string[] statusIDs, string[] contractorIDs)
        {
            Session["startDate"] = startDate;
            Session["endDate"] = endDate;
            Session["selectedCompanyIDs"] = companyIDs;
            Session["selectedPropertyIDs"] = propertyIDs;
            Session["selectedUnitIDs"] = unitIDs;
            Session["selectedStatusIDs"] = statusIDs;
            Session["selectedContractorIDs"] = contractorIDs;

            DateTime start = DateTime.Parse(startDate);
            DateTime end = DateTime.Parse(endDate);
            double totalRentRoll = 0;
            double totalSecurityDeposit = 0;
            double totalBalance = 0;
            StringBuilder sbOperation = new StringBuilder();
            sbOperation.Append("Select distinct tblTenant.*,  tblProperty.Address,cUser.*, tblPropertyUnit.UnitName,cStatusType.Name as StatusName, OperationSummary.balance ,PaidSecurity ");
            sbOperation.Append(" from tblTenant ");
            sbOperation.Append(" inner join  tblPropertyUnit on tblPropertyUnit.UnitID =  tblTenant.UnitID ");
            sbOperation.Append(" INNER JOIN  tblProperty ON tblProperty.PropertyID = tblPropertyUnit.PropertyID ");
            sbOperation.Append(" INNER JOIN mCompanyProperty on mCompanyProperty.PropertyID = tblProperty.PropertyID ");
            sbOperation.Append(" INNER JOIN cUser on cUser.UserID =  tblTenant.UserID ");
            sbOperation.Append(" INNER JOIN cStatusType on cStatusType.StatusTypeID = tblTenant.StatusID ");
            sbOperation.Append(" LEFT OUTER JOIN (select sum(DueAmount - Amount) as balance, contractorid from tblUnitOperation where CategoryID=36 group by contractorid) as OperationSummary on OperationSummary.ContractorID = tblTenant.UserID ");
            sbOperation.Append(" LEFT OUTER JOIN (select sum(Amount) as PaidSecurity, contractorid from tblUnitOperation where categoryid=32 group by contractorid) as SecuritySummary on SecuritySummary.ContractorID = tblTenant.UserID ");
            sbOperation.Append(" where  dateadd(month, tblTenant.LeaseTerm,tblTenant.StartDate)>='" + start + "' ");
            sbOperation.Append(" and tblTenant.StartDate<='" + end + "'");

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
                sbOperation.Append(" AND tblTenant.StatusID IN (" + String.Join(",", statusIDs) + ")");
            }
            if (contractorIDs != null && contractorIDs.Count() > 0 && !string.IsNullOrEmpty(contractorIDs[0]))
            {
                sbOperation.Append(" AND [tblTenant].UserID IN (" + String.Join(",", contractorIDs) + ")");
            }
            sbOperation.Append(" Order by StartDate");

            // Create a list of our result class to hold the data from the query
            // Please ensure you instatiate the class for this controller and not a different controller
            List<Tenant> result = new List<Tenant>();
            // Execute the SQL query and get the results

            using (SqlDataAdapter adapter = new SqlDataAdapter(sbOperation.ToString(), Helpers.Helpers.GetAppConnectionString()))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                if (tb != null && tb.Rows.Count > 0)
                {
                    for (int i = 0; i <tb.Rows.Count; i++)
                    {
                        Tenant  tr = TenantManager.FillInTenantWithData(tb.Rows[i]);
                        result.Add(tr);
                        totalRentRoll += tr.MonthlyPayment;
                        totalSecurityDeposit += tr.SecurityDeposit;
                    }
                }
            }

            ViewBag.TableCaption = reporttitle + " Tenant: " + start.ToString("g") + " thru " + end.ToString("g");
            ViewBag.TotalRentRoll = totalRentRoll;
            ViewBag.TotalDeposit = totalSecurityDeposit;
            ViewBag.TotalBalace = 0;
            return PartialView("ReportView", result);
        }

        [AllowAnonymous]
        public ActionResult Application()
        {
            ViewBag.ReportTitle = "New tenant application";
            // add new user
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            
            var allUnits = GetList((short)Helpers.Helpers.ListType.unit);

            Tenant model = new Tenant();
            
            model.AllUnits = GetSelectListItems((short)Helpers.Helpers.ListType.unit);

            return View(model);
        }

        [AllowAnonymous]
        public ActionResult Add()
        {
            // add new user
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Create new tenant";
            // Get the users from the DB for the drop-down listbox
            var allUsers = GetList((short)Helpers.Helpers.ListType.allUser);

            var allBankAccount = GetList((short)Helpers.Helpers.ListType.bankaccount);

            var allUnits = GetList((short)Helpers.Helpers.ListType.unit);

            Tenant model = new Tenant();

            model.AllTenant = GetSelectListItems((short)Helpers.Helpers.ListType.allUser);
            model.AllUnits = GetSelectListItems((short)Helpers.Helpers.ListType.unit);
            model.AllBankAccount = GetSelectListItems((short)Helpers.Helpers.ListType.bankaccount);

            return View(model);
        }

        [AllowAnonymous]
        public ActionResult Operation(int id)
        {
            // add new user
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Tenant Operation";
            // Get the users from the DB for the drop-down listbox
            var allUsers = GetList((short)Helpers.Helpers.ListType.allUser);

            var allBankAccount = GetList((short)Helpers.Helpers.ListType.bankaccount);

            var allUnits = GetList((short)Helpers.Helpers.ListType.unit);

            Tenant model = TenantManager .GetByID (id);

            model.AllTenant = GetSelectListItems((short)Helpers.Helpers.ListType.allUser);
            model.AllUnits = GetSelectListItems((short)Helpers.Helpers.ListType.unit);
            model.AllBankAccount = GetSelectListItems((short)Helpers.Helpers.ListType.bankaccount);

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [HandleError]
        public ActionResult Operation(Tenant model, string submit)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            switch(submit)
            {
                case "Save":
                    break;
                case "Generate Residential Confirmation Letter":
                    Email.EmailResidentialConfirmation(model.TenantID, model.SendEmailAddress);
                    break;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult SendResidentConfirmationLetter(Tenant model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            Email.EmailResidentialConfirmation(model.TenantID , model.Note);
            return RedirectToAction("Index");
        }


        [HttpPost]
        [AllowAnonymous]
        public ActionResult Add(Tenant model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Add Tenant Record";

            SqlConnection sqlConn = new SqlConnection(Helpers .Helpers .GetAppConnectionString());
            SqlCommand cmd = sqlConn.CreateCommand();
            DataTable dtSearchResult = new DataTable();
            SqlDataAdapter daSearchResult = new SqlDataAdapter();
            model.StatusID = (int)Helpers.Helpers.StatusType.Open;

            try
            {
                sqlConn.Open();
                int DepositStatusID = (int)Helpers.Helpers.StatusType.Open;
                if (model.SecurityDeposit == model.PaidSecurityDeposit)
                {
                    DepositStatusID = (int)Helpers.Helpers.StatusType.Close;
                }
                if (model.SecurityDepositPaidDate == null)
                {
                    model.SecurityDepositPaidDate = DateTime .Now ;
                }

                //add record of the tenant
                cmd.CommandText = "INSERT INTO  tblTenant (UserID, UnitID,  leaseterm,StartDate, monthlypayment,securitydeposit, StatusID, Note) VALUES ( "
                    +  model.UserID + "," + model.UnitId + "," + model.LeaseTerm + ",'" + model.StartDate.ToShortDateString() + "'," + model.MonthlyPayment  + "," + model.SecurityDeposit  + "," + model.StatusID + ",'" + model.Note  + "'); SELECT SCOPE_IDENTITY();";
                int tenantID = int.Parse(cmd.ExecuteScalar().ToString());


                //add record of the security deposit payment
                cmd.CommandText = "INSERT INTO  tblRent (TenantID, RentAmount, PaidAmount, DueDate, IsRent, Note, StatusID, PaymentDate, FinancialAccountID) VALUES ( " 
                    + tenantID + "," + model.SecurityDeposit + "," + model.PaidSecurityDeposit + ",'" + model.StartDate.ToShortDateString() + "', 0 ,'" + model.Note  + "'," + DepositStatusID + ",'" + model.SecurityDepositPaidDate.ToShortDateString() + "'," + model.FinancialBankAccountID + "); SELECT SCOPE_IDENTITY();";

                int rentid = int.Parse(cmd.ExecuteScalar().ToString());
                OperationRecord operation = new OperationRecord();
                operation.StatusID = (short)DepositStatusID;
                operation.CategoryID = (int)Helpers.Helpers.ExpenseCategory.SecurityDeposit;
                operation.IsCredit = true;
                operation.ContractorID = model.UserID;
                operation.UnitID = model.UnitId;
                operation.DueDate = model.StartDate;
                operation.DueAmount = model.SecurityDeposit;
                operation.Payment = model.PaidSecurityDeposit;
                operation.Memo = model.Note;
                operation.LinkedRentID = rentid.ToString ();
                operation.CompleteDate = model.SecurityDepositPaidDate;
                operation.FinancialBankAccountID = model.FinancialBankAccountID;
                operation.UploadBy = (int)Session["UserID"];
                int opID = OperationRecordManager.CreateOperationRecord(operation);

                Email.EmailLease(tenantID, (int)Helpers.Helpers.EmailType.LeaseConfirmation);


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
            ViewBag.ReportTitle = "Edit Lease Terms";

            Tenant tenant = TenantManager.GetByID(id);

            tenant.AllUnits = GetSelectListItems((short)Helpers.Helpers.ListType.unit);
            tenant.AllBankAccount = GetSelectListItems((short)Helpers.Helpers.ListType.bankaccount);
            tenant.AllTenant = GetSelectListItems((short)Helpers.Helpers.ListType.allUser);
            tenant.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);

            return View(tenant);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Edit(Tenant model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Edit Lease Terms";
            TenantManager.EditLease(model);

            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public ActionResult TerminateLease(int id)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            TenantManager.TerminateLease(id);
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public ActionResult Remind(int id)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            Tenant model = TenantManager.GetByID(id);
            Email.EmailLease(model.TenantID, (int)Helpers.Helpers.EmailType.LeaseConfirmation);
            return RedirectToAction("Index");
        }
        
    }
}
