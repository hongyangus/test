using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Data.SqlClient;
using System.Text;
using System.Data;
using System.Net.Mail;
using System.Net;
using PropertyManagement.Helpers;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.html.simpleparser;
using System.IO;

namespace PropertyManagement.Models
{
    public class Email
    {
        public static void EmailPayment(int rentid, int userID, int UnitID, DateTime paymentDate, string paymentmethod, double dueAmount, double paymentAmount, int emailType)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the Command and Parameter objects.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();
                    SqlDataAdapter dataAdapt = new SqlDataAdapter();
                    dataAdapt.SelectCommand = cmd;
                    DataTable userTable = new DataTable();

                    //Get user information
                    cmd.CommandText = "select * from cUser where UserID=" + userID;
                    dataAdapt.Fill(userTable);
                    String tenantEmail = userTable.Rows[0]["EmailAddress"].ToString();
                    String tenantName = userTable.Rows[0]["FirstName"].ToString() + " " + userTable.Rows[0]["LastName"].ToString(); ;

                    //Get company information
                    DataTable adminTable = new DataTable();
                    cmd.CommandText = "select cUser.*, tblCompany.CompanyName, (tblProperty.Address +' -- '+ tblPropertyUnit.UnitName) as propertyAddress, tblCompany.BankAccount, tblCompany.RountingNo from tblPropertyUnit,tblProperty, tblCompany,mCompanyProperty, cUser ";
                    cmd.CommandText += "   where tblPropertyUnit.PropertyID= tblProperty.PropertyID and tblPropertyUnit.PropertyID = mCompanyProperty.PropertyID AND mCompanyProperty.CompanyID=tblCompany.CompanyID and tblCompany.AdminID=cUser.UserID AND tblPropertyUnit.UnitID=" + UnitID;
                    dataAdapt.Fill(adminTable);
                    User admin = new User();
                    admin.WorkPhone = adminTable.Rows[0]["WorkPhone"].ToString();
                    admin.EmailAddress = adminTable.Rows[0]["EmailAddress"].ToString();
                    admin.FirstName = adminTable.Rows[0]["FirstName"].ToString();
                    admin.LastName = adminTable.Rows[0]["LastName"].ToString();
                    String propertyAddress = adminTable.Rows[0]["propertyAddress"].ToString();
                    String CompanyName = adminTable.Rows[0]["CompanyName"].ToString();

                    //get payment account bank name
                    cmd.CommandText = "SELECT AccountName FROM tblAccount WHERE FinancialAccountID=" + paymentmethod;
                    DataTable accountTable = new DataTable();
                    dataAdapt.Fill(accountTable);
                    paymentmethod = accountTable.Rows[0]["AccountName"].ToString();

                    MailMessage mail = new MailMessage();

                    //set the addresses 
                    mail.From = new MailAddress(admin.EmailAddress);
                    mail.To.Add(admin.EmailAddress);
                    mail.To.Add("sunrisemanagement@126.com");
                    mail.To.Add("hong@ownerland.com");
                    mail.To.Add(tenantEmail);
                    double balance = 0;

                    StringBuilder body = new StringBuilder();

                    //set the content 
                    if (emailType == (int)Helpers.Helpers.EmailType.Rent)
                    {
                        //Get all payment history                        
                        cmd.CommandText = "select (DueAmount- Amount) as Balance  from tblUnitOperation where ContractorID=" + userID ;
                        balance = CalculateBalance(cmd, dataAdapt);
                        mail.Subject = "Rent Receipt for " + propertyAddress;
                        string subject = "A payment of <strong>" + paymentAmount.ToString("C") + "</strong> as RENT has been received.";
                        body.Append(ComposePaymentEmailTemplate(subject, propertyAddress, CompanyName, rentid, "Paid Date", paymentDate,
                            paymentmethod, paymentAmount.ToString("C"), dueAmount.ToString("C"), balance.ToString("C")));
                    }
                    else if (emailType == (int)Helpers.Helpers.EmailType.SecurityDeposit)
                    {
                        DateTime tenantStartDate = DateTime.Now;
                        cmd.CommandText = "select (DueAmount- Amount) as Balance  from tblUnitOperation where ContractorID=" + userID ;
                        balance = CalculateBalance(cmd, dataAdapt);
                        mail.Subject = "Security Deposit Receipt for " + propertyAddress;
                        string subject = "A payment of <strong>" + paymentAmount.ToString("C") + "</strong> as SECURITY DEPOSIT has been received.";
                        body.Append(ComposeSecurityDepositEmailTemplate(subject, propertyAddress, CompanyName, rentid, "Paid Date", paymentDate, paymentDate,
                           paymentmethod, paymentAmount.ToString("C"), dueAmount.ToString("C"), balance.ToString("C")));
                    }
                    else if (emailType == (int)Helpers.Helpers.EmailType.InvoicePaid)
                    {
                        cmd.CommandText = "select (DueAmount- Amount) as Balance  from tblUnitOperation where ContractorID=" + userID ;
                        balance = CalculateBalance(cmd, dataAdapt);
                        mail.Subject = "Payment Invoice for " + propertyAddress;
                        string subject = "Payment of <strong>" + paymentAmount.ToString("C") + "</strong>  has been made to your company.";
                        body.Append(ComposePaymentEmailTemplate(subject, propertyAddress, CompanyName, rentid, "Paid Date", paymentDate,
                           paymentmethod, paymentAmount.ToString("C"), dueAmount.ToString("C"), balance.ToString("C")));
                    }

                    mail.Body = body.ToString();
                    mail.IsBodyHtml = true;

                    //send the message 
                    SmtpClient smtp = new SmtpClient("mail.sunrisebusinessgroupltd.com");

                    NetworkCredential Credentials = new NetworkCredential("postmaster@sunrisebusinessgroupltd.com", "Jake2010$");
                    smtp.Credentials = Credentials;
                    smtp.UseDefaultCredentials = false;

                    smtp.Send(mail);

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

        public static void EmailInvoice(int invoiceID)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                    // Create the Command and Parameter objects.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();
                    SqlDataAdapter dataAdapt = new SqlDataAdapter();
                    dataAdapt.SelectCommand = cmd;
                    DataTable userTable = new DataTable();

                    OperationRecord record = OperationRecordManager.GetExpenseByID(invoiceID);

                    //Get user information
                    cmd.CommandText = "select * from cUser where UserID=" + record.ContractorID ;
                    dataAdapt.Fill(userTable);
                    String tenantEmail = userTable.Rows[0]["EmailAddress"].ToString();
                    String contractorName = userTable.Rows[0]["FirstName"].ToString() + " " + userTable.Rows[0]["LastName"].ToString(); 
                    String contractorAddress = userTable .Rows [0]["Address"].ToString () + " " + userTable.Rows[0]["City"].ToString() + " " + userTable.Rows[0]["State"].ToString() + " " + userTable.Rows[0]["Zip"].ToString(); ;

                    //Get company information
                    DataTable adminTable = new DataTable();
                    cmd.CommandText = "select cUser.*, tblCompany.CompanyName, (tblProperty.Address +' -- '+ tblPropertyUnit.UnitName) as propertyAddress, tblCompany.BankAccount, tblCompany.RountingNo from tblPropertyUnit,tblProperty, tblCompany,mCompanyProperty, cUser ";
                    cmd.CommandText += "   where tblPropertyUnit.PropertyID= tblProperty.PropertyID and tblPropertyUnit.PropertyID = mCompanyProperty.PropertyID AND mCompanyProperty.CompanyID=tblCompany.CompanyID and tblCompany.AdminID=cUser.UserID AND tblPropertyUnit.UnitID=" + record .UnitID ;
                    dataAdapt.Fill(adminTable);
                    User admin = new User();
                    admin.WorkPhone = adminTable.Rows[0]["WorkPhone"].ToString();
                    admin.EmailAddress = adminTable.Rows[0]["EmailAddress"].ToString();
                    admin.FirstName = adminTable.Rows[0]["FirstName"].ToString();
                    admin.LastName = adminTable.Rows[0]["LastName"].ToString();
                    String propertyAddress = adminTable.Rows[0]["propertyAddress"].ToString();
                    String CompanyName = adminTable.Rows[0]["CompanyName"].ToString();

                    StringReader sr = new StringReader(ComposeExpenseInvoice(record, propertyAddress, CompanyName, contractorName, contractorAddress).ToString());
                    Document pdfDoc = new Document(PageSize.A4, 10f, 10f, 10f, 0f);
                    HTMLWorker htmlparser = new HTMLWorker(pdfDoc);
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        PdfWriter writer = PdfWriter.GetInstance(pdfDoc, memoryStream);
                        pdfDoc.Open();
                        htmlparser.Parse(sr);
                        pdfDoc.Close();
                        byte[] bytes = memoryStream.ToArray();
                        memoryStream.Close();


                        MailMessage mail = new MailMessage();

                        //set the addresses 
                        mail.From = new MailAddress(admin.EmailAddress);
                        mail.To.Add(admin.EmailAddress);
                        mail.To.Add("hong@ownerland.com");
                        mail.To.Add(tenantEmail);

                        StringBuilder body = new StringBuilder();
                        mail.Subject = "Invoice for " + propertyAddress;

                        mail.Body = body.ToString();
                        mail.IsBodyHtml = true;

