using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PropertyManagement.Models;

namespace PropertyManagement.ViewModels.User
{
    public class AddUserVM
    {
        public int UserID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string CellPhone { get; set; }
        public string HomePhone { get; set; }
        public string WorkPhone { get; set; }
        public string EmailAddress { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string SSN { get; set; }
        public string WebUrl { get; set; }
        public int StatusID { get; set; }
        public string Status { get; set; }
        public List<CheckBoxListItem> Roles { get; set; }
        public List<CheckBoxListItem> Companies { get; set; }
        public IEnumerable<SelectListItem> AllStatus { get; set; }

        public AddUserVM()
        {
            Roles = new List<CheckBoxListItem>();
            Companies = new List<CheckBoxListItem>();
        }
    }
}
