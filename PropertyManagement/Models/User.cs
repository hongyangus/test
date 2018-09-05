using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PropertyManagement.Models
{   
    public class User
    {
        public User()
        {
            this.Companies = new HashSet<CheckBoxListItem>();
            this.Roles = new HashSet<CheckBoxListItem>();
        }
        public int ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string CellPhone { get; set; }
        public string HomePhone { get; set; }
        public string WorkPhone { get; set; }
        public string EmailAddress { get; set; }
        public DateTime DOB { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string SSN { get; set; }
        public string WebUrl { get; set; }
        public int StatusID { get; set; }
        public string Status { get; set; }
        public string Company { get; set; }
        public int CompanyID { get; set; }
        public double Amount { get; set; }
        public virtual ICollection<CheckBoxListItem> Roles { get; set; }
        public virtual ICollection<CheckBoxListItem> Companies { get; set; }
    }
}