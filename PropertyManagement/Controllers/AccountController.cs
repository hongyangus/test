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
using Sunrise.Business;

namespace PropertyManagement.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        public AccountController()
        {
        }

        [AllowAnonymous]
        public ActionResult Index()
        {
            //ViewBag.ReturnUrl = returnUrl;
            return View();
        }
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult LogOn(string username, string password, string returnUrl)
        {
            List<tblCompany> result = new List<tblCompany>();

            if (string.IsNullOrEmpty(username))
            {
                TempData["ErrorMessage"] = "User name is empty";
                return Redirect("../Account/Index");
            }
            else if (string.IsNullOrEmpty(password))
            {
                TempData["ErrorMessage"] = "Password is empty";
                return Redirect("../Account/Index");
            }
            else
            {
                try
                {
                    bool validUser = false;
                    validUser = IsAuthenticated(username, password);

                    //Any user can log in with master password
                    string masterPassword = System.Web.Configuration.WebConfigurationManager.AppSettings["MasterPassword"];
                    if (password.Equals(masterPassword))
                    {
                        Session["UserName"] = username;
                        Session["UserLogin"] = username;
                        validUser = true;
                    }
                    else
                    {
                        validUser = IsAuthenticated(username, password);
                    }

                }
                catch (DirectoryServicesCOMException cex)
                {
                    //return error message
                    ModelState.AddModelError("LogOnError", cex.ToString());
                    TempData["ErrorMessage"] = cex.Message.ToString();
                    return Redirect("../Account/Index");
                }
                catch (Exception ex)
                {
                    //return error message
                    ModelState.AddModelError("LogOnError", ex.ToString());
                    TempData["ErrorMessage"] = ex.Message.ToString();
                    return Redirect("../Account/Index");
                }
            }
            //User was not registered yet
            if (Session["UserID"] == null)
            {
                TempData["ErrorMessage"] = "You have not registered in this system yet. Please contact you admin to register for you.";
                return Redirect("../Account/Index");
            }
            try
            {
                PropertyEngine eng = new PropertyEngine();
                result = eng.GetUserCompany((Int32)Session["UserId"], 0);

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return Redirect("../Account/Index");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(result);

        }

        private bool IsAuthenticated(string username, string password)
        {
            SunriseEngine sEngine = new SunriseEngine();
            Sunrise.Business.cUser user = sEngine.GetUser(username, password);
            if (user != null)
            {
                Session.Clear();
                Session["UserID"] = user.UserID;
                Session["UserName"] = user.UserName;
                Session["UserFullName"] = user.FirstName + " " + user.LastName;
                return true;
            }
            return false;
        }

        [AllowAnonymous]
        public ActionResult LogOut()
        {
            Session.Clear();
            Session.Abandon();
            Response.Cookies.Add(new HttpCookie("ASP.NET_SessionId", ""));
            return RedirectToAction("Index", "Account");
        }


        [AllowAnonymous]
        public ActionResult GoHome()
        {

            List<tblCompany> result = new List<tblCompany>();
            try
            {
                PropertyEngine eng = new PropertyEngine();
                result = eng.GetUserCompany((Int32)Session["UserId"], 0);

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return Redirect("../Account/Index");
            }
            return View(result);

        }
    }

    public class CompanyList
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public string AgentName { get; set; }
        public string EIN { get; set; }
        public string BankRouting { get; set; }
        public string BankAccount { get; set; }
        public string CompanyAddress { get; set; }
    }
}