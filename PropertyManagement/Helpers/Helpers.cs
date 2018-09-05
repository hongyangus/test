using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Web.Mvc;

namespace PropertyManagement.Helpers
{
    public static class Helpers
    {
        public enum ListType
        {
            company = 1,
            property = 2,
            unit = 3,
            bankaccount = 4,
            contractor = 6,
            allUser = 7,
            allExpenseCategory = 8,
            allStatus = 9,
            allRoles = 10,
            allAccountType = 11,
            allTenant = 12,
            allTenantWithUnit = 13,
            allReportList = 14,
            allWarehouse = 15,
            allTemplate = 16, 
            allSku = 17,
            allState = 18,
            allStore = 19,
            allEbayAccount = 20,
            allECommercePurchaseStatus = 21,
            allECommercePurchasePlan = 22,
            allECommerceCompany = 23,
            allECommerceVendor = 24,
            allECommerceCountry = 25
        };
        public enum StatusType
        {
            Open = 1,
            Pending = 2,
            Close = 3
        };

        public enum ExpenseCategory
        {
            Rent = 36,
            SecurityDeposit = 32,
            OwnerContribute = 23,
            OwnerWithdraw = 22,
            Repair = 17
        };

        public enum StatusCategory
        {
            EcommercePurchase = 1,
            OrderPlan = 2,
            OwnerContribute = 23,
            OwnerWithdraw = 22,
            Repair = 17
        };
        
        public enum EmailType
        {
            Rent = 36,
            SecurityDeposit = 32,
            Invoice = 1,
            RentReminder = 2,
            LeaseConfirmation = 3,
            LeaseTermination = 4,
            LeaseChange = 5,
            InvoicePaid = 6,
            InvoiceDue = 7
        };

        public enum DrilldownLevel
        {
            Daily = 1,
            Weekly = 2,
            Monthly = 3,
            Quarterly = 4,
            Yearly = 5
        };

        public enum AxisType
        {
            xAxis = 1,
            yAxis = 2
        };

        public enum ReportList
        {
            IncomeStatement = 1,
            Asset = 2,
            Monthly = 3,
            Quarterly = 4,
            Yearly = 5
        };

        public static string GetAppConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["SunriseConnectionString"].ConnectionString;
        }
        public static string GetERPConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["ECommerce"].ConnectionString;
        }
        public static string GetUserManagedCompanyString(string userID)
        {
            if (userID.Equals("1"))
            {
                return " (select companyID from tblCompanyUser)";
            }
            else
            {
                return " (select companyID from tblCompanyUser where tblCompanyUser.RoleID <5 and tblCompanyUser.UserID = " + userID + ")";
            }
        }

        public static int AdminRole = 2;

        public static int GetLoggedInUserID()
        {
            return int.Parse (System.Web.HttpContext.Current.Session["UserID"].ToString ());
        }        

        public enum ChartType
        {
            Column = 1,
            Bar = 2,
            Line = 3
        }


        public enum ComparisionType
        {
            Revenue = 1,
            Sku = 2,
            Account = 3,
            State = 4,
            Warehouse = 5,
            Vendor = 6
        }
        public enum ValueType
        {
            TotalRevenue = 1,
            TotalCost = 2,
            TotalVolume = 3,
            TotalShipping = 4,
            TotalInternationalShipping = 5,
            TotalProfit = 6,
            TotalFee = 7,
            AverageRevenue = 9,
            AverageCost = 10,
            AverageVolume = 11,
            AverageShipping = 12,
            AverageInternationalShipping = 13,
            AverageFee = 14,
            AverageProfit = 15,
            StandardDeviationShipping = 16
        }


        public enum VendorType
        {
            Haotang = 1,
            TotalFolks = 2,
            LLX = 3,
            MasonAutoParts = 4,
            Sunrise = 5

        }

        
        public enum WarehouseType
        {
            Middletown = 1,
            WestChester = 2,
            California = 3,
            Texas = 4,
            Burminghan = 5
        }

        public enum AccountType
        {
            PNCBANK = 1,
            EBayPurchase = 2,
            EBaySales = 3,
            AmazonPurchase = 4,
            AmazonSales = 5,
            FifthBank =6,
            transmotor = 7,
            WestChester = 8
        }

        static string appString = ConfigurationManager.ConnectionStrings["SunriseConnectionString"].ConnectionString;

    }
}