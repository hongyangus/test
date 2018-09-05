using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PropertyManagement.Models
{
    public class Unit
    {
        public int UnitID { get; set; }
        public string UnitName { get; set; }
        public int PropertyID { get; set; }
        public string Note { get; set; }
    }
}