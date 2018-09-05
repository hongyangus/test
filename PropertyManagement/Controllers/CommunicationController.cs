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
    public class CommunicationController : BaseController
    {
        private string reporttitle = "Communication"; // Specify the report title here

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

            var statusList = GetList((short)Helpers.Helpers.ListType.allStatus);
            ViewBag.statusList = new MultiSelectList(statusList, "id", "description");

            var contractors = GetList((short)Helpers.Helpers.ListType.allTenant);
            ViewBag.Users = new MultiSelectList(contractors, "id", "description");

            var templates = GetList((short)Helpers.Helpers.ListType.allTemplate);
            ViewBag.Templates = new MultiSelectList(templates, "id", "description");

            ViewBag.rentMonth = DateTime.Now;


            //setup default value of the start date and end date
            if (Session["startDate"] == null)
            {
                DateTime oneMonth = DateTime.Now.AddMonths(-1);
                Session["startDate"] = new DateTime(oneMonth.Year, oneMonth.Month, 1).ToString("MM/dd/yyyy");
                Session["endDate"] = DateTime.Now.ToString("MM/dd/yyyy");
                string[] statusIDs = new string[] { ((int)Helpers.Helpers.StatusType.Open).ToString() };
                Session["selectedStatusIDs"] = statusIDs;
            }

            return View();
        }

    }
}
