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
using PropertyManagement.ViewModels.User;
using System.Data;
using System.Text;
using PropertyManagement.Helpers;

namespace PropertyManagement.Controllers
{
    [Authorize]
    public class CompanyController : BaseController
    {
        //
        // GET: /ManageUser/

        private string reporttitle = "Manage Company";

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
        public PartialViewResult ReportView(string[] companyIDs)
        {
            return PartialView("ReportView", CompanyManager .GetByIDs (companyIDs));
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Add()
        {
            // add new user
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Create new company";

            AddCompanyVM model = new AddCompanyVM();
            model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);
            model.AllUser = GetSelectListItems((short)Helpers.Helpers.ListType.allUser);
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Add(AddCompanyVM model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Add New Company";

            //var selectedRoles = model.Roles.Where(x => x.IsChecked).Select(x => x.ID).ToList();
            //var selectedCompanies = model.Companies.Where(x => x.IsChecked).Select(x => x.ID).ToList();
            CompanyManager.Add(model);
            return RedirectToAction("Index");
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Edit(int id)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Edit Company";

            var user = CompanyManager.GetByID(id);
            var model = new EditCompanyVM()
            {
                CompanyID = user.CompanyID,
                CompanyName = user.CompanyName,
                StartDate = user.StartDate,
                CompanyCellPhone = user.CompanyCellPhone,
                CompanyPhone = user.CompanyPhone,
                Address = user.Address,
                City = user.City,
                State = user.State,
                Zip = user.Zip,
                EIN = user.EIN,
                EmailAddress = user.EmailAddress,
                BankAccount = user.BankAccount,
                RountingNo = user.RountingNo,
                Status = user.Status,
                StatusID = user.StatusID,
                AdminID = user.AdminID 
            };
           
            model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);
            model.AllUser = GetSelectListItems((short)Helpers.Helpers.ListType.allUser);

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Edit(EditCompanyVM model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Edit User";
            
            CompanyManager.Edit(model);
            return RedirectToAction("Index");
        }


    }
    
}

