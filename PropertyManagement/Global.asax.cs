using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Optimization;

namespace PartialViewDemo
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);

            // TJO - Add a filter to display the website maintenance page if DB flag is set.
            // Note: See the BaseContoller code for class to handle DB check and view override.
            GlobalFilters.Filters.Add(new PropertyManagement.Controllers.OfflineActionFilter());
            //

            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            
        }
        protected void Session_Start(object sender, EventArgs e)
        {
        }
        protected void Session_Start()
        {
            Session.Timeout = 240;
        }
    }
}