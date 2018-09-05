using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PropertyManagement.Models;

namespace PropertyManagement.ViewModels
{
    public class CompanyIndexVM: AddCompanyVM
    {
        public List<Company> Companys { get; set; }
    }
}