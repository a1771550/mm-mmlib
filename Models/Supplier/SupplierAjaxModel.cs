using MMCommonLib.BaseModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLib.Models.Supplier
{
    public class SupplierAjaxModel
    {
        public List<SupplierModel> Vendors { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get { return int.Parse(ConfigurationManager.AppSettings["SupplierPageSize"]); } }
        public string Keyword { get; set; }
        public int RecordCount;
    }
}
