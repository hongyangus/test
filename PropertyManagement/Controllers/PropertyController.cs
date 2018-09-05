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
    public class PropertyController : BaseController
    {
        //
        // GET: /ManageUser/

        private string reporttitle = "Manage Property";

        [AllowAnonymous]
        public ActionResult Index()
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = reporttitle;

            var companies = GetList((short)Helpers.Helpers.ListType.company);
            ViewBag.companies = new MultiSelectList(companies, "id", "description");
            return View();
        }

        [AllowAnonymous]
        public PartialViewResult ReportView(string[] companyIDs )
        {
            TempData["companyIDs"] = companyIDs;
            return PartialView("ReportView", PropertyManager.GetByCompanyIDs(companyIDs, ((int)Session["UserID"])));
        }

        [AllowAnonymous]
        public PartialViewResult DetailTableView(string tableid, string propertyID)
        {
            // Define the base SQL query 
            string SQLString = "SELECT tblPropertyUnit.UnitID, UnitName, tblPropertyUnit.PropertyID, tblPropertyUnit.Notes FROM tblPropertyUnit, mCompanyProperty WHERE mCompanyProperty.PropertyID = tblPropertyUnit.PropertyID and tblPropertyUnit.PropertyID IN (" + propertyID + ")";
            
            // Add the group by clause
            SQLString += " ORDER BY tblPropertyUnit.UnitName";

            // Create a list of our result class to hold the data from the query
            // Please ensure you instatiate the class for this controller and not a different controller
            List<Unit> result = new List<Unit>();

            // Execute the SQL query and get the results
            using (SqlConnection connection = new SqlConnection(Helpers .Helpers .GetAppConnectionString()))
            {
                SqlCommand command = new SqlCommand(SQLString, connection); // Create the Command and Parameter objects
                // Open the connection in a try/catch block 
                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read()) // Read each result row and extract the data
                    {
                        Unit row = new Unit();
                        row.UnitID = Int32.Parse (reader[0].ToString());
                        row.UnitName = reader[1].ToString();
                        row.PropertyID = Int32.Parse(reader[2].ToString());
                        row.Note = reader[3].ToString();
                        result.Add(row);
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    string exms = ex.Message;
                }
            }
            ViewBag.tableid = tableid;
            return PartialView("DetailTableView", result);
        }



        [HttpGet]
        [AllowAnonymous]
        public ActionResult Add()
        {
            // add new user
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Add new Property";

            AddPropertyVM model = new AddPropertyVM();
            model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);
            model.AllCompany = GetSelectListItems((short)Helpers.Helpers.ListType.company);
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Add(AddPropertyVM model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Add New Property";

            //var selectedRoles = model.Roles.Where(x => x.IsChecked).Select(x => x.ID).ToList();
            //var selectedCompanies = model.Companies.Where(x => x.IsChecked).Select(x => x.ID).ToList();
            PropertyManager.Add(model);
            return RedirectToAction("Index");
        }


        [HttpGet]
        [AllowAnonymous]
        public ActionResult AddUnit()
        {
            // add new user
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Add new unit";

            AddPropertyVM model = new AddPropertyVM();
            model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);
            model.AllProperty = GetSelectListItems((short)Helpers.Helpers.ListType.property);
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult AddUnit(AddPropertyVM model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Add New unit";

            //var selectedRoles = model.Roles.Where(x => x.IsChecked).Select(x => x.ID).ToList();
            //var selectedCompanies = model.Companies.Where(x => x.IsChecked).Select(x => x.ID).ToList();
            PropertyManager.AddUnit(model);
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public ActionResult Edit(int id)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Edit Property";

            var property = PropertyManager.GetByID(id);
            var model = new EditPropertyVM()
            {
                PropertyID = property.PropertyID,
                PurchaseDate = property.PurchaseDate,
                Address = property.Address,
                City = property.City,
                Zip = property.Zip,
                PurchasePrice = property.PurchasePrice,
                PropertyTaxYearPayment = property.PropertyTaxYearPayment,
                PropertyTaxMailingAddress = property.PropertyTaxMailingAddress,
                PropertyTaxDueDate = property.PropertyTaxDueDate,
                StatusID = property.StatusID,
                note = property.note,
                InterestRate = property.InterestRate,
                LoanAmount = property.LoanAmount,
                InsuranceCompany = property.InsuranceCompany,
                InsurancePremium = property.InsurancePremium,
                InsurancePolicyNumber = property.InsurancePolicyNumber,
                InsuranceDueDate = property.InsuranceDueDate,
                InsuranceBillMailingAddress = property.InsuranceBillMailingAddress,
                SoldDate = property.SoldDate,
                amortization = property.amortization,
                CurrentEstimateMarketValue = property.CurrentEstimateMarketValue,
                ShareHoldPercentage = property.ShareHoldPercentage,
                CompanyID = property.CompanyID
        };

            model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);
            model.AllCompany  = GetSelectListItems((short)Helpers.Helpers.ListType.company);

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Edit(EditPropertyVM model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Edit Property";

            PropertyManager.Edit(model);
            return RedirectToAction("Index");
        }


        [AllowAnonymous]
        public ActionResult DeleteUnit(int id)
        {
            PropertyManager.DeleteUnit(id);
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public ActionResult EditUnit(int id)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Edit Unit";

            var unit = PropertyManager.GetUnitByID(id);

            return View(unit);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult EditUnit(Unit model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Edit User";

            PropertyManager.EditUnit(model);
            return RedirectToAction("Index");
        }

    }

}

