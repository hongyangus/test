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
    public class UserController : BaseController
    {
        //
        // GET: /ManageUser/

        private string reporttitle = "Manage User";

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
            return PartialView("ReportView", UserManager.GetByCompanyIDs(companyIDs, (int)Session["UserID"]));
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Add()
        {
            // add new user
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Create new user";

            AddUserVM model = new AddUserVM();
            model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);

            var allRoles = UserManager.GetAllRoles(UserManager.GetUserMostRightRole((int)Session["UserID"]));
            model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);
            var checkBoxListItems = new List<CheckBoxListItem>();
            foreach (var genre in allRoles)
            {
                checkBoxListItems.Add(new CheckBoxListItem()
                {
                    ID = genre.ID,
                    Display = genre.Display
                });
            }
            model.Roles = checkBoxListItems;

            var allReports = GetCheckBoxList((short)Helpers.Helpers.ListType.company);
            var reportcheckBoxListItems = new List<CheckBoxListItem>();
            foreach (var genre in allReports)
            {
                reportcheckBoxListItems.Add(new CheckBoxListItem()
                {
                    ID = genre.ID,
                    Display = genre.Display
                });
            }
            model.Companies = reportcheckBoxListItems;
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Add(AddUserVM model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Add User";

            var selectedRoles = model.Roles.Where(x => x.IsChecked).Select(x => x.ID).ToList();
            var selectedCompanies = model.Companies.Where(x => x.IsChecked).Select(x => x.ID).ToList();
            UserManager.Add(model, selectedRoles[0], selectedCompanies);
            return RedirectToAction("Index");
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Edit(int id)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Edit User";

            var user = UserManager.GetByID(id);
            var model = new EditUserVM ()
            {
                UserID = user.ID,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Password = user.Password,
                CellPhone = user.CellPhone,
                WorkPhone = user.WorkPhone,
                EmailAddress = user.EmailAddress,
                City = user.City,
                State = user.State,
                Zip  = user.Zip,
                SSN = user.SSN,
                WebUrl = user.WebUrl,
                StatusID = user.StatusID 
            };
            var userRoles = UserManager.GetAllRolesForUser(id);
            var allRoles = UserManager.GetAllRoles(UserManager.GetUserMostRightRole((int)Session["UserID"]));
            model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);
            var checkBoxListItems = new List<CheckBoxListItem>();
            foreach (var genre in allRoles)
            {
                checkBoxListItems.Add(new CheckBoxListItem()
                {
                    ID = genre.ID,
                    Display = genre.Display ,
                    IsChecked = userRoles.Where(x => x.ID == genre.ID).Any()
                });
            }
            model.Roles = checkBoxListItems;

            var userReports = UserManager.GetAllCompanyForUser(id);
            var allReports = GetCheckBoxList((short)Helpers.Helpers.ListType.company);
            var reportcheckBoxListItems = new List<CheckBoxListItem>();
            foreach (var genre in allReports)
            {
                reportcheckBoxListItems.Add(new CheckBoxListItem()
                {
                    ID = genre.ID,
                    Display = genre.Display,
                    IsChecked = userReports.Where(x => x.ID == genre.ID).Any()
                });
            }
            model.Companies  = reportcheckBoxListItems;
            return View(model);
        }


        [HttpPost]
        [AllowAnonymous]
        public ActionResult Edit(EditUserVM model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "Edit User";

            var selectRoles = model.Roles.Where(x => x.IsChecked).Select(x => x.ID).ToList();
            var selectReports = model.Companies .Where(x => x.IsChecked).Select(x => x.ID).ToList();
            UserManager.Edit(model , selectRoles, selectReports);
            return RedirectToAction("Index");
        }


        
    }
}
