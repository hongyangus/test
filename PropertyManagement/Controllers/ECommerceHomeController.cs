using System;
using System.Web.Mvc;
using System.Collections.Generic;
using System.Data.SqlClient;
using PropertyManagement.Models;
using PropertyManagement.ViewModels.Property;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using System.Web;
using OfficeOpenXml;

namespace PropertyManagement.Controllers
{
    [Authorize]
    public class ECommerceHomeController : BaseController
    {
        //
        // GET: /ECommerceHome/

        private string reporttitle = "Manage Company";

        [AllowAnonymous]
        public ActionResult Index()
        {
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            ViewBag.ReportTitle = "ecommerce home";
            return View();
        }


        [AllowAnonymous]
        public ActionResult LoadShippingRate()
        {
            // Enable security to redirect to login page if user is not logged in or we are not running in the VS IDE
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            // Save the report title to the ViewBag
            ViewBag.ReportTitle = "Load shipping rate";

            var countryList = GetList((short)Helpers.Helpers.ListType.allECommerceCountry);
            ViewBag.countryList = new MultiSelectList(countryList, "id", "description");

            return View();
        }

        [AllowAnonymous]
        public ActionResult Upload(FormCollection formCollection)
        {
            if (Request != null)
            {
                HttpPostedFileBase file = Request.Files["UploadedFile"];
                if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
                {
                    string fileName = file.FileName;
                    string fileContentType = file.ContentType;
                    byte[] fileBytes = new byte[file.ContentLength];
                    var data = file.InputStream.Read(fileBytes, 0, Convert.ToInt32(file.ContentLength));
                    List<ShipRate> shipRateList = new List<ShipRate>();
                    List<string> nameList = new List<string>();
                    int countryID = Int32.Parse(formCollection["CountryID"]);
                    using (var package = new ExcelPackage(file.InputStream))
                    {
                        ExcelWorksheets currentSheet = package.Workbook.Worksheets;
                        for (int i = 1; i < currentSheet.Count+1; i++)
                        {
                            ExcelWorksheet workSheet = currentSheet[i];
                            var noOfCol = workSheet.Dimension.End.Column;
                            var noOfRow = workSheet.Dimension.End.Row;
                            string name = workSheet.Name;
                            string carrier = name.Split(' ')[0];
                            nameList.Add(name);

                            for (int rowIterator = 2; rowIterator <= noOfRow; rowIterator++)
                            {
                                var shipRate = new ShipRate();
                                shipRate.name = name;
                                shipRate.countryID = countryID;
                                shipRate.carrier = carrier;
                                shipRate.weight = (double)workSheet.Cells[rowIterator, 1].Value;
                                shipRate.zone1 = (double)workSheet.Cells[rowIterator, 2].Value;
                                shipRate.zone2 = (double)workSheet.Cells[rowIterator, 3].Value;
                                shipRate.zone3 = (double)workSheet.Cells[rowIterator, 4].Value;
                                shipRate.zone4 = (double)workSheet.Cells[rowIterator, 5].Value;
                                shipRate.zone5 = (double)workSheet.Cells[rowIterator, 6].Value;
                                shipRate.zone6 = (double)workSheet.Cells[rowIterator, 7].Value;
                                shipRate.zone7 = (double)workSheet.Cells[rowIterator, 8].Value;
                                shipRate.zone8 = (double)workSheet.Cells[rowIterator, 9].Value;
                                shipRate.zone9 = (double)workSheet.Cells[rowIterator, 10].Value;
                                shipRate.zone10 = (double)workSheet.Cells[rowIterator, 11].Value;
                                shipRate.zone11 = (double)workSheet.Cells[rowIterator, 12].Value;
                                shipRate.zone12 = (double)workSheet.Cells[rowIterator, 13].Value;
                                shipRate.zone13 = (double)workSheet.Cells[rowIterator, 14].Value;
                                shipRate.statusID = 1;
                                shipRateList.Add(shipRate);
                            }
                        }
                    }

                    MySqlConnection conn = new MySqlConnection(Helpers.Helpers.GetERPConnectionString());
                    try
                    {
                        conn.Open();
                        //loop through and disable old history data
                        string sql = "update c_shiprate set statusID = 3, updateDate='" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")
                            + "' where  name in('" + String.Join("','", nameList) + "')";
                        MySqlCommand cmd = new MySqlCommand(sql, conn);
                        MySqlDataReader rdr = cmd.ExecuteReader();
                        rdr.Close();
                        //loop through and insert new shipping rate
                        for (int i = 0; i < shipRateList.Count; i++)
                        {
                            ShipRate shipRate = shipRateList[i];
                            cmd.CommandText = "insert into c_shiprate (name,carrier, weight,zone1,zone2,zone3,zone4,zone5,zone6,zone7,zone8, zone9,zone10, zone11, zone12,zone13,statusID,countryID, updateDate)" +
                                "values ("
                                + "'" + shipRate.name + "',"
                                + "'" + shipRate.carrier + "',"
                                + "" + shipRate.weight + ","
                                + "" + shipRate.zone1 + ","
                                + "" + shipRate.zone2 + ","
                                + "" + shipRate.zone3 + ","
                                + "" + shipRate.zone4 + ","
                                + "" + shipRate.zone5 + ","
                                + "" + shipRate.zone6 + ","
                                + "" + shipRate.zone7 + ","
                                + "" + shipRate.zone8 + ","
                                + "" + shipRate.zone9 + ","
                                + "" + shipRate.zone10 + ","
                                + "" + shipRate.zone11 + ","
                                + "" + shipRate.zone12 + ","
                                + "" + shipRate.zone13 + ","
                                + "" + shipRate.statusID + ","
                                + "" + shipRate.countryID + ","
                                + "'" + DateTime .Now.ToString ("yyyy-MM-dd hh:mm:ss") + "')";
                            rdr = cmd.ExecuteReader();
                            rdr.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    conn.Close();
                    
                }
            }
            return View("Index");
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult LoadingShippingRate()
        {
            // Enable security to redirect to login page if user is not logged in or we are not running in the VS IDE
            if (Session["UserName"] == null) { return RedirectToAction("Index", "Account"); }
            if (Request.Files.Count > 0)
            {
                var file = Request.Files[0];

                if (file != null && file.ContentLength > 0)
                {
                    //var fileName = Path.GetFileName(file.FileName);
                    //var path = Path.Combine(Server.MapPath("~/Images/"), fileName);
                    //file.SaveAs(path);
                }
            }

            return View();
        }

    }

    public class ShipRate
    {
        public int shipRateID { get; set; }
        public string name { get; set; }
        public string carrier { get; set; }
        public double weight { get; set; }
        public double zone1 { get; set; }
        public double zone2 { get; set; }
        public double zone3 { get; set; }
        public double zone4 { get; set; }
        public double zone5 { get; set; }
        public double zone6 { get; set; }
        public double zone7 { get; set; }
        public double zone8 { get; set; }
        public double zone9 { get; set; }
        public double zone10 { get; set; }
        public double zone11 { get; set; }
        public double zone12 { get; set; }
        public double zone13 { get; set; }
        public int statusID { get; set; }
        public int countryID { get; set; }
    }
}
