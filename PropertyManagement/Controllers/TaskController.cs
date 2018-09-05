using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Configuration;
using System.Data.SqlClient;
using PropertyManagement.Models;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using CCHMC.Radiology.Library.Authentication;
using CCHMC.AD.Library.Extensions;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using PropertyManagement.ViewModels;
using PropertyManagement.ViewModels.Task;
using System.Data;
using System.Text;
using PropertyManagement.Helpers;

namespace PropertyManagement.Controllers
{
    [Authorize]
    public class TaskController : BaseController
    {
        private string reporttitle = "Task Management";

        [AllowAnonymous]
        public ActionResult Index()
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Task"); }
            ViewBag.ReportTitle = reporttitle;
            ViewBag.TableCaption = "Task";

            var contractors = GetList((short)Helpers.Helpers.ListType.allUser);
            ViewBag.contractors = new MultiSelectList(contractors, "id", "description");

            var statusList = GetList((short)Helpers.Helpers.ListType.allStatus);
            ViewBag.statusList = new MultiSelectList(statusList, "id", "description");

            return View();
        }

        [AllowAnonymous]
        public PartialViewResult ReportView(string[] statusIDs, string[] contractorIDs)
        {

            if (contractorIDs != null && contractorIDs.Count() > 0 && !string.IsNullOrEmpty(contractorIDs[0]))
            {
                if ((int)Session["UserID"] != 1)
                {
                    contractorIDs[0] = ((int)Session["UserID"]).ToString();
                }
            }
            Session["selectedStatusIDs"] = statusIDs;
            Session["selectedContractorIDs"] = contractorIDs;
            var allTask = TaskManager.GetAllActiveTaskForUser(statusIDs, contractorIDs);
            return PartialView("ReportView", allTask);
        }


        [AllowAnonymous]
        public ActionResult Close(int id)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Task"); }
            ViewBag.ReportTitle = reporttitle;

            TaskManager.CloseTask(id);
            return RedirectToAction("Index");
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Add()
        {
            // add new user
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Task"); }
            ViewBag.ReportTitle = "Add Task";
            AddTaskVM model = new AddTaskVM();
            model.StatusID = (int)Helpers.Helpers.StatusType.Open;
            model.ContractorID = (Int32)Session["UserID"];
            model.AllTenant = GetSelectListItems((short)Helpers.Helpers.ListType.allUser);
            model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);
            model.AllBankAccount = GetSelectListItems((short)Helpers.Helpers.ListType.bankaccount);
            model.AllUnit = GetSelectListItems((short)Helpers.Helpers.ListType.unit);
            model.ContractorID = (Int32)Session["UserID"];
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Add(AddTaskVM model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Task"); }
            ViewBag.ReportTitle = "Add Task";
            model.AdminID = Int32.Parse(Session["UserID"].ToString());
            TaskManager.AddTask(model);
            return RedirectToAction("Index");
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Edit(int id)
        {
            ViewBag.ReportTitle = reporttitle;
            AddTaskVM model = new AddTaskVM();
            model = TaskManager.GetTaskByID(id);
            model.AllTenant = GetSelectListItems((short)Helpers.Helpers.ListType.allUser);
            model.AllStatus = GetSelectListItems((short)Helpers.Helpers.ListType.allStatus);
            model.AllBankAccount = GetSelectListItems((short)Helpers.Helpers.ListType.bankaccount);
            model.AllUnit = GetSelectListItems((short)Helpers.Helpers.ListType.unit);
            return View(model);
        }


        [HttpPost]
        [AllowAnonymous]
        public ActionResult Edit(AddTaskVM model)
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Task"); }
            ViewBag.ReportTitle = "Edit Task";
            model.AdminID = Int32.Parse(Session["UserID"].ToString());

            TaskManager.EditTask(model);
            return RedirectToAction("Index");
        }
        
        [AllowAnonymous]
        public ActionResult ReportIndex()
        { // Enable security to redirect to login page if user is not logged in or we are not running in the VS IDE
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


            var ReportType = GetDropdownReportList();
            ViewBag.ReportType = new MultiSelectList(ReportType, "id", "description");

            return View();
        }
    }
}