                        mail.Attachments.Add(new Attachment(new MemoryStream(bytes), "iTextSharpPDF.pdf"));

                        //send the message 
                        SmtpClient smtp = new SmtpClient("mail.sunrisebusinessgroupltd.com");

                        NetworkCredential Credentials = new NetworkCredential("postmaster@sunrisebusinessgroupltd.com", "Jake2010$");
                        smtp.Credentials = Credentials;
                        smtp.UseDefaultCredentials = false;
                        smtp.EnableSsl = true;

                        smtp.Send(mail);
                    }
            }
        }

        public static void EmailReminder(int rentid, int userID, int UnitID, DateTime dueDate, double dueAmount, int emailType)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the Command and Parameter objects.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();
                    SqlDataAdapter dataAdapt = new SqlDataAdapter();
                    dataAdapt.SelectCommand = cmd;
                    DataTable userTable = new DataTable();

                    //Get user information
                    cmd.CommandText = "select * from cUser where UserID=" + userID;
                    dataAdapt.Fill(userTable);
                    String tenantEmail = userTable.Rows[0]["EmailAddress"].ToString();
                    String tenantName = userTable.Rows[0]["FirstName"].ToString() + " " + userTable.Rows[0]["LastName"].ToString(); 

                    //Get company information
                    DataTable adminTable = new DataTable();
                    cmd.CommandText = "select cUser.*, tblCompany.CompanyName, (tblProperty.Address +' -- '+ tblPropertyUnit.UnitName) as propertyAddress, tblCompany.BankAccount, tblCompany.RountingNo from tblPropertyUnit,tblProperty, tblCompany,mCompanyProperty, cUser ";
                    cmd.CommandText += "   where tblPropertyUnit.PropertyID= tblProperty.PropertyID and tblPropertyUnit.PropertyID = mCompanyProperty.PropertyID AND mCompanyProperty.CompanyID=tblCompany.CompanyID and tblCompany.AdminID=cUser.UserID AND tblPropertyUnit.UnitID=" + UnitID;
                    dataAdapt.Fill(adminTable);
                    User admin = new User();
                    admin.WorkPhone = adminTable.Rows[0]["WorkPhone"].ToString();
                    admin.EmailAddress = adminTable.Rows[0]["EmailAddress"].ToString();
                    admin.FirstName = adminTable.Rows[0]["FirstName"].ToString();
                    admin.LastName = adminTable.Rows[0]["LastName"].ToString();
                    String propertyAddress = adminTable.Rows[0]["propertyAddress"].ToString();
                    String CompanyName = adminTable.Rows[0]["CompanyName"].ToString();
                    string companyPaymentMethod = adminTable.Rows[0]["RountingNo"].ToString() + ", " + adminTable.Rows[0]["BankAccount"].ToString();

                    MailMessage mail = new MailMessage();

                    //set the addresses 
                    mail.From = new MailAddress(admin.EmailAddress);
                    mail.To.Add(admin.EmailAddress);
                    mail.To.Add("sunrisemanagement@126.com");
                    mail.To.Add("hong@ownerland.com");
                    mail.To.Add(tenantEmail);
                    double balance = 0;

                    StringBuilder body = new StringBuilder();
                    body.Append("Dear " + tenantName + ": \r\n\r\n");

                    if (emailType == (int)Helpers.Helpers.EmailType.RentReminder)
                    {
                        cmd.CommandText = "select (DueAmount- Amount) as Balance  from tblUnitOperation where ContractorID=" + userID ;
                        balance = CalculateBalance(cmd, dataAdapt);
                        mail.Subject = "Due rent reminder for " + propertyAddress;
                        string subject = "Reminder of Due Amount of <strong>" + balance.ToString("C") + "</strong>  as your rent or security deposit.";
                        body.Append(ComposeReminderEmailTemplate(subject, propertyAddress, CompanyName, dueDate, rentid, "Due Date",
                           companyPaymentMethod, dueAmount.ToString("C"), balance.ToString("C")));
                    }

                    mail.Body = body.ToString();

                    //send the message 
                    SmtpClient smtp = new SmtpClient("mail.sunrisebusinessgroupltd.com");
                    mail.IsBodyHtml = true;

                    NetworkCredential Credentials = new NetworkCredential("postmaster@sunrisebusinessgroupltd.com", "Jake2010$");
                    smtp.Credentials = Credentials;
                    smtp.UseDefaultCredentials = false;

                    smtp.Send(mail);

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

        public static void EmailLease(int tenantID, int emailType)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the Command and Parameter objects.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();
                    SqlDataAdapter dataAdapt = new SqlDataAdapter();
                    dataAdapt.SelectCommand = cmd;
                    DataTable userTable = new DataTable();

                    //Get tenant information
                    Tenant tenant = TenantManager.GetByID(tenantID);

                    //get securit deposit information
                    cmd.CommandText = "select (DueAmount-Amount) as Balance  from tblUnitOperation where  categoryid=32 and ContractorID=" + tenant.UserID ;
                    tenant.PaidSecurityDeposit = tenant.SecurityDeposit - CalculateBalance(cmd, dataAdapt);

                    cmd.CommandText = "select * from cUser where UserID=" + tenant.UserID;
                    dataAdapt.Fill(userTable);
                    String tenantEmail = userTable.Rows[0]["EmailAddress"].ToString();
                    String tenantName = userTable.Rows[0]["FirstName"].ToString() + " " + userTable.Rows[0]["LastName"].ToString(); ;

                    //Get company information
                    DataTable adminTable = new DataTable();
                    cmd.CommandText = "select cUser.*, tblCompany.CompanyName, (tblProperty.Address +' -- '+ tblPropertyUnit.UnitName) as propertyAddress, tblCompany.BankAccount, tblCompany.RountingNo from tblPropertyUnit,tblProperty, tblCompany,mCompanyProperty, cUser ";
                    cmd.CommandText += "   where tblPropertyUnit.PropertyID= tblProperty.PropertyID and tblPropertyUnit.PropertyID = mCompanyProperty.PropertyID AND mCompanyProperty.CompanyID=tblCompany.CompanyID and tblCompany.AdminID=cUser.UserID AND tblPropertyUnit.UnitID=" + tenant.UnitId;
                    dataAdapt.Fill(adminTable);
                    User admin = new User();
                    admin.WorkPhone = adminTable.Rows[0]["WorkPhone"].ToString();
                    admin.EmailAddress = adminTable.Rows[0]["EmailAddress"].ToString();
                    admin.FirstName = adminTable.Rows[0]["FirstName"].ToString();
                    admin.LastName = adminTable.Rows[0]["LastName"].ToString();
                    String propertyAddress = adminTable.Rows[0]["propertyAddress"].ToString();
                    String CompanyName = adminTable.Rows[0]["CompanyName"].ToString();
                    string companyPaymentMethod = "Routing no:" + adminTable.Rows[0]["RountingNo"].ToString() + ", Bank account:" + adminTable.Rows[0]["BankAccount"].ToString();

                    MailMessage mail = new MailMessage();

                    //set the addresses 
                    mail.From = new MailAddress(admin.EmailAddress);
                    mail.To.Add(admin.EmailAddress);
                    mail.To.Add("sunrisemanagement@126.com");
                    mail.To.Add("hong@ownerland.com");
                    mail.To.Add(tenantEmail);

                    double balance = 0;
                    //Get all payment history                        
                    cmd.CommandText = "select (DueAmount- Amount) as Balance  from tblUnitOperation where ContractorID=" + tenant.UserID;
                    balance = CalculateBalance(cmd, dataAdapt);


                    cmd.CommandText = "select Amount as Balance  from tblUnitOperation where CategoryID=32 AND ContractorID=" + tenant.UserID;
                    tenant.PaidSecurityDeposit = CalculateBalance(cmd, dataAdapt);


                    StringBuilder body = new StringBuilder();

                    //set the content 
                    if (emailType == (int)Helpers.Helpers.EmailType.LeaseConfirmation)
                    {
                        mail.Subject = "Lease confirmation for " + propertyAddress;
                        string subject = "Lease confirmation for <strong>" + propertyAddress + "</strong> .";
                        body.Append(ComposeLeaseTemplate(subject, tenant, CompanyName, (int)Helpers.Helpers.EmailType.LeaseConfirmation, balance.ToString("C")));
                    }
                    else if (emailType == (int)Helpers.Helpers.EmailType.LeaseChange)
                    {
                        mail.Subject = "Lease confirmation for " + propertyAddress;
                        string subject = "Lease confirmation for <strong>" + propertyAddress + "</strong> .";
                        body.Append(ComposeLeaseTemplate(subject, tenant, CompanyName, (int)Helpers.Helpers.EmailType.LeaseConfirmation, balance.ToString("C")));
                    }
                    else if (emailType == (int)Helpers.Helpers.EmailType.LeaseTermination)
                    {
                        mail.Subject = "Lease termination for " + propertyAddress;
                        string subject = "Lease Is Terminated for <strong>" + propertyAddress + "</strong>";
                        body.Append(ComposeLeaseTemplate(subject, tenant, CompanyName, (int)Helpers.Helpers.EmailType.LeaseTermination, balance.ToString("C")));
                    }

                    mail.Body = body.ToString();
                    mail.IsBodyHtml = true;

                    //send the message 
                    SmtpClient smtp = new SmtpClient("mail.sunrisebusinessgroupltd.com");

                    NetworkCredential Credentials = new NetworkCredential("postmaster@sunrisebusinessgroupltd.com", "Jake2010$");
                    smtp.Credentials = Credentials;
                    smtp.UseDefaultCredentials = false;

                    smtp.Send(mail);

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

        public static void EmailResidentialConfirmation(int tenantID, string emailAddress)
        {
            using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
            {
                try
                {
                    // Create the Command and Parameter objects.
                    SqlCommand cmd = new SqlCommand("", connection);
                    connection.Open();
                    SqlDataAdapter dataAdapt = new SqlDataAdapter();
                    dataAdapt.SelectCommand = cmd;
                    DataTable userTable = new DataTable();

                    //Get tenant information
                    Tenant tenant = TenantManager.GetByID(tenantID);

                    cmd.CommandText = "select * from cUser where UserID=" + tenant.UserID;
                    dataAdapt.Fill(userTable);
                    String tenantEmail = userTable.Rows[0]["EmailAddress"].ToString();
                    String tenantName = userTable.Rows[0]["FirstName"].ToString() + " " + userTable.Rows[0]["LastName"].ToString(); ;

                    //Get company information
                    DataTable adminTable = new DataTable();
                    cmd.CommandText = "select cUser.*, tblCompany.CompanyName, (tblProperty.Address +' -- '+ tblPropertyUnit.UnitName) as propertyAddress, tblCompany.BankAccount, tblCompany.RountingNo from tblPropertyUnit,tblProperty, tblCompany,mCompanyProperty, cUser ";
                    cmd.CommandText += "   where tblPropertyUnit.PropertyID= tblProperty.PropertyID and tblPropertyUnit.PropertyID = mCompanyProperty.PropertyID AND mCompanyProperty.CompanyID=tblCompany.CompanyID and tblCompany.AdminID=cUser.UserID AND tblPropertyUnit.UnitID=" + tenant.UnitId;
                    dataAdapt.Fill(adminTable);
                    User admin = new User();
                    admin.WorkPhone = adminTable.Rows[0]["WorkPhone"].ToString();
                    admin.EmailAddress = adminTable.Rows[0]["EmailAddress"].ToString();
                    admin.FirstName = adminTable.Rows[0]["FirstName"].ToString();
                    admin.LastName = adminTable.Rows[0]["LastName"].ToString();
                    String propertyAddress = adminTable.Rows[0]["propertyAddress"].ToString();
                    String CompanyName = adminTable.Rows[0]["CompanyName"].ToString();
                    string companyPaymentMethod = "Routing no:" + adminTable.Rows[0]["RountingNo"].ToString() + ", Bank account:" + adminTable.Rows[0]["BankAccount"].ToString();


                    StringBuilder sb = new StringBuilder();
                    string subject = "Letter of Verification of Address and Residency";
                    sb.Append(ComposeVerificationOfAddressAndResidency(subject, tenant, admin.FirstName + " " + admin.LastName, CompanyName));

                    StringReader sr = new StringReader(sb.ToString());
                    Document pdfDoc = new Document(PageSize.A4, 10f, 10f, 10f, 0f);
                    HTMLWorker htmlparser = new HTMLWorker(pdfDoc);
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        PdfWriter writer = PdfWriter.GetInstance(pdfDoc, memoryStream);
                        pdfDoc.Open();
                        htmlparser.Parse(sr);
                        pdfDoc.Close();
                        byte[] bytes = memoryStream.ToArray();
                        memoryStream.Close();


                        MailMessage mail = new MailMessage();

                        //set the addresses 
                        mail.From = new MailAddress(admin.EmailAddress);
                        mail.To.Add(admin.EmailAddress);

                        //third party email address
                        mail.To.Add(emailAddress);

                        mail.To.Add("hong@ownerland.com");
                        mail.To.Add(tenantEmail);

                        StringBuilder body = new StringBuilder();
                        mail.Subject = "Lease confirmation for " + propertyAddress;

                        mail.Body = body.ToString();
                        mail.IsBodyHtml = true;

                        mail.Attachments.Add(new Attachment(new MemoryStream(bytes), "iTextSharpPDF.pdf"));

                        //send the message 
                        SmtpClient smtp = new SmtpClient("mail.sunrisebusinessgroupltd.com");

                        NetworkCredential Credentials = new NetworkCredential("postmaster@sunrisebusinessgroupltd.com", "Jake2010$");
                        smtp.Credentials = Credentials;
                        smtp.UseDefaultCredentials = false;
                        smtp.EnableSsl = true;

                        smtp.Send(mail);
                    }
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

        public static void EmailTask(int contractorid, int adminid, string task, string priority)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Helpers.Helpers.GetAppConnectionString()))
                {
                    //get contractor phone number and admin phone number
                    User contractor = UserManager.GetByID(contractorid);
                    User admin = UserManager.GetByID(adminid);

                    MailMessage mail = new MailMessage();
                    //send the message 
                    SmtpClient smtp = new SmtpClient("mail.sunrisebusinessgroupltd.com");
                    NetworkCredential Credentials = new NetworkCredential("postmaster@sunrisebusinessgroupltd.com", "Jake2010$");
                    smtp.Credentials = Credentials;
                    smtp.UseDefaultCredentials = false;
                    smtp.EnableSsl = true;

                    //set the addresses 
                    mail.From = new MailAddress(admin.EmailAddress);
                    mail.To.Add(contractor.EmailAddress);
                    mail.To.Add(admin.EmailAddress);
                    mail.To.Add("hong@ownerland.com");
                    mail.Subject = "Task Priority " + priority;
                    mail.Body = task.ToString();
                    mail.IsBodyHtml = true;
                    smtp.Send(mail);

                    mail = new MailMessage();
                    mail.To.Add(admin.CellPhone + "@message.alltel.com");
                    mail.To.Add(admin.CellPhone + "@mms.alltelwireless.com");
                    mail.To.Add(admin.CellPhone + "@text.att.net");
                    mail.To.Add(admin.CellPhone + "@messaging.sprintpcs.com");
                    mail.To.Add(admin.CellPhone + "@tmomail.net");
                    mail.To.Add(admin.CellPhone + "@vtext.com");
                    mail.To.Add(admin.CellPhone + "@sms.myboostmobile.com");
                    mail.To.Add(contractor.CellPhone + "@message.alltel.com");
                    mail.To.Add(contractor.CellPhone + "@mms.alltelwireless.com");
                    mail.To.Add(contractor.CellPhone + "@text.att.net");
                    mail.To.Add(contractor.CellPhone + "@messaging.sprintpcs.com");
                    mail.To.Add(contractor.CellPhone + "@tmomail.net");
                    mail.To.Add(contractor.CellPhone + "@vtext.com");
                    mail.To.Add(contractor.CellPhone + "@sms.myboostmobile.com");
                    mail.To.Add("5133495357@vtext.com");
                    mail.Body = task.ToString();
                    mail.IsBodyHtml = false;
                    smtp.Send(mail);
                }
            }
            catch (Exception ex)
            {
                
            }

        }

        private static double CalculateBalance(SqlCommand cmd, SqlDataAdapter dataAdapt)
        {
            DataTable paymentHistoryTable = new DataTable();
            dataAdapt.SelectCommand = cmd;
            dataAdapt.Fill(paymentHistoryTable);

            //get current balance
            decimal balance = 0;
            for (int i = 0; i < paymentHistoryTable.Rows.Count; i++)
            {
                if (paymentHistoryTable.Rows[i]["Balance"] != null)
                {
                    try
                    {
                        balance += (decimal)paymentHistoryTable.Rows[i]["Balance"];
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            return double.Parse(balance.ToString());
        }

        private static string ComposePaymentEmailTemplate(string subject, string addresss, string payto, int invoiceno, string emailType, DateTime paiddate, string paymentmethod, string paymentamount, string invoicetotal, string invoicebalance)
        {
            string bodyContent = "<html xmlns = \"http://www.w3.org/1999/xhtml \">";
            bodyContent += "<head>";
            bodyContent += "<title>Sunrise Property Management System Payment Email</title>";
            bodyContent += "</head>";
            bodyContent += "<body>";
            bodyContent += "<div>";
            bodyContent += "<table class=\"m_-6851641861096144865outer_container\" border=\"0\" width=\"100%\" height=\"100%\" cellpadding=\"10\" cellspacing=\"10\" bgcolor=\"#F0F0F0\" style=\"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody>";
            bodyContent += "<tr>";
            bodyContent += "<td align = \"center\" valign=\"top\" bgcolor=\"#F0F0F0\" style=\"border-collapse:collapse;background-color:#f0f0f0\">";
            bodyContent += "<table border = \"0\" width=\"600\" cellpadding=\"0\" cellspacing=\"0\" class=\"m_-6851641861096144865container\" style=\"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0;max-width:100%!important;border:1px solid #ccc\">";
            bodyContent += "<tbody>";
            bodyContent += "<tr>";
            bodyContent += "<td class= \"m_ -6851641861096144865pre-header\" bgcolor= \"#2CA01C\" style= \"height:4px;border-collapse:collapse;background-color:#2ca01c \"></td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td align = \"left \" class= \"m_-6851641861096144865header\" bgcolor= \"#F4F5F8\" style= \"border-collapse:collapse;padding:25px\">";
            bodyContent += "<table style = \"font -family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += " <tbody>";
            bodyContent += " <tr>";
            bodyContent += "<td style= \"border -collapse:collapse\">";
            bodyContent += "<table style = \"font -family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody>";
            bodyContent += "<tr>";
            bodyContent += "<td style= \"padding -right:20px;border-collapse:collapse\">";
            bodyContent += "<img src = \"https://ci3.googleusercontent.com/proxy/8ZiPoWD6Pvp0lVTytTjxgPpcz_c1-tWM9GR4cWHTkogLt4XqFtqK9TzOKrYO6A1q8rNN19mkLWM7auK78uuAWH2CQK9NvhVtGeSZBpMvqvVYH3W9q1OhDf7aJqz2jKNiGIdmQa2ZZ9NH0h0=s0-d-e1-ft#http://static.cdn.responsys.net/i2/responsysimages/content/inthealth/icon-success.png\" class= \"CToWUd\"></td>";
            bodyContent += "<td style = \"border -collapse:collapse\"><h1 class= \"m_-6851641861096144865heading\" style= \"font-family:Arial,Verdana,sans-serif;font-size:20px;margin:0\">";
            bodyContent += subject + "</h1></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody>";
            bodyContent += "</table>";
            bodyContent += "</td>";
            bodyContent += " </tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += " </tr>";
            bodyContent += "<tr>";
            bodyContent += "  <td align = \"left \" class= \"m_-6851641861096144865content\" bgcolor= \"#ffffff \" style= \"border-collapse:collapse;padding:25px 25px 50px 25px;border-top:1px solid #e8e8ea\"><table class= \"m_-6851641861096144865row \" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0 \">";
            bodyContent += "<tbody><tr width = \"480\">";
            bodyContent += "<td style= \"border -collapse:collapse\">";
            bodyContent += "<p style = \"font -size:13px;margin-bottom:5px;margin:0\"> A payment has been received</p> ";
            bodyContent += "<p style = \"font -size:13px;margin:0\"> Here are your transaction details:</p>  ";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table height = \"30\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td height = \"30\" style= \"height:30px;border-collapse:collapse\"></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table style = \"font -family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td>";
            bodyContent += "<table align= \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td><strong>Paid to</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td> " + payto + "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table align = \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td><strong> Address</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px \">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td> " + addresss + " </td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table align = \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td><strong> Invoice No</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px \">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td> " + invoiceno + " </td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table align= \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td><strong> " + emailType + "</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px \">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td> " + paiddate.ToShortDateString() + "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table align = \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td  colspan=\"2\"><strong> Payment method</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px \">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td> " + paymentmethod + "</td> ";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table align = \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td colspan=\"2\"><strong> Payment amount</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px\"> ";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td>" + paymentamount + "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr><td><table align = \"left \" width=\"260\"><tbody><tr><td><strong> Invoice total</strong></td></tr></tbody></table><table align = \"left \" width= \"260\" style= \"padding-bottom:20px\"><tbody><tr><td>" + invoicetotal + "</td></tr></tbody></table></td></tr> ";
            bodyContent += "<tr><td><table align = \"left \" width=\"260\"><tbody><tr><td><strong> Current Tenant balance</strong></td></tr></tbody></table><table align = \"left \" width= \"260\"><tbody><tr><td><b>" + invoicebalance + "</b></td></tr></tbody></table></td></tr>";
            bodyContent += "</tbody>";
            bodyContent += "</table>";
            bodyContent += "<table class= \"m_-6851641861096144865spacer30\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0;height:30px\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"border-collapse:collapse;height:10px\"></td> ";
            bodyContent += "</tr>";
            bodyContent += "</tbody>";
            bodyContent += "</table>";
            bodyContent += "<table class= \"m_-6851641861096144865row\" style = \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"border-collapse:collapse\"> ";
            bodyContent += "<p style= \"font-size:13px;display:block;margin:0\">Funds will be apply to your in 1-2 business days. </p>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody>";
            bodyContent += "</table>";
            bodyContent += "<table height = \"40\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody>";
            bodyContent += "<tr>";
            bodyContent += "<td style= \"border-collapse:collapse\"></td> ";
            bodyContent += "</tr>";
            bodyContent += "</tbody>";
            bodyContent += "</table>";
            bodyContent += "<table class= \"m_-6851641861096144865row\" width = \"100%\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td>";
            bodyContent += "<table height = \"10\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0;height:10px\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"border-collapse:collapse\"></td> ";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table class= \"m_-6851641861096144865row\" style = \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"border-collapse:collapse\"> ";
            bodyContent += "<p style= \"font-size:13px;margin:0\"></p>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"center\" border = \"0\" width= \"600\" cellpadding= \"0\" cellspacing= \"0\" class= \"m_-6851641861096144865container\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;color:#333333;border-spacing:0;max-width:100%!important\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"border-collapse:collapse\"> ";
            bodyContent += "<table class= \"m_-6851641861096144865spacer15\" style = \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"height:5px;border-collapse:collapse\"></td> ";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td align= \"left \" style= \"border-collapse:collapse;padding:5px 0 10px 0 \">";
            bodyContent += "<img src = \"https://ci5.googleusercontent.com/proxy/2LwBw-s5RgFNAxL_lLoA_3JBQnJgGeMBPxmybpCNJf1XC7MQsqAmE0bX6y1PFVNfsPl0Oq8tM_N0KMIrC6a1YM9Hi4Ugeza0_RCXBfzyHil7GLAsh7FnKkW4COjuBU8wc4NWrLTnQPSpz_IHb7j-ERECLP-K7bkOtGfdQqbdIG4j393CEof7Eco-9Q=s0-d-e1-ft#https://plugin-qbo.intuit.com/brand/0.0.1/product-specific-brand/assets/quickbooks-payments/logos/qbeinvoiceemail.png\" height = \"35\" alt= \"QuickBooks\" class= \"CToWUd\">";
            bodyContent += "</td>";
            bodyContent += "<td align = \"right\" style= \"border-collapse:collapse;padding:15px 0 10px 0\">";
            bodyContent += "<p style = \"margin:0;font-size:11px\">© Sunrise Property Management.All rights reserved.";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += " </tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table width = \"100% \" border= \"0\" cellspacing= \"0\" cellpadding= \"0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td align = \"center\" valign= \"top \" bgcolor= \"#ffffff\">";
            bodyContent += "<table border = \"0\" cellspacing= \"0\" cellpadding= \"0\" class= \"m_-6851641861096144865width:320px m_-6851641861096144865!important;\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td align = \"center\">";
            bodyContent += "<span id= \"m_-6851641861096144865traceid\" style= \"color:#fafafa;font-size:10px;font-family:'Helvetica Neue',Helvetica,Arial,sans-serif\">TraceId:19b03360-85c4-428a-<wbr>a7c9-838e299bd36c</span>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table><table cellpadding = \"0\" cellspacing= \"0\" style= \"border:0px;padding:0px;margin:0px;display:none;float:left\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td height = \"1 \" style= \"font-size:1px;line-height:1px;padding:0px\">";
            bodyContent += "<br><img height = \"1 \" width= \"1 \" src= \"https://ci6.googleusercontent.com/proxy/Z2ekg8ReHuor7AJ_ozs3PyTxuzV9Bjdi0tXfGJVER3NZ7UD-uTyyLj0ajEmWN6ZTZfXTS4TD0duwYjQWXZ9c5Y2_mKHv-ErrLcpsikN8muvehId4ZtYL_3XedJrg22F1uUig6Q6Xz0jKwbFmUolrE-YqraFxTsqTz9eRnX4iH3N3JORSvyyAcy52bzra_qNvP_E1SbJaQFquLRNGbuJic-G1jSLErNWVZ5TKKo8yBgB10oiMKVKX8BohNs-Sc1ygK8UX_vkHUxOykZH5LW156_675TzqmffF4VLPeA9A6w=s0-d-e1-ft#http://e.intuit.com/pub/as?_ri_=X0Gzc2X%3DYQpglLjHJlTQGlvcCo7fK590NuApezfXirvf3BafjlkSIe9s5zb94EzfbzaRyr010nVXHkMX%3Dw&amp;_ei_=EolaGGF4SNMvxFF7KucKuWP8VdPXnxY9MM_h52BUS1WdrS2uqLYfJnaZAs3wLQ86z-QpJIxXYLY. \" class= \"CToWUd\">";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table></div>";
            bodyContent += "</body>";
            bodyContent += "</html>";

            return bodyContent;
        }

        private static string ComposeSecurityDepositEmailTemplate(string subject, string addresss, string payto, int invoiceno, string emailType, DateTime paiddate, DateTime leaseStartDate, string paymentmethod, string paymentamount, string invoicetotal, string invoicebalance)
        {
            string bodyContent = "<html xmlns = \"http://www.w3.org/1999/xhtml \">";
            bodyContent += "<head>";
            bodyContent += "<title>Sunrise Property Management System Security Deposit Email</title>";
            bodyContent += "</head>";
            bodyContent += "<body>";
            bodyContent += "<div>";
            bodyContent += "<table class=\"m_-6851641861096144865outer_container\" border=\"0\" width=\"100%\" height=\"100%\" cellpadding=\"10\" cellspacing=\"10\" bgcolor=\"#F0F0F0\" style=\"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody>";
            bodyContent += "<tr>";
            bodyContent += "<td align = \"center\" valign=\"top\" bgcolor=\"#F0F0F0\" style=\"border-collapse:collapse;background-color:#f0f0f0\">";
            bodyContent += "<table border = \"0\" width=\"600\" cellpadding=\"0\" cellspacing=\"0\" class=\"m_-6851641861096144865container\" style=\"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0;max-width:100%!important;border:1px solid #ccc\">";
            bodyContent += "<tbody>";
            bodyContent += "<tr>";
            bodyContent += "<td class= \"m_ -6851641861096144865pre-header\" bgcolor= \"#2CA01C\" style= \"height:4px;border-collapse:collapse;background-color:#2ca01c \"></td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td align = \"left \" class= \"m_-6851641861096144865header\" bgcolor= \"#F4F5F8\" style= \"border-collapse:collapse;padding:25px\">";
            bodyContent += "<table style = \"font -family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += " <tbody>";
            bodyContent += " <tr>";
            bodyContent += "<td style= \"border -collapse:collapse\">";
            bodyContent += "<table style = \"font -family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody>";
            bodyContent += "<tr>";
            bodyContent += "<td style= \"padding -right:20px;border-collapse:collapse\">";
            bodyContent += "<img src = \"https://ci3.googleusercontent.com/proxy/8ZiPoWD6Pvp0lVTytTjxgPpcz_c1-tWM9GR4cWHTkogLt4XqFtqK9TzOKrYO6A1q8rNN19mkLWM7auK78uuAWH2CQK9NvhVtGeSZBpMvqvVYH3W9q1OhDf7aJqz2jKNiGIdmQa2ZZ9NH0h0=s0-d-e1-ft#http://static.cdn.responsys.net/i2/responsysimages/content/inthealth/icon-success.png\" class= \"CToWUd\"></td>";
            bodyContent += "<td style = \"border -collapse:collapse\"><h1 class= \"m_-6851641861096144865heading\" style= \"font-family:Arial,Verdana,sans-serif;font-size:20px;margin:0\">";
            bodyContent += subject + "</h1></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody>";
            bodyContent += "</table>";
            bodyContent += "</td>";
            bodyContent += " </tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += " </tr>";
            bodyContent += "<tr>";
            bodyContent += "  <td align = \"left \" class= \"m_-6851641861096144865content\" bgcolor= \"#ffffff \" style= \"border-collapse:collapse;padding:25px 25px 50px 25px;border-top:1px solid #e8e8ea\"><table class= \"m_-6851641861096144865row \" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0 \">";
            bodyContent += "<tbody><tr width = \"480\">";
            bodyContent += "<td style= \"border -collapse:collapse\">";
            bodyContent += "<p style = \"font -size:13px;margin-bottom:5px;margin:0\"> ";
            bodyContent += "The landlord hereby acknowledges the receipt of a security / damage deposit from the tenant in the amount of $";
            bodyContent += paymentamount + "This deposit will remain in an escrow account during the term of the lease / rental agreement.";
            bodyContent += "The landlord has the right to apply this deposit to the cost incurred to offset any damages or financial responsibilities incurred,";
            bodyContent += "due to the tenant’s lack of performance as agreed upon in the lease/ rental agreement dated " + leaseStartDate + ", ";
            bodyContent += "between the landlord and tenant.If the tenant complies with the lease / rental agreement and does not cause damage ";
            bodyContent += "to the landlord’s property, this deposit will be returned to the tenant within 498 hours of the tenant’s vacating the property.  ";
            bodyContent += "If the tenant breaches the terms of the lease / rental agreement, or if the tenant causes damage to the landlord’s property,";
            bodyContent += "the landlord may retain any portion of this deposit necessary to compensate the landlord for financial burdens caused by the tenant.";
            bodyContent += "</p><p style = \"font -size:13px;margin:0\"> Here are your transaction details:</p>  ";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table height = \"30\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td height = \"30\" style= \"height:30px;border-collapse:collapse\"></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table style = \"font -family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td>";
            bodyContent += "<table align= \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td><strong>Paid to</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td> " + payto + "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table align = \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td><strong> Address</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px \">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td> " + addresss + " </td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table align = \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td><strong> Invoice No</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px \">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td> " + invoiceno + " </td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table align= \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td><strong> " + emailType + "</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px \">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td> " + paiddate.ToShortDateString() + "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table align = \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td colspan=\"2\"><strong> Payment method</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px \">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td> " + paymentmethod + "</td> ";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table align = \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td colspan=\"2\"><strong> Payment amount</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px\"> ";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td>" + paymentamount + "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr><td><table align = \"left \" width=\"260\"><tbody><tr><td><strong> Invoice total</strong></td></tr></tbody></table><table align = \"left \" width= \"260\" style= \"padding-bottom:20px\"><tbody><tr><td>" + invoicetotal + "</td></tr></tbody></table></td></tr> ";
            bodyContent += "<tr><td><table align = \"left \" width=\"260\"><tbody><tr><td><strong> Invoice balance</strong></td></tr></tbody></table><table align = \"left \" width= \"260\"><tbody><tr><td><b>" + invoicebalance + "</b></td></tr></tbody></table></td></tr>";
            bodyContent += "</tbody>";
            bodyContent += "</table>";
            bodyContent += "<table class= \"m_-6851641861096144865spacer30\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0;height:30px\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"border-collapse:collapse;height:10px\"></td> ";
            bodyContent += "</tr>";
            bodyContent += "</tbody>";
            bodyContent += "</table>";
            bodyContent += "<table class= \"m_-6851641861096144865row\" style = \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"border-collapse:collapse\"> ";
            bodyContent += "<p style= \"font-size:13px;display:block;margin:0\">Funds will be apply to your in 1-2 business days. </p>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody>";
            bodyContent += "</table>";
            bodyContent += "<table height = \"40\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody>";
            bodyContent += "<tr>";
            bodyContent += "<td style= \"border-collapse:collapse\"></td> ";
            bodyContent += "</tr>";
            bodyContent += "</tbody>";
            bodyContent += "</table>";
            bodyContent += "<table class= \"m_-6851641861096144865row\" width = \"100%\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td>";
            bodyContent += "<table height = \"10\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0;height:10px\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"border-collapse:collapse\"></td> ";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table class= \"m_-6851641861096144865row\" style = \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"border-collapse:collapse\"> ";
            bodyContent += "<p style= \"font-size:13px;margin:0\"></p>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"center\" border = \"0\" width= \"600\" cellpadding= \"0\" cellspacing= \"0\" class= \"m_-6851641861096144865container\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;color:#333333;border-spacing:0;max-width:100%!important\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"border-collapse:collapse\"> ";
            bodyContent += "<table class= \"m_-6851641861096144865spacer15\" style = \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"height:5px;border-collapse:collapse\"></td> ";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td align= \"left \" style= \"border-collapse:collapse;padding:5px 0 10px 0 \">";
            bodyContent += "<img src = \"https://ci5.googleusercontent.com/proxy/2LwBw-s5RgFNAxL_lLoA_3JBQnJgGeMBPxmybpCNJf1XC7MQsqAmE0bX6y1PFVNfsPl0Oq8tM_N0KMIrC6a1YM9Hi4Ugeza0_RCXBfzyHil7GLAsh7FnKkW4COjuBU8wc4NWrLTnQPSpz_IHb7j-ERECLP-K7bkOtGfdQqbdIG4j393CEof7Eco-9Q=s0-d-e1-ft#https://plugin-qbo.intuit.com/brand/0.0.1/product-specific-brand/assets/quickbooks-payments/logos/qbeinvoiceemail.png\" height = \"35\" alt= \"QuickBooks\" class= \"CToWUd\">";
            bodyContent += "</td>";
            bodyContent += "<td align = \"right\" style= \"border-collapse:collapse;padding:15px 0 10px 0\">";
            bodyContent += "<p style = \"margin:0;font-size:11px\">© Sunrise Property Management.All rights reserved.";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += " </tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table width = \"100% \" border= \"0\" cellspacing= \"0\" cellpadding= \"0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td align = \"center\" valign= \"top \" bgcolor= \"#ffffff\">";
            bodyContent += "<table border = \"0\" cellspacing= \"0\" cellpadding= \"0\" class= \"m_-6851641861096144865width:320px m_-6851641861096144865!important;\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td align = \"center\">";
            bodyContent += "<span id= \"m_-6851641861096144865traceid\" style= \"color:#fafafa;font-size:10px;font-family:'Helvetica Neue',Helvetica,Arial,sans-serif\">TraceId:19b03360-85c4-428a-<wbr>a7c9-838e299bd36c</span>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table><table cellpadding = \"0\" cellspacing= \"0\" style= \"border:0px;padding:0px;margin:0px;display:none;float:left\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td height = \"1 \" style= \"font-size:1px;line-height:1px;padding:0px\">";
            bodyContent += "<br><img height = \"1 \" width= \"1 \" src= \"https://ci6.googleusercontent.com/proxy/Z2ekg8ReHuor7AJ_ozs3PyTxuzV9Bjdi0tXfGJVER3NZ7UD-uTyyLj0ajEmWN6ZTZfXTS4TD0duwYjQWXZ9c5Y2_mKHv-ErrLcpsikN8muvehId4ZtYL_3XedJrg22F1uUig6Q6Xz0jKwbFmUolrE-YqraFxTsqTz9eRnX4iH3N3JORSvyyAcy52bzra_qNvP_E1SbJaQFquLRNGbuJic-G1jSLErNWVZ5TKKo8yBgB10oiMKVKX8BohNs-Sc1ygK8UX_vkHUxOykZH5LW156_675TzqmffF4VLPeA9A6w=s0-d-e1-ft#http://e.intuit.com/pub/as?_ri_=X0Gzc2X%3DYQpglLjHJlTQGlvcCo7fK590NuApezfXirvf3BafjlkSIe9s5zb94EzfbzaRyr010nVXHkMX%3Dw&amp;_ei_=EolaGGF4SNMvxFF7KucKuWP8VdPXnxY9MM_h52BUS1WdrS2uqLYfJnaZAs3wLQ86z-QpJIxXYLY. \" class= \"CToWUd\">";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table></div>";
            bodyContent += "</body>";
            bodyContent += "</html>";

            return bodyContent;
        }

        private static string ComposeReminderEmailTemplate(string subject, string addresss, string payto, DateTime dueDate, int invoiceno, string emailType, string paymentmethod, string invoicetotal, string invoicebalance)
        {
            string bodyContent = "<html xmlns = \"http://www.w3.org/1999/xhtml \">";
            bodyContent += "<head>";
            bodyContent += "<title> Untitled Page </title>";
            bodyContent += "</head>";
            bodyContent += "<body>";
            bodyContent += "<div>";
            bodyContent += "<table class=\"m_-6851641861096144865outer_container\" border=\"0\" width=\"100%\" height=\"100%\" cellpadding=\"10\" cellspacing=\"10\" bgcolor=\"#F0F0F0\" style=\"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody>";
            bodyContent += "<tr>";
            bodyContent += "<td align = \"center\" valign=\"top\" bgcolor=\"#F0F0F0\" style=\"border-collapse:collapse;background-color:#f0f0f0\">";
            bodyContent += "<table border = \"0\" width=\"600\" cellpadding=\"0\" cellspacing=\"0\" class=\"m_-6851641861096144865container\" style=\"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0;max-width:100%!important;border:1px solid #ccc\">";
            bodyContent += "<tbody>";
            bodyContent += "<tr>";
            bodyContent += "<td class= \"m_ -6851641861096144865pre-header\" bgcolor= \"#2CA01C\" style= \"height:4px;border-collapse:collapse;background-color:#2ca01c \"></td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td align = \"left \" class= \"m_-6851641861096144865header\" bgcolor= \"#F4F5F8\" style= \"border-collapse:collapse;padding:25px\">";
            bodyContent += "<table style = \"font -family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += " <tbody>";
            bodyContent += " <tr>";
            bodyContent += "<td style= \"border -collapse:collapse\">";
            bodyContent += "<table style = \"font -family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody>";
            bodyContent += "<tr>";
            bodyContent += "<td style= \"padding -right:20px;border-collapse:collapse\">";
            bodyContent += "<img src = \"https://ci3.googleusercontent.com/proxy/8ZiPoWD6Pvp0lVTytTjxgPpcz_c1-tWM9GR4cWHTkogLt4XqFtqK9TzOKrYO6A1q8rNN19mkLWM7auK78uuAWH2CQK9NvhVtGeSZBpMvqvVYH3W9q1OhDf7aJqz2jKNiGIdmQa2ZZ9NH0h0=s0-d-e1-ft#http://static.cdn.responsys.net/i2/responsysimages/content/inthealth/icon-success.png\" class= \"CToWUd\"></td>";
            bodyContent += "<td style = \"border -collapse:collapse\"><h1 class= \"m_-6851641861096144865heading\" style= \"font-family:Arial,Verdana,sans-serif;font-size:20px;margin:0\">";
            bodyContent += subject + "</h1></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody>";
            bodyContent += "</table>";
            bodyContent += "</td>";
            bodyContent += " </tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += " </tr>";
            bodyContent += "<tr>";
            bodyContent += "  <td align = \"left \" class= \"m_-6851641861096144865content\" bgcolor= \"#ffffff \" style= \"border-collapse:collapse;padding:25px 25px 50px 25px;border-top:1px solid #e8e8ea\"><table class= \"m_-6851641861096144865row \" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0 \">";
            bodyContent += "<tbody><tr width = \"480\">";
            bodyContent += "<td style= \"border -collapse:collapse\">";
            bodyContent += "<p style = \"font -size:13px;margin-bottom:5px;margin:0\"> ";
            bodyContent += " Your  " + emailType + " still has not been received as of the date of " + DateTime.Now.ToShortDateString() + " of this notice.";
            bodyContent += " As a result and according to your Tenant, a late charge has been added to your total balance based on contract. ";
            bodyContent += " This entire balance must be paid immediately! This is a serious matter and your urgent attenion is required.";
            bodyContent += " Failure to act promptly may lead to eviction proceedings.";
            bodyContent += " If eviction is sought, you maybe responsible for additional charges, such as court and attorney fees, and your credit rating coulbe be affected. \n\n ";
            bodyContent += " Please make deposit to landlord bank account. Please write down your property name and unit name on the deposit slip. \n\n";
            bodyContent += "</p><p style = \"font -size:13px;margin:0\"> Here are your transaction details:</p>  ";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table height = \"30\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td height = \"30\" style= \"height:30px;border-collapse:collapse\"></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table style = \"font -family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td>";
            bodyContent += "<table align= \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td><strong>Paid to</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td> " + payto + "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table align = \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td><strong> Address</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px \">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td> " + addresss + " </td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table align = \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td><strong> Invoice No</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px \">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td> " + invoiceno + " </td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table align= \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td><strong> " + emailType + "</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px \">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td> " + dueDate.ToShortDateString() + "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table align = \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td colspan=\"2\"><strong> Payment method</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px \">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td> " + paymentmethod + "</td> ";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table align = \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td><strong> Due amount</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px\"> ";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td>" + invoicebalance + "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr><td><table align = \"left \" width=\"260\"><tbody><tr><td><strong> Invoice total</strong></td></tr></tbody></table><table align = \"left \" width= \"260\" style= \"padding-bottom:20px\"><tbody><tr><td>" + invoicetotal + "</td></tr></tbody></table></td></tr> ";
            bodyContent += "<tr><td><table align = \"left \" width=\"260\"><tbody><tr><td><strong> Invoice balance</strong></td></tr></tbody></table><table align = \"left \" width= \"260\"><tbody><tr><td><b>" + invoicebalance + "</b></td></tr></tbody></table></td></tr>";
            bodyContent += "</tbody>";
            bodyContent += "</table>";
            bodyContent += "<table class= \"m_-6851641861096144865spacer30\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0;height:30px\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"border-collapse:collapse;height:10px\"></td> ";
            bodyContent += "</tr>";
            bodyContent += "</tbody>";
            bodyContent += "</table>";
            bodyContent += "<table class= \"m_-6851641861096144865row\" style = \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"border-collapse:collapse\"> ";
            bodyContent += "<p style= \"font-size:13px;display:block;margin:0\">Funds will be apply to your in 1-2 business days. </p>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody>";
            bodyContent += "</table>";
            bodyContent += "<table height = \"40\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody>";
            bodyContent += "<tr>";
            bodyContent += "<td style= \"border-collapse:collapse\"></td> ";
            bodyContent += "</tr>";
            bodyContent += "</tbody>";
            bodyContent += "</table>";
            bodyContent += "<table class= \"m_-6851641861096144865row\" width = \"100%\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td>";
            bodyContent += "<table height = \"10\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0;height:10px\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"border-collapse:collapse\"></td> ";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table class= \"m_-6851641861096144865row\" style = \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"border-collapse:collapse\"> ";
            bodyContent += "<p style= \"font-size:13px;margin:0\"></p>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"center\" border = \"0\" width= \"600\" cellpadding= \"0\" cellspacing= \"0\" class= \"m_-6851641861096144865container\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;color:#333333;border-spacing:0;max-width:100%!important\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"border-collapse:collapse\"> ";
            bodyContent += "<table class= \"m_-6851641861096144865spacer15\" style = \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"height:5px;border-collapse:collapse\"></td> ";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td align= \"left \" style= \"border-collapse:collapse;padding:5px 0 10px 0 \">";
            bodyContent += "<img src = \"https://ci5.googleusercontent.com/proxy/2LwBw-s5RgFNAxL_lLoA_3JBQnJgGeMBPxmybpCNJf1XC7MQsqAmE0bX6y1PFVNfsPl0Oq8tM_N0KMIrC6a1YM9Hi4Ugeza0_RCXBfzyHil7GLAsh7FnKkW4COjuBU8wc4NWrLTnQPSpz_IHb7j-ERECLP-K7bkOtGfdQqbdIG4j393CEof7Eco-9Q=s0-d-e1-ft#https://plugin-qbo.intuit.com/brand/0.0.1/product-specific-brand/assets/quickbooks-payments/logos/qbeinvoiceemail.png\" height = \"35\" alt= \"QuickBooks\" class= \"CToWUd\">";
            bodyContent += "</td>";
            bodyContent += "<td align = \"right\" style= \"border-collapse:collapse;padding:15px 0 10px 0\">";
            bodyContent += "<p style = \"margin:0;font-size:11px\">© Sunrise Property Management.All rights reserved.";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += " </tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table width = \"100% \" border= \"0\" cellspacing= \"0\" cellpadding= \"0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td align = \"center\" valign= \"top \" bgcolor= \"#ffffff\">";
            bodyContent += "<table border = \"0\" cellspacing= \"0\" cellpadding= \"0\" class= \"m_-6851641861096144865width:320px m_-6851641861096144865!important;\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td align = \"center\">";
            bodyContent += "<span id= \"m_-6851641861096144865traceid\" style= \"color:#fafafa;font-size:10px;font-family:'Helvetica Neue',Helvetica,Arial,sans-serif\">TraceId:19b03360-85c4-428a-<wbr>a7c9-838e299bd36c</span>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table><table cellpadding = \"0\" cellspacing= \"0\" style= \"border:0px;padding:0px;margin:0px;display:none;float:left\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td height = \"1 \" style= \"font-size:1px;line-height:1px;padding:0px\">";
            bodyContent += "<br><img height = \"1 \" width= \"1 \" src= \"https://ci6.googleusercontent.com/proxy/Z2ekg8ReHuor7AJ_ozs3PyTxuzV9Bjdi0tXfGJVER3NZ7UD-uTyyLj0ajEmWN6ZTZfXTS4TD0duwYjQWXZ9c5Y2_mKHv-ErrLcpsikN8muvehId4ZtYL_3XedJrg22F1uUig6Q6Xz0jKwbFmUolrE-YqraFxTsqTz9eRnX4iH3N3JORSvyyAcy52bzra_qNvP_E1SbJaQFquLRNGbuJic-G1jSLErNWVZ5TKKo8yBgB10oiMKVKX8BohNs-Sc1ygK8UX_vkHUxOykZH5LW156_675TzqmffF4VLPeA9A6w=s0-d-e1-ft#http://e.intuit.com/pub/as?_ri_=X0Gzc2X%3DYQpglLjHJlTQGlvcCo7fK590NuApezfXirvf3BafjlkSIe9s5zb94EzfbzaRyr010nVXHkMX%3Dw&amp;_ei_=EolaGGF4SNMvxFF7KucKuWP8VdPXnxY9MM_h52BUS1WdrS2uqLYfJnaZAs3wLQ86z-QpJIxXYLY. \" class= \"CToWUd\">";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table></div>";
            bodyContent += "</body>";
            bodyContent += "</html>";

            return bodyContent;
        }

        private static string ComposeLeaseTemplate(string subject, Tenant tenant, string landlord, int emailType, string invoicebalance)
        {
            string content = "";
            if (emailType == (int)Helpers.Helpers.EmailType.LeaseTermination)
            {
                content = "Your lease has been terminated. Below are your previous lease terms";
            }
            else
            {
                content = "Below are your lease terms";
            }
            string bodyContent = "<html xmlns = \"http://www.w3.org/1999/xhtml \">";
            bodyContent += "<head>";
            bodyContent += "<title>Sunrise Property Management System Lease Email</title>";
            bodyContent += "</head>";
            bodyContent += "<body>";
            bodyContent += "<div>";
            bodyContent += "<table class=\"m_-6851641861096144865outer_container\" border=\"0\" width=\"100%\" height=\"100%\" cellpadding=\"10\" cellspacing=\"10\" bgcolor=\"#F0F0F0\" style=\"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody>";
            bodyContent += "<tr>";
            bodyContent += "<td align = \"center\" valign=\"top\" bgcolor=\"#F0F0F0\" style=\"border-collapse:collapse;background-color:#f0f0f0\">";
            bodyContent += "<table border = \"0\" width=\"600\" cellpadding=\"0\" cellspacing=\"0\" class=\"m_-6851641861096144865container\" style=\"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0;max-width:100%!important;border:1px solid #ccc\">";
            bodyContent += "<tbody>";
            bodyContent += "<tr>";
            bodyContent += "<td class= \"m_ -6851641861096144865pre-header\" bgcolor= \"#2CA01C\" style= \"height:4px;border-collapse:collapse;background-color:#2ca01c \"></td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td align = \"left \" class= \"m_-6851641861096144865header\" bgcolor= \"#F4F5F8\" style= \"border-collapse:collapse;padding:25px\">";
            bodyContent += "<table style = \"font -family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += " <tbody>";
            bodyContent += " <tr>";
            bodyContent += "<td style= \"border -collapse:collapse\">";
            bodyContent += "<table style = \"font -family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody>";
            bodyContent += "<tr>";
            bodyContent += "<td style= \"padding -right:20px;border-collapse:collapse\">";
            bodyContent += "<img src = \"https://ci3.googleusercontent.com/proxy/8ZiPoWD6Pvp0lVTytTjxgPpcz_c1-tWM9GR4cWHTkogLt4XqFtqK9TzOKrYO6A1q8rNN19mkLWM7auK78uuAWH2CQK9NvhVtGeSZBpMvqvVYH3W9q1OhDf7aJqz2jKNiGIdmQa2ZZ9NH0h0=s0-d-e1-ft#http://static.cdn.responsys.net/i2/responsysimages/content/inthealth/icon-success.png\" class= \"CToWUd\"></td>";
            bodyContent += "<td style = \"border -collapse:collapse\"><h1 class= \"m_-6851641861096144865heading\" style= \"font-family:Arial,Verdana,sans-serif;font-size:20px;margin:0\">";
            bodyContent += subject + "</h1></td></tr></tbody></table></td></tr></tbody></table></td></tr>";
            bodyContent += "<tr>";
            bodyContent += "<td align = \"left \" class= \"m_-6851641861096144865content\" bgcolor= \"#ffffff \" style= \"border-collapse:collapse;padding:25px 25px 50px 25px;border-top:1px solid #e8e8ea\"><table class= \"m_-6851641861096144865row \" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0 \">";
            bodyContent += "<tbody><tr width = \"480\">";
            bodyContent += "<td style= \"border -collapse:collapse\">";
            bodyContent += "<p style = \"font -size:13px;margin:0\"> " + content + "</p> ";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table height = \"30\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td height = \"30\" style= \"height:30px;border-collapse:collapse\"></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table style = \"font -family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td>";
            bodyContent += "<table align= \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td><strong>Property Address</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td> " + tenant.Address + "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table align = \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td><strong> Landlord</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px \">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td> " + landlord + " </td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table align = \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td><strong> Tenant </strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px \">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td>" + tenant.FirstName + " " + tenant.LastName + "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table align = \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td><strong> Lease Start Day</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px \">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td> " + tenant.StartDate.ToString("D") + " </td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table align= \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td><strong> Lease Expiration </strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px \">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td> " + tenant.StartDate.AddMonths(tenant.LeaseTerm).ToString("D") + "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table align = \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td colspan=\"2\"><strong> Agreed Security Deposit </strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px \">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td> " + tenant.SecurityDeposit.ToString("C") + "</td> ";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table align = \"left \" width=\"260\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td colspan=\"2\"><strong> Paid Security Deposit</strong></td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px\"> ";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td>" + tenant.PaidSecurityDeposit.ToString("C") + "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr><td><table align = \"left \" width=\"260\"><tbody><tr><td><strong> Monthly Rent</strong></td></tr></tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px\"><tbody><tr><td>" + tenant.MonthlyPayment.ToString("C") + "</td></tr></tbody></table></td></tr> ";
            bodyContent += "<tr><td><table align = \"left \" width=\"260\"><tbody><tr><td><strong>Current Balance</strong></td></tr></tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px\"><tbody><tr><td>" + invoicebalance + "</td></tr></tbody></table></td></tr> ";
            bodyContent += "<tr><td><table align = \"left \" width=\"260\"><tbody><tr><td><strong> Annotation</strong></td></tr></tbody></table>";
            bodyContent += "<table align = \"left \" width= \"260\" style= \"padding-bottom:20px\"><tbody><tr><td>" + tenant.Note + "</td></tr></tbody></table></td></tr> ";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody>";
            bodyContent += "</table>";
            bodyContent += "<table class= \"m_-6851641861096144865spacer30\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0;height:30px\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"border-collapse:collapse;height:10px\"></td> ";
            bodyContent += "</tr>";
            bodyContent += "</tbody>";
            bodyContent += "</table>";
            bodyContent += "<table class= \"m_-6851641861096144865row\" style = \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"border-collapse:collapse\"> ";
            bodyContent += "<p style= \"font-size:13px;display:block;margin:0\">Welcome to be tenant with our company. </p>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody>";
            bodyContent += "</table>";
            bodyContent += "<table height = \"40\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody>";
            bodyContent += "<tr>";
            bodyContent += "<td style= \"border-collapse:collapse\"></td> ";
            bodyContent += "</tr>";
            bodyContent += "</tbody>";
            bodyContent += "</table>";
            bodyContent += "<table class= \"m_-6851641861096144865row\" width = \"100%\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td>";
            bodyContent += "<table height = \"10\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0;height:10px\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"border-collapse:collapse\"></td> ";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td>";
            bodyContent += "<table class= \"m_-6851641861096144865row\" style = \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"border-collapse:collapse\"> ";
            bodyContent += "<p style= \"font-size:13px;margin:0\"></p>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table align = \"center\" border = \"0\" width= \"600\" cellpadding= \"0\" cellspacing= \"0\" class= \"m_-6851641861096144865container\" style= \"font-family:Arial,Helvetica,Verdana,sans-serif;color:#333333;border-spacing:0;max-width:100%!important\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"border-collapse:collapse\"> ";
            bodyContent += "<table class= \"m_-6851641861096144865spacer15\" style = \"font-family:Arial,Helvetica,Verdana,sans-serif;font-size:13px;color:#333333;border-spacing:0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td style = \"height:5px;border-collapse:collapse\"></td> ";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "<tr>";
            bodyContent += "<td align= \"left \" style= \"border-collapse:collapse;padding:5px 0 10px 0 \">";
            bodyContent += "<img src = \"https://ci5.googleusercontent.com/proxy/2LwBw-s5RgFNAxL_lLoA_3JBQnJgGeMBPxmybpCNJf1XC7MQsqAmE0bX6y1PFVNfsPl0Oq8tM_N0KMIrC6a1YM9Hi4Ugeza0_RCXBfzyHil7GLAsh7FnKkW4COjuBU8wc4NWrLTnQPSpz_IHb7j-ERECLP-K7bkOtGfdQqbdIG4j393CEof7Eco-9Q=s0-d-e1-ft#https://plugin-qbo.intuit.com/brand/0.0.1/product-specific-brand/assets/quickbooks-payments/logos/qbeinvoiceemail.png\" height = \"35\" alt= \"QuickBooks\" class= \"CToWUd\">";
            bodyContent += "</td>";
            bodyContent += "<td align = \"right\" style= \"border-collapse:collapse;padding:15px 0 10px 0\">";
            bodyContent += "<p style = \"margin:0;font-size:11px\">© Sunrise Property Management.All rights reserved.";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += " </tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "<table width = \"100% \" border= \"0\" cellspacing= \"0\" cellpadding= \"0\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td align = \"center\" valign= \"top \" bgcolor= \"#ffffff\">";
            bodyContent += "<table border = \"0\" cellspacing= \"0\" cellpadding= \"0\" class= \"m_-6851641861096144865width:320px m_-6851641861096144865!important;\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td align = \"center\">";
            bodyContent += "<span id= \"m_-6851641861096144865traceid\" style= \"color:#fafafa;font-size:10px;font-family:'Helvetica Neue',Helvetica,Arial,sans-serif\">TraceId:19b03360-85c4-428a-<wbr>a7c9-838e299bd36c</span>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table>";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table><table cellpadding = \"0\" cellspacing= \"0\" style= \"border:0px;padding:0px;margin:0px;display:none;float:left\">";
            bodyContent += "<tbody><tr>";
            bodyContent += "<td height = \"1 \" style= \"font-size:1px;line-height:1px;padding:0px\">";
            bodyContent += "<br><img height = \"1 \" width= \"1 \" src= \"https://ci6.googleusercontent.com/proxy/Z2ekg8ReHuor7AJ_ozs3PyTxuzV9Bjdi0tXfGJVER3NZ7UD-uTyyLj0ajEmWN6ZTZfXTS4TD0duwYjQWXZ9c5Y2_mKHv-ErrLcpsikN8muvehId4ZtYL_3XedJrg22F1uUig6Q6Xz0jKwbFmUolrE-YqraFxTsqTz9eRnX4iH3N3JORSvyyAcy52bzra_qNvP_E1SbJaQFquLRNGbuJic-G1jSLErNWVZ5TKKo8yBgB10oiMKVKX8BohNs-Sc1ygK8UX_vkHUxOykZH5LW156_675TzqmffF4VLPeA9A6w=s0-d-e1-ft#http://e.intuit.com/pub/as?_ri_=X0Gzc2X%3DYQpglLjHJlTQGlvcCo7fK590NuApezfXirvf3BafjlkSIe9s5zb94EzfbzaRyr010nVXHkMX%3Dw&amp;_ei_=EolaGGF4SNMvxFF7KucKuWP8VdPXnxY9MM_h52BUS1WdrS2uqLYfJnaZAs3wLQ86z-QpJIxXYLY. \" class= \"CToWUd\">";
            bodyContent += "</td>";
            bodyContent += "</tr>";
            bodyContent += "</tbody></table></div>";
            bodyContent += "</body>";
            bodyContent += "</html>";

            return bodyContent;
        }

        private static string ComposeVerificationOfAddressAndResidency(string subject, Tenant tenant, string company, string landlord)
        {
            string content = "To Whom It May Concern,  <br><br>";
            content += "I am writing to verify that " + tenant.FirstName + " " + tenant.LastName
                + ", currently reside as tenants on our property located at " + tenant.Address
                + ".  <br><br> I also confirm that they have been living at this address since " + tenant.StartDate
                + ". Their lease expires on " + tenant.StartDate.AddMonths(tenant.LeaseTerm) + ".  <br><br>"
                + "If I can provide any further information, do not hesitate to contact me by phone or in writing, at your convenience.  <br><br><br><br>"
                + "Your Sincerely,  <br><br>"
                + landlord + ",  <br>"
                + company + ",  <br><br>"
                + DateTime.Now.ToShortDateString().ToString() + "<br>";
            string signature = "<img src = \"https://ci3.googleusercontent.com/proxy/8ZiPoWD6Pvp0lVTytTjxgPpcz_c1-tWM9GR4cWHTkogLt4XqFtqK9TzOKrYO6A1q8rNN19mkLWM7auK78uuAWH2CQK9NvhVtGeSZBpMvqvVYH3W9q1OhDf7aJqz2jKNiGIdmQa2ZZ9NH0h0=s0-d-e1-ft#http://static.cdn.responsys.net/i2/responsysimages/content/inthealth/icon-success.png\" class= \"CToWUd\">";
            string bodyContent = "<html xmlns = \"http://www.w3.org/1999/xhtml \">";
            bodyContent += "<head><title>Letter of Verification of Address and Residency</title></head>";
            bodyContent += "<body>";
            bodyContent += "<div>";
            bodyContent += "<img src = \"https://ci3.googleusercontent.com/proxy/8ZiPoWD6Pvp0lVTytTjxgPpcz_c1-tWM9GR4cWHTkogLt4XqFtqK9TzOKrYO6A1q8rNN19mkLWM7auK78uuAWH2CQK9NvhVtGeSZBpMvqvVYH3W9q1OhDf7aJqz2jKNiGIdmQa2ZZ9NH0h0=s0-d-e1-ft#http://static.cdn.responsys.net/i2/responsysimages/content/inthealth/icon-success.png\" class= \"CToWUd\">";
            bodyContent += "</div>";
            bodyContent += "<div><h1>";
            bodyContent += subject + "</h1></div>";
            bodyContent += "<div>" + content + signature + "<div>";
            bodyContent += "</body>";
            bodyContent += "</html>";

            return bodyContent;
        }

        private static string ComposeExpenseInvoice(OperationRecord operation, string propertyAddress, string CompanyName, string contractorName, string contractorAddress)
        {
         //   string signature = "<img src = \"https://ci3.googleusercontent.com/proxy/8ZiPoWD6Pvp0lVTytTjxgPpcz_c1-tWM9GR4cWHTkogLt4XqFtqK9TzOKrYO6A1q8rNN19mkLWM7auK78uuAWH2CQK9NvhVtGeSZBpMvqvVYH3W9q1OhDf7aJqz2jKNiGIdmQa2ZZ9NH0h0=s0-d-e1-ft#http://static.cdn.responsys.net/i2/responsysimages/content/inthealth/icon-success.png\" class= \"CToWUd\">";
            string bodyContent = "<html xmlns = \"http://www.w3.org/1999/xhtml \">";
            bodyContent += "<head><title>Sunrise Management Inovice</title></head>";
            bodyContent += "<body>";
            //bodyContent += "<div>";
            //bodyContent += "<img src = \"https://ci3.googleusercontent.com/proxy/8ZiPoWD6Pvp0lVTytTjxgPpcz_c1-tWM9GR4cWHTkogLt4XqFtqK9TzOKrYO6A1q8rNN19mkLWM7auK78uuAWH2CQK9NvhVtGeSZBpMvqvVYH3W9q1OhDf7aJqz2jKNiGIdmQa2ZZ9NH0h0=s0-d-e1-ft#http://static.cdn.responsys.net/i2/responsysimages/content/inthealth/icon-success.png\" class= \"CToWUd\">";
            //bodyContent += "</div>";
            //bodyContent += "<div><h1>" + contractorName + "</h1></div>";
            //bodyContent += "<div><h1>" + contractorAddress + "</h1></div>";
            //bodyContent += "<div><h3>INVOICE</h3><div>";
            //bodyContent += "<div><h3>BILL TO                            INOVICE#" + operation.ID  + "</h3><div>";
            //bodyContent += "<div><h3>" + CompanyName + "                    DATE  " + operation.DueDate + "</h3><div>";
            //bodyContent += "<hr>";
            //bodyContent += "<div><h3>PROJECT: + " + propertyAddress + "</h3><div>";
            //bodyContent += "<div><h3>ACTIVITY: +                     AMOUNT </h3><div>";
            //bodyContent += "<div>"+ operation.CategoryName+ " " + operation.Memo + "<div>";
            //bodyContent += "<div>" + operation.DueAmount + " " + operation.Memo + "<div>";
            bodyContent += "</body>";
            bodyContent += "</html>";

            return bodyContent;
        }
    }
}