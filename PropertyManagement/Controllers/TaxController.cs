using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using CCHMC.AD.Library.Extensions;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using PropertyManagement.Models;
using PropertyManagement.ViewModels;
using PropertyManagement.ViewModels.User;
using System.Data;
using System.Text;
using PropertyManagement.Helpers;


namespace PropertyManagement.Controllers
{
    public class TaxController :  BaseController
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

            var contractors = GetList((short)Helpers.Helpers.ListType.allUser);
            ViewBag.contractors = new MultiSelectList(contractors, "id", "description");

            //setup default value of the start date and end date
            DateTime startDate = new DateTime(DateTime.Now .Year -1, 1,1);
            Session["startDate"] = startDate.ToString("MM/dd/yyyy");
            Session["endDate"] = startDate.AddYears(1).ToString("MM/dd/yyyy");
            Session["expenseValue"] = "600";

            return View();
        }

        [AllowAnonymous]
        public PartialViewResult ReportView(string startDate, string endDate, string[] companyIDs,  double lowerThresholdValue)
        {
            Session["startDate"] = startDate;
            Session["endDate"] = endDate;
            Session["selectedCompanyIDs"] = companyIDs;


            DateTime start = DateTime.Parse(startDate);
            DateTime end = DateTime.Parse(endDate);
            double totalRentRoll = 0;
            double totalSecurityDeposit = 0;
            StringBuilder sb = new StringBuilder();
            sb.Append("select tblCompany.EIN,cuser.UserID, CUSER.FirstName,CUSER.LastName,  CUSER.address,cuser.City,cuser.State,cuser.Zip,CUSER.CellPhone, cuser.SSN, ");
            sb.Append("SUM(tblUnitOperation.AMOUNT) as TotalAmount FROM tblCompany ");
            sb.Append("INNER JOIN mCompanyProperty ON tblCompany.CompanyID = mCompanyProperty.CompanyID ");
            sb.Append("INNER JOIN tblPropertyUnit ON tblPropertyUnit.PropertyID = mCompanyProperty.PropertyID ");
            sb.Append("INNER JOIN tblUnitOperation ON tblUnitOperation.UnitID = tblPropertyUnit.UnitID ");
            sb.Append("INNER JOIN cUser ON cUser.UserID = tblUnitOperation.ContractorID ");
            sb.Append(" where  tblUnitOperation.FinishDate >='" + startDate + "' ");
            sb.Append(" and tblUnitOperation.FinishDate<='" + endDate + "'");

            // Add modality id to the where clause if appropriate
            if (companyIDs != null && companyIDs.Count() > 0 && !string.IsNullOrEmpty(companyIDs[0]))
            {
                sb.Append(" AND mCompanyProperty.CompanyID IN (" + String.Join(",", companyIDs) + ")");
            }
            else
            {
                //get the companys only the owner can access
                sb.Append(" AND mCompanyProperty.CompanyID IN (" + GetUserManagedCompanyString() + ")");
            }
            sb.Append(" group by tblCompany.EIN, cuser.UserID, tblCompany.CompanyID, ");
            sb.Append("DATENAME(year, tblUnitOperation.FinishDate) ,CUSER.FirstName,CUSER.LastName,CUSER.CellPhone, ");
            sb.Append("CUSER.address, CUSER.address,cuser.City,cuser.State,cuser.Zip,cuser.SSN ");
            sb.Append("HAVING SUM(tblUnitOperation.AMOUNT) <= -" + lowerThresholdValue);
            sb.Append(" Order by TotalAmount desc");
            List<User> allUser = new List<User>();

            using (SqlDataAdapter adapter = new SqlDataAdapter(sb.ToString(), Helpers.Helpers.GetAppConnectionString()))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];

                if (tb != null && tb.Rows.Count > 0)
                {
                    for (int i = 0; i < tb.Rows.Count; i++)
                    {
                        DataRow dr = tb.Rows[i];
                        allUser.Add(UserManager.FillInUserWithData(dr));
                    }
                }
            }

            ViewBag.TableCaption = reporttitle + " Tax Report for 1099 or w-2: " + start.ToString("g") + " thru " + end.ToString("g");
            ViewBag.TotalRentRoll = totalRentRoll;
            ViewBag.TotalDeposit = totalSecurityDeposit;
            ViewBag.TotalBalace = 0;
            return PartialView("ReportView", allUser);
        }


    }
}
