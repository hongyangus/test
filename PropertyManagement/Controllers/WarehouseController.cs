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
    public class WarehouseController : BaseController
    {
        //
        // GET: /Warehouse/

        [AllowAnonymous]
        public ActionResult Index()
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }

            return View();
        }

    }
}
