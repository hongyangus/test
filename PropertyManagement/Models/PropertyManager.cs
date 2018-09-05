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
using PropertyManagement.ViewModels.Property;
using System.Data;
using System.Text;
using PropertyManagement.Helpers;

namespace PropertyManagement.Models
{
    public class PropertyManager
    {
        public static Property GetByID(int id)
        {
            string SQLString = "select tblProperty.*, cStatusType.*, mCompanyProperty.CompanyID from tblProperty ";
            SQLString += " LEFT OUTER JOIN cStatusType ON cStatusType.StatusTypeID= tblProperty.StatusID ";
            SQLString += " LEFT OUTER JOIN mCompanyProperty ON mCompanyProperty.PropertyID = tblProperty.PropertyID ";
            SQLString += " WHERE tblProperty.PropertyID =  " + id;
            using (SqlDataAdapter adapter = new SqlDataAdapter(SQLString, Helpers.Helpers.GetAppConnectionString()))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                if (tb != null && tb.Rows.Count > 0)
                {
                    DataRow dr = tb.Rows[0];
                    return FillInPropertyWithData(dr);
                }
                return null;
            }
        }

        public static List<Property> GetByCompanyIDs(string[] companyIDs, int userID)
        {
            string SQLString = "select tblProperty.*, cStatusType.*, mCompanyProperty.CompanyID from tblProperty ";
            SQLString += " LEFT OUTER JOIN cStatusType ON cStatusType.StatusTypeID= tblProperty.StatusID ";
            SQLString += " INNER JOIN mCompanyProperty on mCompanyProperty.PropertyID = tblProperty.PropertyID ";
            if (companyIDs != null && companyIDs.Count() > 0 && !string.IsNullOrEmpty(companyIDs[0]))
            {
                SQLString += " AND mCompanyProperty.CompanyID in  (" + String.Join(",", companyIDs) + ")";
            }
            else
            {
                //get the companys only the owner can access
                SQLString += " AND mCompanyProperty.CompanyID IN (" + Helpers.Helpers.GetUserManagedCompanyString(userID.ToString ()) +")";
            }

            using (SqlDataAdapter adapter = new SqlDataAdapter(SQLString, Helpers.Helpers.GetAppConnectionString()))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                List<Property> allProperty = new List<Property>();

                if (tb != null && tb.Rows.Count > 0)
                {
                    for (int i = 0; i <tb.Rows.Count; i++)
                    {
                        DataRow dr = tb.Rows[i];
                        allProperty.Add(FillInPropertyWithData(dr));
                    }
                }
                return allProperty;
            }
        }

        public static List<Property> SearchProperty(string startDate, string endDate, string[] companyIDs, string[] propertyIDs, string[] statusIDs, int userid)
        {
            StringBuilder sbOperation = new StringBuilder();
            StringBuilder whereClause = new StringBuilder();

            sbOperation.Append ( "select tblProperty.*, cStatusType.*, mCompanyProperty.CompanyID from tblProperty ");
            sbOperation.Append(" LEFT OUTER JOIN cStatusType ON cStatusType.StatusTypeID= tblProperty.StatusID ");
            sbOperation.Append(" INNER JOIN mCompanyProperty on mCompanyProperty.PropertyID = tblProperty.PropertyID ");
            sbOperation.Append(" INNER JOIN tblPropertyUnit on tblPropertyUnit.PropertyID = tblProperty.PropertyID ");
            sbOperation.Append(" INNER JOIN tblUnitOperation on tblUnitOperation.UnitID = tblPropertyUnit.UnitID ");

            if (!String.IsNullOrEmpty(startDate))
            {
                DateTime start = DateTime.Parse(startDate);
                whereClause.Append(" and [tblUnitOperation].FinishDate>='" + start.ToShortDateString() + "' ");
            }
            if (!String.IsNullOrEmpty(endDate))
            {
                DateTime end = DateTime.Parse(endDate);
                whereClause.Append(" and [tblUnitOperation].FinishDate<='" + end.ToShortDateString() + "'");
            }

            // Add modality id to the where clause if appropriate
            if (companyIDs != null && companyIDs.Count() > 0 && !string.IsNullOrEmpty(companyIDs[0]))
            {
                whereClause.Append(" AND mCompanyProperty.CompanyID IN (" + String.Join(",", companyIDs) + ")");
            }
            else
            {
                //get the companys only the owner can access
                whereClause.Append(" AND mCompanyProperty.CompanyID IN (" + Helpers.Helpers .GetUserManagedCompanyString(userid.ToString ()) + ")");
            }
            // Add modality id to the where clause if appropriate
            if (propertyIDs != null && propertyIDs.Count() > 0 && !string.IsNullOrEmpty(propertyIDs[0]))
            {
                whereClause.Append(" AND tblProperty.PropertyID IN (" + String.Join(",", propertyIDs) + ")");
            }
            // Add modality id to the where clause if appropriate
            if (statusIDs != null && statusIDs.Count() > 0 && !string.IsNullOrEmpty(statusIDs[0]))
            {
                whereClause.Append(" AND tblProperty.StatusID IN (" + String.Join(",", statusIDs) + ")");
            }

            sbOperation.Append(whereClause.Remove(0, 4).Insert(0, " where "));

            //sbOperation.Append(" Order by DueDate");


            using (SqlDataAdapter adapter = new SqlDataAdapter(sbOperation.ToString (), Helpers.Helpers.GetAppConnectionString()))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                List<Property> allProperty = new List<Property>();

                if (tb != null && tb.Rows.Count > 0)
                {
                    for (int i = 0; i < tb.Rows.Count; i++)
                    {
                        DataRow dr = tb.Rows[i];
                        allProperty.Add(FillInPropertyWithData(dr));
                    }
                }
                return allProperty;
            }
        }


        public static Unit GetUnitByID(int id)
        {
            string SQLString = "select * from tblPropertyUnit ";
            SQLString += " WHERE UnitID =  " + id;
            using (SqlDataAdapter adapter = new SqlDataAdapter(SQLString, Helpers.Helpers.GetAppConnectionString()))
            {
                DataSet ds = new DataSet();
                adapter.Fill(ds);

                DataTable tb = (DataTable)ds.Tables[0];
                if (tb != null && tb.Rows.Count > 0)
                {
                    DataRow dr = tb.Rows[0];
                    Unit unit = new Unit();
                    unit.UnitID = (int)dr["UnitID"];
                    unit.UnitName = dr["UnitName"].ToString ();
                    unit.Note = dr["Notes"].ToString ();
                    return unit;
                }
                return null;
            }
        }
        public static void AddUnit(AddPropertyVM model)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the user record.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();

                    //add Unit 0 by default
                    cmd.CommandText = "Insert into tblPropertyUnit (UnitName, PropertyID, Notes) values ('" + model.Address + "', " + model.PropertyID + ", '" + model.note + "');SELECT SCOPE_IDENTITY();";
                    int unitID = int.Parse(cmd.ExecuteScalar().ToString());
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public static void Add(AddPropertyVM model)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the user record.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();

                    SqlParameter IDParameter = new SqlParameter("@PropertyID", SqlDbType.Int);
                    IDParameter.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(IDParameter);
                    StringBuilder sb = new StringBuilder();
                    sb.Append("insert into tblProperty (");
                    sb.Append("PurchaseDate ,");
                    sb.Append("Address ,");
                    sb.Append("City ,");
                    sb.Append("Zip ,");
                    sb.Append("PurchasePrice ,");
                    sb.Append("PropertyTaxYearPayment ,");
                    sb.Append("PropertyTaxMailingAddress ,");
                    sb.Append("PropertyTaxDueDate ,");
                    sb.Append("StatusID ,");
                    sb.Append("note ,");
                    sb.Append("InterestRate ,");
                    sb.Append("LoanAmount ,");
                    sb.Append("InsuranceCompany ,");
                    sb.Append("InsurancePremium ,");
                    sb.Append("InsurancePolicyNumber ,");
                    sb.Append("InsuranceDueDate ,");
                    sb.Append("InsuranceBillMailingAddress ,");
                    sb.Append("SoldDate ,");
                    sb.Append("amortization ,");
                    sb.Append("CurrentEstimateMarketValue ,");
                    sb.Append("ShareHoldPercentage )");
                    sb.Append(" values (");
                    if (model.PurchaseDate != DateTime.MinValue)
                    {
                        sb.Append("'" + model.PurchaseDate + "',");
                    }
                    else
                    {
                        sb.Append("null,");
                    }
                    sb.Append("'" + model.Address + "',");
                    sb.Append("'" + model.City + "',");
                    sb.Append("'" + model.Zip + "',");
                    sb.Append(model.PurchasePrice + ",");
                    sb.Append(model.PropertyTaxYearPayment + ",");
                    sb.Append("'" + model.PropertyTaxMailingAddress + "',");
                    if(model.PropertyTaxDueDate != DateTime.MinValue )
                    { 
                    sb.Append("'" + model.PropertyTaxDueDate + "',");
                    }
                    else
                    {
                        sb.Append("null,");
                    }
                    sb.Append(model.StatusID + ",");
                    sb.Append("'" + model.note + "',");
                    sb.Append(model.InterestRate + ",");
                    sb.Append(model.LoanAmount + ",");
                    sb.Append("'" + model.InsuranceCompany + "',");
                    sb.Append(model.InsurancePremium + ",");
                    sb.Append("'" + model.InsurancePolicyNumber + "',");
                    if (model.InsuranceDueDate != DateTime.MinValue)
                    {
                        sb.Append("'" + model.InsuranceDueDate + "',");
                    }
                    else
                    {
                        sb.Append("null,");
                    }
                    sb.Append("'" + model.InsuranceBillMailingAddress + "',");
                    if (model.SoldDate != DateTime.MinValue)
                    {
                        sb.Append("'" + model.SoldDate + "',");
                    }
                    else
                    {
                        sb.Append("null,");
                    }
                    sb.Append(model.amortization + ",");
                    sb.Append(model.CurrentEstimateMarketValue + ",");
                    sb.Append(model.ShareHoldPercentage);
                    sb.Append(") SET @PropertyID=SCOPE_IDENTITY();");
                    cmd.CommandText = sb.ToString();
                    cmd.ExecuteNonQuery();
                    model.PropertyID = (int)IDParameter.Value;

                    //add Unit 0 by default
                    cmd.CommandText = "Insert into tblPropertyUnit (UnitName, PropertyID, Notes) values ('Unit 0', "+ model.PropertyID +", 'system auto generated')";
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    connection.Close();
                }

            }
            //insert company property
            InsertCompanyProperty(model.PropertyID  , model.CompanyID , model.PurchaseDate);
        }

        public static void Edit(EditPropertyVM model)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the user record.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append(" update tblProperty set ");
                    sb.Append(" PurchaseDate ='" + model.PurchaseDate + "',");
                    sb.Append(" Address ='" + model.Address + "',");
                    sb.Append(" City ='" + model.City + "',");
                    sb.Append(" Zip ='" + model.Zip + "',");
                    sb.Append(" PurchasePrice =" + model.PurchasePrice + ",");
                    sb.Append(" PropertyTaxYearPayment =" + model.PropertyTaxYearPayment + ",");
                    sb.Append(" PropertyTaxMailingAddress ='" + model.PropertyTaxMailingAddress + "',");
                    sb.Append(" PropertyTaxDueDate ='" + model.PropertyTaxDueDate + "',");
                    sb.Append(" StatusID =" + model.StatusID + ",");
                    sb.Append(" note ='" + model.note + "',");
                    sb.Append(" InterestRate =" + model.InterestRate + ",");
                    sb.Append(" LoanAmount =" + model.LoanAmount + ",");
                    sb.Append(" InsuranceCompany ='" + model.InsuranceCompany + "',");
                    sb.Append(" InsurancePremium =" + model.InsurancePremium + ",");
                    sb.Append(" InsurancePolicyNumber ='" + model.InsurancePolicyNumber + "',");
                    sb.Append(" InsuranceDueDate ='" + model.InsuranceDueDate + "',");
                    sb.Append(" InsuranceBillMailingAddress ='" + model.InsuranceBillMailingAddress + "',");
                    sb.Append(" SoldDate ='" + model.SoldDate + "',");
                    sb.Append(" amortization =" + model.amortization + ",");
                    sb.Append(" CurrentEstimateMarketValue =" + model.CurrentEstimateMarketValue + ",");
                    sb.Append(" ShareHoldPercentage =" + model.ShareHoldPercentage );
                    sb.Append(" where PropertyID = " + model.PropertyID);
                    cmd.CommandText = sb.ToString();
                    cmd.ExecuteNonQuery();

                    //update company id for property
                    sb.Clear();
                    sb.Append("update mCompanyProperty set CompanyID = " + model.CompanyID + " where propertyID=" + model.PropertyID);
                    cmd.CommandText = sb.ToString();
                    cmd.ExecuteNonQuery();

                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public static void InsertCompanyProperty(int propertyID, int companyID, DateTime startDate)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the user record.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append("insert into mCompanyProperty (");
                    sb.Append("PropertyID ,");
                    sb.Append("CompanyID ,");
                    sb.Append("StartDate )");
                    sb.Append("values(");
                    sb.Append(propertyID + ",");
                    sb.Append(companyID + ",");
                    sb.Append("'"+ startDate +"')" );
                    cmd.CommandText = sb.ToString();
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public static Property FillInPropertyWithData(DataRow dr)
        {
            Property property = new Property();
            if (dr["PropertyID"] != DBNull.Value)
            {
                property.PropertyID = int.Parse(dr["PropertyID"].ToString());
            }
            if (dr["PurchaseDate"] != DBNull.Value)
            {
                property.PurchaseDate = DateTime.Parse(dr["PurchaseDate"].ToString());
            }
            property.Address = dr["Address"].ToString();
            property.City = dr["City"].ToString();
            property.Zip = dr["Zip"].ToString();
            if (dr["PurchasePrice"] != DBNull.Value)
            {
                property.PurchasePrice = double.Parse(dr["PurchasePrice"].ToString());
            }
            if (dr["PropertyTaxYearPayment"] != DBNull.Value)
            {
                property.PropertyTaxYearPayment = double.Parse(dr["PropertyTaxYearPayment"].ToString());
            }
            property.PropertyTaxMailingAddress = dr["PropertyTaxMailingAddress"].ToString();
            if (dr["PropertyTaxDueDate"] != DBNull.Value)
            {
                property.PropertyTaxDueDate = DateTime.Parse(dr["PropertyTaxDueDate"].ToString());
            }
            if (dr["StatusID"] != DBNull.Value)
            {
                property.StatusID = int.Parse(dr["StatusID"].ToString());
            }
            if (dr["CompanyID"] != DBNull.Value)
            {
                property.CompanyID = int.Parse(dr["CompanyID"].ToString());
            }
            property.note = dr["note"].ToString();
            if (dr["InterestRate"] != DBNull.Value)
            {
                property.InterestRate = double.Parse(dr["InterestRate"].ToString());
            }
            if (dr["LoanAmount"] != DBNull.Value)
            {
                property.LoanAmount = double.Parse(dr["LoanAmount"].ToString());
            }
            property.InsuranceCompany = dr["InsuranceCompany"].ToString();
            if (dr["InsurancePremium"] != DBNull.Value)
            {
                property.InsurancePremium = double.Parse(dr["InsurancePremium"].ToString());
            }
            property.InsurancePolicyNumber = dr["InsurancePolicyNumber"].ToString();
            if (dr["InsuranceDueDate"] != DBNull.Value)
            {
                property.InsuranceDueDate = DateTime.Parse(dr["InsuranceDueDate"].ToString());
            }
            property.InsuranceBillMailingAddress = dr["InsuranceBillMailingAddress"].ToString();
            if (dr["SoldDate"] != DBNull.Value)
            {
                property.SoldDate = DateTime.Parse(dr["SoldDate"].ToString());
            }
            if (dr["amortization"] != DBNull.Value)
            {
                property.amortization = double.Parse(dr["amortization"].ToString());
            }
            if (dr["CurrentEstimateMarketValue"] != DBNull.Value)
            {
                property.CurrentEstimateMarketValue = double.Parse(dr["CurrentEstimateMarketValue"].ToString());
            }
            if (dr["ShareHoldPercentage"] != DBNull.Value)
            {
                property.ShareHoldPercentage = double.Parse(dr["ShareHoldPercentage"].ToString());
            }
            return property;
        }

        public static void DeleteUnit(int unitID)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the user record.
                    SqlCommand cmd = new SqlCommand("delete from tblPropertyUnit where unitid=" + unitID.ToString(), connection);
                    connection.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    connection.Close();
                }
            }

        }

        public static void EditUnit(Unit model)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the user record.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append(" update tblPropertyUnit set ");
                    sb.Append(" UnitName ='" + model.UnitName + "',");
                    sb.Append(" Notes ='" + model.Note + "'");
                    sb.Append(" where UnitID = " + model.UnitID );
                    cmd.CommandText = sb.ToString();
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    connection.Close();
                }
            }
        }

    }
}