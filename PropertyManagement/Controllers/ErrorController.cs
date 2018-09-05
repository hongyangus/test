using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PropertyManagement.Controllers
{
    public class ErrorController : Controller
    {
        [AllowAnonymous]
        public ActionResult Index()
        {
            BaseController bc = new BaseController();
            bc.LogException("not found error");
            Response.StatusCode = 404;
            return View();
        }
        //
        // GET: /Error/
        [AllowAnonymous]
        public ActionResult NotFound()
        {
            BaseController bc = new BaseController();
            bc.LogException("not found error");
            Response.StatusCode = 404;
            return View();
        }

        // GET: Error/Error
        [AllowAnonymous]
        public ActionResult Error()
        {
            //in the global.asax.cs code we handle the error. maybe we can send it to an email.

            BaseController bc = new BaseController();
            bc.LogException("error");
            //return a status code for proper seo
            Response.StatusCode = 500;

            return View();
        }
        // GET: Error/Error
        [AllowAnonymous]
        public ActionResult ServerError()
        {
            //in the global.asax.cs code we handle the error. maybe we can send it to an email.

            //return a status code for proper seo
            BaseController bc = new BaseController();
            ViewBag.Exception = "error";
            
            bc.LogException("server error");
            Response.StatusCode = 500;

            return View();
        }

    }
}
