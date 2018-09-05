using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace PropertyManagement.Models
{
    public class ReportManager
    {
        public static DataTable  GetInterest(DateTime start, DateTime end, string unitedIDString, int drilldownlevel)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(" select distinct.tblProperty.PropertyID, tblProperty.LoanAmount, tblProperty.InterestRate,tblProperty.PurchaseDate,tblProperty.SoldDate ");
            sb.Append(" from tblProperty, tblPropertyUnit where tblProperty.PropertyID = tblPropertyUnit.PropertyID ");
            sb.Append(" AND tblPropertyUnit.UnitID in ( " + unitedIDString + ")");
            sb.Append(" AND tblProperty.InterestRate is not null and tblProperty.InterestRate > 0 ");
            sb.Append(" AND tblProperty.LoanAmount is not null and tblProperty.LoanAmount > 0 AND tblProperty.PurchaseDate is not null ");
            SqlConnection conn = new SqlConnection(Helpers .Helpers .GetAppConnectionString());
            SqlDataAdapter adapter = new SqlDataAdapter();
            adapter.SelectCommand = new SqlCommand(sb.ToString(), conn);


            DataTable myDataTable = new DataTable();
            myDataTable.Columns.Add(new DataColumn("CategoryName", typeof(string)));
            myDataTable.Columns.Add(new DataColumn("PropertyID", typeof(int)));
            myDataTable.Columns.Add(new DataColumn("Interest", typeof(double)));
            myDataTable.Columns.Add(new DataColumn("Year", typeof(int)));
            myDataTable.Columns.Add(new DataColumn("Quarter", typeof(int)));
            myDataTable.Columns.Add(new DataColumn("MonthNumber", typeof(int)));

            conn.Open();
            try
            {
                DataTable queryTable = new DataTable();
                adapter.Fill(queryTable);
                if (queryTable != null && queryTable.Rows.Count > 0)
                {
                    for (int i = 0; i < queryTable.Rows.Count; i++)
                    {
                        DataRow dr = queryTable.Rows[i];
                        if(drilldownlevel == (int)Helpers .Helpers .DrilldownLevel .Monthly )
                        {
                            int difference = (end.Month + end.Year * 12) - (start.Month + start.Year * 12);
                            for (int j = 0; j < difference; j++)
                            {
                                DataRow resultDR = myDataTable.NewRow();
                                resultDR["CategoryName"] = dr["Interest"];
                                resultDR["PropertyID"] = dr["PropertyID"];
                                resultDR["Year"] = start.Year;
                                resultDR["Month"] = start.Month;
                                resultDR["Interest"] = (double)(dr["InterestRate"]) * (double)(dr["LoanAmount"]) * 30 / 365;
                                myDataTable.Rows.Add(resultDR);
                                start = start.AddMonths(1);
                            }
                        }
                        else if (drilldownlevel == (int)Helpers.Helpers.DrilldownLevel.Quarterly )
                        {
                            int difference = (end.Month + end.Year * 4) - (start.Month + start.Year * 4);
                            for (int j = 0; j < difference; j++)
                            {
                                DataRow resultDR = myDataTable.NewRow();
                                resultDR["CategoryName"] = dr["Interest"];
                                resultDR["PropertyID"] = dr["PropertyID"];
                                resultDR["Year"] = start.Year;
                                resultDR["Month"] = start.Month;
                                resultDR["Interest"] = (double)(dr["InterestRate"]) * (double)(dr["LoanAmount"]) * 30 / 365;
                                myDataTable.Rows.Add(resultDR);
                                start = start.AddMonths(3);
                            }

                        }
                        else if (drilldownlevel == (int)Helpers.Helpers.DrilldownLevel.Yearly )
                        {
                            int difference = end.Year - start.Year ;
                            for (int j = 0; j < difference; j++)
                            {
                                DataRow resultDR = myDataTable.NewRow();
                                resultDR["CategoryName"] = dr["Interest"];
                                resultDR["PropertyID"] = dr["PropertyID"];
                                resultDR["Year"] = start.Year;
                                resultDR["Month"] = start.Month;
                                resultDR["Interest"] = (double)(dr["InterestRate"]) * (double)(dr["LoanAmount"]) * 30 / 365;
                                myDataTable.Rows.Add(resultDR);
                                start = start.AddYears (1);
                            }

                        }
                    }
                }
            }
            finally
            {
                conn.Close();
            }
            return myDataTable;
        }

        //private void GetSum()
        //{
        //    netIncome = totalRent - totalInterest + totalExpense;
        //    lblNetIncome.Text = netIncome.ToString("0.##");
        //}
        private void GetIncomestatementExpense(DateTime start, DateTime end, int drilldownlevel, string unitIDString)
        {
            StringBuilder sbExpense = new StringBuilder();
            sbExpense.Append("SELECT DATENAME(year, tblUnitOperation.FinishDate) as Year,");
            if (drilldownlevel == (int)Helpers .Helpers .DrilldownLevel .Quarterly)
            {
                sbExpense.Append("DATENAME(Quarter, tblUnitOperation.FinishDate) as Quarter, ");
            }
            else if (drilldownlevel == (int)Helpers.Helpers.DrilldownLevel.Monthly)
            {
                sbExpense.Append("DATENAME(Month, tblUnitOperation.FinishDate) as Month, datepart(mm, tblUnitOperation.FinishDate) as MonthNumber,");
            }
            sbExpense.Append("cExpenseCategory.CategoryName, cExpenseCategory.CategoryID, ");
            sbExpense.Append("SUM(tblUnitOperation.Amount) AS TotalPrice");
            sbExpense.Append(" FROM tblUnitOperation INNER JOIN cExpenseCategory ON tblUnitOperation.CategoryID = cExpenseCategory.CategoryID");
            sbExpense.Append(" WHERE UnitID in (" + unitIDString + ")");
            sbExpense.Append(" AND tblUnitOperation.FinishDate>= '" + start + "' and tblUnitOperation.FinishDate <= '" + end + "'");
            //excluse owner contribution, owner withdraw and mortgage, propery purchase, security deposit
            sbExpense.Append(" AND tblUnitOperation.CategoryID not in (22,23,26, 32,34)");
            if (drilldownlevel == (int)Helpers.Helpers.DrilldownLevel.Yearly )
            {
                sbExpense.Append(" GROUP BY cExpenseCategory.CategoryID,cExpenseCategory.CategoryName, DATENAME(year, tblUnitOperation.FinishDate)");
                sbExpense.Append(" ORDER BY cExpenseCategory.CategoryName, DATENAME(year, tblUnitOperation.FinishDate)");
            }
            else if (drilldownlevel == (int)Helpers.Helpers.DrilldownLevel.Quarterly )
            {
                sbExpense.Append(" GROUP BY cExpenseCategory.CategoryID,cExpenseCategory.CategoryName, DATENAME(year, tblUnitOperation.FinishDate), DATENAME(Quarter, tblUnitOperation.FinishDate)");
                sbExpense.Append(" ORDER BY cExpenseCategory.CategoryName, DATENAME(year, tblUnitOperation.FinishDate), DATENAME(Quarter, tblUnitOperation.FinishDate)");
            }
            else if (drilldownlevel == (int)Helpers.Helpers.DrilldownLevel.Monthly )
            {
                sbExpense.Append(" GROUP BY cExpenseCategory.CategoryID,cExpenseCategory.CategoryName, DATENAME(year, tblUnitOperation.FinishDate), DATENAME(Month, tblUnitOperation.FinishDate),datepart(mm, tblUnitOperation.FinishDate)");
                sbExpense.Append(" ORDER BY cExpenseCategory.CategoryName, DATENAME(year, tblUnitOperation.FinishDate), datepart(mm, tblUnitOperation.FinishDate)");
            }
            String ConnString = System.Configuration.ConfigurationManager.ConnectionStrings["SunriseConnectionString"].ConnectionString;
            SqlConnection conn = new SqlConnection(ConnString);
            SqlDataAdapter adapter = new SqlDataAdapter();
            adapter.SelectCommand = new SqlCommand(sbExpense.ToString(), conn);

            DataTable myDataTable = new DataTable();
            myDataTable.Columns.Add(new DataColumn("CategoryName", typeof(string)));
            myDataTable.Columns.Add(new DataColumn("TotalPrice", typeof(double)));
            myDataTable.Columns.Add(new DataColumn("Year", typeof(int)));
            myDataTable.Columns.Add(new DataColumn("Quarter", typeof(int)));
            myDataTable.Columns.Add(new DataColumn("MonthNumber", typeof(int)));

            conn.Open();
            try
            {
                adapter.Fill(myDataTable);
            }
            finally
            {
                conn.Close();
            }

            List<double> columnSum = new List<double>();
            DataTable pivotTable = new DataTable();
            pivotTable.Columns.Add(new DataColumn("CategoryName", typeof(string)));
            if (drilldownlevel== (int)Helpers .Helpers .DrilldownLevel .Quarterly )
            {
                int quarterSpan = (end.Year - start.Year) * 4 + (int)(end.Month / 3) - (int)(start.Month / 3) + 1;
                for (int quarter = 1; quarter <= quarterSpan; quarter++)
                {
                    pivotTable.Columns.Add(new DataColumn(quarter.ToString(), typeof(double)));
                    columnSum.Add(0);
                }
                string category = "";
                DataRow dr = null;
                for (int i = 0; i < myDataTable.Rows.Count; i++)
                {
                    string category1 = (string)myDataTable.Rows[i][0];
                    if (category1 != category)
                    {
                        dr = pivotTable.NewRow();
                        dr[0] = category1;
                        pivotTable.Rows.Add(dr);
                        category = category1;
                    }
                    String col = (((int)(myDataTable.Rows[i][2]) - start.Year) * 4 + (int)(myDataTable.Rows[i][3])).ToString();
                    dr[col] = myDataTable.Rows[i][1];
                }
            }
            else if (drilldownlevel == (int)Helpers.Helpers.DrilldownLevel.Monthly )
            {
                int monthSpan = (end.Year - start.Year) * 12 + end.Month - start.Month + 1;
                for (int month = 1; month <= monthSpan; month++)
                {
                    pivotTable.Columns.Add(new DataColumn(month.ToString(), typeof(double)));
                    columnSum.Add(0);
                }
                string category = "";
                DataRow dr = null;
                for (int i = 0; i < myDataTable.Rows.Count; i++)
                {
                    string category1 = (string)myDataTable.Rows[i][0];
                    if (category1 != category)
                    {
                        dr = pivotTable.NewRow();
                        dr[0] = category1;
                        pivotTable.Rows.Add(dr);
                        category = category1;
                    }
                    DateTime temp = new DateTime((int)(myDataTable.Rows[i][2]), (Int32)(myDataTable.Rows[i][4]), 1);
                    String col = ((temp.Year - start.Year) * 12 + temp.Month - start.Month + 1).ToString();
                    dr[col] = myDataTable.Rows[i][1];
                }
            }
            else
            {
                for (int year = start.Year; year <= end.Year; year++)
                {
                    pivotTable.Columns.Add(new DataColumn(year.ToString(), typeof(double)));
                    columnSum.Add(0);
                }
                string category = "";
                DataRow dr = null;
                for (int i = 0; i < myDataTable.Rows.Count; i++)
                {
                    string category1 = (string)myDataTable.Rows[i][0];
                    if (category1 != category)
                    {
                        dr = pivotTable.NewRow();
                        dr[0] = category1;
                        pivotTable.Rows.Add(dr);
                        category = category1;
                    }
                    String col = (myDataTable.Rows[i][2]).ToString();
                    dr[col] = myDataTable.Rows[i][1];
                }
            }

            RadGrid1.DataSource = pivotTable;

        }

        private void GetIncomestatementIncome(DateTime start, DateTime end, int drilldownlevel)
        {
            if (drdPeriod.SelectedValue.Equals("1"))
            {
                SqlDataSource1.SelectCommand = "select year,  sum(PaidAmount) as rent from vw_rent "
                       + "where vw_rent.PaymentDate >'" + start + "' and vw_rent.PaymentDate<'" + end
                       + "' And UnitID in ( " + getUnitIdString() + ")"
                       + " group by year Order by year";
            }
            else if (drdPeriod.SelectedValue.Equals("2"))
            {
                SqlDataSource1.SelectCommand = "select year, quarter,  sum(PaidAmount) as rent from vw_rent "
                       + "where vw_rent.PaymentDate >'" + start + "' and vw_rent.PaymentDate<'" + end
                       + "' And UnitID in ( " + getUnitIdString() + ")"
                       + " group by year, quarter Order by year, quarter";
            }
            else if (drdPeriod.SelectedValue.Equals("3"))
            {
                SqlDataSource1.SelectCommand = "select year, month,  sum(PaidAmount) as rent from vw_rent "
                       + "where vw_rent.PaymentDate >'" + start + "' and vw_rent.PaymentDate<'" + end
                       + "' And UnitID in ( " + getUnitIdString() + ")"
                       + " group by year, month, MonthNumber Order by year, MonthNumber";
            }
            System.Web.UI.DataSourceSelectArguments args = new System.Web.UI.DataSourceSelectArguments();
            DataView view = (DataView)SqlDataSource1.Select(args);

            rgRent.DataSource = SqlDataSource1;
            rgRent.DataBind();

        }
    }
}