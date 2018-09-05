using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PropertyManagement.Models
{
    public class Report
    {
        public Report()
        {
        }
        public int ReportID { get; set; }
        public IEnumerable<SelectListItem> AllReports { get; set; }
    }
}