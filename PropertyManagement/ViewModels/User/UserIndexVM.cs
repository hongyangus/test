using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PropertyManagement.Models;

namespace PropertyManagement.ViewModels.User
{
    public class UserIndexVM
    {
          public List<PropertyManagement.Models.User> Users { get; set; }
    }
}
