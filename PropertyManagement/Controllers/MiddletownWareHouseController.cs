using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Configuration;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;

namespace PropertyManagement.Controllers
{
    [Authorize]
    public class MiddletownWareHouseController :  BaseController
    {
        private string reporttitle = "Exams Performed Heatmap Chart"; // Specify the report title here

        [AllowAnonymous]
        public ActionResult Index()
        {
            // Save the report title to the ViewBag
            ViewBag.ReportTitle = reporttitle;

            // Get the modalities from the DB for the drop-down listbox
            var modalities = GetDropdownChartType();
            ViewBag.Modalities = new MultiSelectList(modalities, "id", "description");

            // Setup default value of the start date and end date
            string startDate = string.Empty;
            string endDate = string.Empty;

            return View();
        }

        [AllowAnonymous]
        public PartialViewResult ChartView(string startDate, string endDate, string[] modalityID)
        {
            // Manage the datetimes            
            DateTime start_date = DateTime.Today.AddDays(-1).AddHours(7); // Default start datetime
            DateTime end_date = DateTime.Today.AddHours(7); // Default end datetime
            if (!String.IsNullOrEmpty(startDate))
            {
                start_date = Convert.ToDateTime(startDate).AddHours(7); // Start of day is 7am
            }
            if (!String.IsNullOrEmpty(endDate))
            {
                end_date = Convert.ToDateTime(endDate).AddHours(7); // Start of day is 7am
            }
            ViewBag.startDate = startDate;
            ViewBag.endDate = endDate;

         

            // Define the base SQL query 
            string SQLString = "SELECT DOW, HOD, nDays, " + "" + " FROM ("
                               + " Select CONVERT(date, xf.end_exam_dt) 'Date', xf.end_exam_dow as 'DOW', xf.end_exam_hour 'HOD', DATEDIFF(day,'" + start_date + "','" + end_date + "') 'nDays', count(*) 'daytotal'"
                               + " FROM rad_exam_facts as xf"
                               + " INNER JOIN rad_exams as x on x.id = xf.rad_exam_id"
                               + " INNER JOIN modalities as m on m.id = xf.modality_id"
                               + " INNER JOIN rad_reports as r ON r.rad_exam_id = xf.rad_exam_id"
                               + " INNER JOIN sites as s on s.id = x.site_id"
                               + " WHERE xf.end_exam_dt >= '" + start_date + "' AND xf.end_exam_dt <= '" + end_date + "'"
                               + " AND s.site NOT IN ('SHR','UC') AND xf.site_location_id NOT IN (13)";    // Remove UC and SHR, and also Outside Reads
            // Add modality id to the where clause if appropriate
            string modality_title = " (All modalities)";
            if (modalityID != null && modalityID.Count() > 0 && !string.IsNullOrEmpty(modalityID[0]))
            {
                SQLString += " AND m.id in (" + String.Join(",", modalityID) + ")";
                modality_title = " (" + String.Join(",", "") + ")";
            }
          

            // Close the sub-query
            SQLString += " GROUP BY CONVERT(date, xf.end_exam_dt), xf.end_exam_dow,xf.end_exam_hour ) qry1";
            // Add the group by clause
            SQLString += " GROUP BY DOW, HOD, nDays";
            // Add the order by clause
            SQLString += " ORDER BY DOW, HOD, nDays";

            // Create a list of our result class to hold the data from the query
            // Please ensure you instatiate the class for this controller and not a different controller
            List<HeatmapExamsPerformedChartResult> result = new List<HeatmapExamsPerformedChartResult>();

            // Execute the SQL query and get the results
            //using (SqlConnection connection = new SqlConnection(Helpers .Helpers .GetAppConnectionString()))
            //{
            //    SqlCommand command = new SqlCommand(SQLString, connection); // Create the Command and Parameter objects
            //    // Open the connection in a try/catch block 
            //    try
            //    {
            //        connection.Open();
            //        SqlDataReader reader = command.ExecuteReader();
            //        while (reader.Read()) // Read each result row and extract the data
            //        {
            //            HeatmapExamsPerformedChartResult row = new HeatmapExamsPerformedChartResult();
            //            row.DOW = reader[0].ToString();
            //            row.HOD = reader[1].ToString();
            //            row.ExamCount = reader[3].ToString();
            //            result.Add(row);
            //        }
            //        reader.Close();
            //    }
            //    catch (Exception ex)
            //    {
            //        string exms = ex.Message;
            //    }
            //}

            // Build arrays for each day of the week by hour
            decimal[] sundayarray = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            decimal[] mondayarray = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            decimal[] tuesdayarray = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            decimal[] wednesdayarray = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            decimal[] thursdayarray = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            decimal[] fridayarray = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            decimal[] saturdayarray = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            // Fill in the arrays with the data from our query (day 1=monday, day 7=Sunday)
            //foreach (HeatmapExamsPerformedChartResult row in result)
            //{
            //    int dow = Int32.Parse(row.DOW);
            //    int hod = Int32.Parse(row.HOD);
            //    decimal examcount = decimal.Parse(row.ExamCount);

            //    switch (dow)
            //    {
            //        case 1:
            //            mondayarray[hod] = examcount;
            //            break;
            //        case 2:
            //            tuesdayarray[hod] = examcount;
            //            break;
            //        case 3:
            //            wednesdayarray[hod] = examcount;
            //            break;
            //        case 4:
            //            thursdayarray[hod] = examcount;
            //            break;
            //        case 5:
            //            fridayarray[hod] = examcount;
            //            break;
            //        case 6:
            //            saturdayarray[hod] = examcount;
            //            break;
            //        case 7:
            //            sundayarray[hod] = examcount;
            //            break;
            //    }
            //} // loop through results

            // Data to return to chart view for chart processing
            ViewData["sundayarray"] = sundayarray;
            ViewData["mondayarray"] = mondayarray;
            ViewData["tuesdayarray"] = tuesdayarray;
            ViewData["wednesdayarray"] = wednesdayarray;
            ViewData["thursdayarray"] = thursdayarray;
            ViewData["fridayarray"] = fridayarray;
            ViewData["saturdayarray"] = saturdayarray;

            List<HeatmapExamsPerformedReportResult> ReportResult = new List<HeatmapExamsPerformedReportResult>();
            HeatmapExamsPerformedReportResult trow1 = new HeatmapExamsPerformedReportResult();
            trow1.DayOfWeek = "Sunday";
            trow1.midnight = sundayarray[0]; trow1.am1 = sundayarray[1]; trow1.am2 = sundayarray[2]; trow1.am3 = sundayarray[3]; trow1.am4 = sundayarray[4]; trow1.am5 = sundayarray[5]; trow1.am6 = sundayarray[6];
            trow1.am7 = sundayarray[7]; trow1.am8 = sundayarray[8]; trow1.am9 = sundayarray[9]; trow1.am10 = sundayarray[10]; trow1.am11 = sundayarray[11]; trow1.noon = sundayarray[12];
            trow1.pm1 = sundayarray[13]; trow1.pm2 = sundayarray[14]; trow1.pm3 = sundayarray[15]; trow1.pm4 = sundayarray[16]; trow1.pm5 = sundayarray[17]; trow1.pm6 = sundayarray[18];
            trow1.pm7 = sundayarray[19]; trow1.pm8 = sundayarray[20]; trow1.pm9 = sundayarray[21]; trow1.pm10 = sundayarray[22]; trow1.pm11 = sundayarray[23];
            ReportResult.Add(trow1);
            HeatmapExamsPerformedReportResult trow2 = new HeatmapExamsPerformedReportResult();
            trow2.DayOfWeek = "Monday";
            trow2.midnight = mondayarray[0]; trow2.am1 = mondayarray[1]; trow2.am2 = mondayarray[2]; trow2.am3 = mondayarray[3]; trow2.am4 = mondayarray[4]; trow2.am5 = mondayarray[5]; trow2.am6 = mondayarray[6];
            trow2.am7 = mondayarray[7]; trow2.am8 = mondayarray[8]; trow2.am9 = mondayarray[9]; trow2.am10 = mondayarray[10]; trow2.am11 = mondayarray[11]; trow2.noon = mondayarray[12];
            trow2.pm1 = mondayarray[13]; trow2.pm2 = mondayarray[14]; trow2.pm3 = mondayarray[15]; trow2.pm4 = mondayarray[16]; trow2.pm5 = mondayarray[17]; trow2.pm6 = mondayarray[18];
            trow2.pm7 = mondayarray[19]; trow2.pm8 = mondayarray[20]; trow2.pm9 = mondayarray[21]; trow2.pm10 = mondayarray[22]; trow2.pm11 = mondayarray[23];
            ReportResult.Add(trow2);
            HeatmapExamsPerformedReportResult trow3 = new HeatmapExamsPerformedReportResult();
            trow3.DayOfWeek = "Tuesday";
            trow3.midnight = tuesdayarray[0]; trow3.am1 = tuesdayarray[1]; trow3.am2 = tuesdayarray[2]; trow3.am3 = tuesdayarray[3]; trow3.am4 = tuesdayarray[4]; trow3.am5 = tuesdayarray[5]; trow3.am6 = tuesdayarray[6];
            trow3.am7 = tuesdayarray[7]; trow3.am8 = tuesdayarray[8]; trow3.am9 = tuesdayarray[9]; trow3.am10 = tuesdayarray[10]; trow3.am11 = tuesdayarray[11]; trow3.noon = tuesdayarray[12];
            trow3.pm1 = tuesdayarray[13]; trow3.pm2 = tuesdayarray[14]; trow3.pm3 = tuesdayarray[15]; trow3.pm4 = tuesdayarray[16]; trow3.pm5 = tuesdayarray[17]; trow3.pm6 = tuesdayarray[18];
            trow3.pm7 = tuesdayarray[19]; trow3.pm8 = tuesdayarray[20]; trow3.pm9 = tuesdayarray[21]; trow3.pm10 = tuesdayarray[22]; trow3.pm11 = tuesdayarray[23];
            ReportResult.Add(trow3);
            HeatmapExamsPerformedReportResult trow4 = new HeatmapExamsPerformedReportResult();
            trow4.DayOfWeek = "Wednesday";
            trow4.midnight = wednesdayarray[0]; trow4.am1 = wednesdayarray[1]; trow4.am2 = wednesdayarray[2]; trow4.am3 = wednesdayarray[3]; trow4.am4 = wednesdayarray[4]; trow4.am5 = wednesdayarray[5]; trow4.am6 = wednesdayarray[6];
            trow4.am7 = wednesdayarray[7]; trow4.am8 = wednesdayarray[8]; trow4.am9 = wednesdayarray[9]; trow4.am10 = wednesdayarray[10]; trow4.am11 = wednesdayarray[11]; trow4.noon = wednesdayarray[12];
            trow4.pm1 = wednesdayarray[13]; trow4.pm2 = wednesdayarray[14]; trow4.pm3 = wednesdayarray[15]; trow4.pm4 = wednesdayarray[16]; trow4.pm5 = wednesdayarray[17]; trow4.pm6 = wednesdayarray[18];
            trow4.pm7 = wednesdayarray[19]; trow4.pm8 = wednesdayarray[20]; trow4.pm9 = wednesdayarray[21]; trow4.pm10 = wednesdayarray[22]; trow4.pm11 = wednesdayarray[23];
            ReportResult.Add(trow4);
            HeatmapExamsPerformedReportResult trow5 = new HeatmapExamsPerformedReportResult();
            trow5.DayOfWeek = "Thursday";
            trow5.midnight = thursdayarray[0]; trow5.am1 = thursdayarray[1]; trow5.am2 = thursdayarray[2]; trow5.am3 = thursdayarray[3]; trow5.am4 = thursdayarray[4]; trow5.am5 = thursdayarray[5]; trow5.am6 = thursdayarray[6];
            trow5.am7 = thursdayarray[7]; trow5.am8 = thursdayarray[8]; trow5.am9 = thursdayarray[9]; trow5.am10 = thursdayarray[10]; trow5.am11 = thursdayarray[11]; trow5.noon = thursdayarray[12];
            trow5.pm1 = thursdayarray[13]; trow5.pm2 = thursdayarray[14]; trow5.pm3 = thursdayarray[15]; trow5.pm4 = thursdayarray[16]; trow5.pm5 = thursdayarray[17]; trow5.pm6 = thursdayarray[18];
            trow5.pm7 = thursdayarray[19]; trow5.pm8 = thursdayarray[20]; trow5.pm9 = thursdayarray[21]; trow5.pm10 = thursdayarray[22]; trow5.pm11 = thursdayarray[23];
            ReportResult.Add(trow5);
            HeatmapExamsPerformedReportResult trow6 = new HeatmapExamsPerformedReportResult();
            trow6.DayOfWeek = "Friday";
            trow6.midnight = fridayarray[0]; trow6.am1 = fridayarray[1]; trow6.am2 = fridayarray[2]; trow6.am3 = fridayarray[3]; trow6.am4 = fridayarray[4]; trow6.am5 = fridayarray[5]; trow6.am6 = fridayarray[6];
            trow6.am7 = fridayarray[7]; trow6.am8 = fridayarray[8]; trow6.am9 = fridayarray[9]; trow6.am10 = fridayarray[10]; trow6.am11 = fridayarray[11]; trow6.noon = fridayarray[12];
            trow6.pm1 = fridayarray[13]; trow6.pm2 = fridayarray[14]; trow6.pm3 = fridayarray[15]; trow6.pm4 = fridayarray[16]; trow6.pm5 = fridayarray[17]; trow6.pm6 = fridayarray[18];
            trow6.pm7 = fridayarray[19]; trow6.pm8 = fridayarray[20]; trow6.pm9 = fridayarray[21]; trow6.pm10 = fridayarray[22]; trow6.pm11 = fridayarray[23];
            ReportResult.Add(trow6);
            HeatmapExamsPerformedReportResult trow7 = new HeatmapExamsPerformedReportResult();
            trow7.DayOfWeek = "Saturday";
            trow7.midnight = saturdayarray[0]; trow7.am1 = saturdayarray[1]; trow7.am2 = saturdayarray[2]; trow7.am3 = saturdayarray[3]; trow7.am4 = saturdayarray[4]; trow7.am5 = saturdayarray[5]; trow7.am6 = saturdayarray[6];
            trow7.am7 = saturdayarray[7]; trow7.am8 = saturdayarray[8]; trow7.am9 = saturdayarray[9]; trow7.am10 = saturdayarray[10]; trow7.am11 = saturdayarray[11]; trow7.noon = saturdayarray[12];
            trow7.pm1 = saturdayarray[13]; trow7.pm2 = saturdayarray[14]; trow7.pm3 = saturdayarray[15]; trow7.pm4 = saturdayarray[16]; trow7.pm5 = saturdayarray[17]; trow7.pm6 = saturdayarray[18];
            trow7.pm7 = saturdayarray[19]; trow7.pm8 = saturdayarray[20]; trow7.pm9 = saturdayarray[21]; trow7.pm10 = saturdayarray[22]; trow7.pm11 = saturdayarray[23];
            ReportResult.Add(trow7);
            ReportResult.Add(trow7);
            ReportResult.Add(trow7);
            ReportResult.Add(trow7);
            ReportResult.Add(trow7);
            ReportResult.Add(trow7);
            ReportResult.Add(trow7);
            ReportResult.Add(trow7);
            ReportResult.Add(trow7);
            ReportResult.Add(trow7);
            ReportResult.Add(trow7);
            ReportResult.Add(trow7);
            ReportResult.Add(trow7);
            ReportResult.Add(trow7);
            ReportResult.Add(trow7);
            ReportResult.Add(trow7);
            ReportResult.Add(trow7);
            ReportResult.Add(trow7);
            ReportResult.Add(trow7);

            ViewBag.TableCaption = ViewBag.querytype + " " + reporttitle + modality_title + ": " + start_date.ToString("d") + " to " + end_date.ToString("d");
            return PartialView("ChartView", ReportResult);
        }
    }
    public class HeatmapExamsPerformedChartResult
    {
        public string DOW { get; set; }
        public string HOD { get; set; }
        public string ExamCount { get; set; }
    }
    public class HeatmapExamsPerformedReportResult
    {
        public string DayOfWeek { get; set; }
        public decimal? midnight { get; set; }
        public decimal? am1 { get; set; }
        public decimal? am2 { get; set; }
        public decimal? am3 { get; set; }
        public decimal? am4 { get; set; }
        public decimal? am5 { get; set; }
        public decimal? am6 { get; set; }
        public decimal? am7 { get; set; }
        public decimal? am8 { get; set; }
        public decimal? am9 { get; set; }
        public decimal? am10 { get; set; }
        public decimal? am11 { get; set; }
        public decimal? noon { get; set; }
        public decimal? pm1 { get; set; }
        public decimal? pm2 { get; set; }
        public decimal? pm3 { get; set; }
        public decimal? pm4 { get; set; }
        public decimal? pm5 { get; set; }
        public decimal? pm6 { get; set; }
        public decimal? pm7 { get; set; }
        public decimal? pm8 { get; set; }
        public decimal? pm9 { get; set; }
        public decimal? pm10 { get; set; }
        public decimal? pm11 { get; set; }
    }

}
