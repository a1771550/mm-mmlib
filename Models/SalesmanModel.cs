using MMDAL;
using MMLib.Models.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace MMLib.Models
{
    public class SalesmanModel:UserModel
    {
        public List<long> AssignedContactIds { get; set; }
        public string MaxSalesCode { get; set; }
        //public CrmSalesGroup SalesGroup { getPG; set; }
        //public Dictionary<int, string> DicSalesGroup { getPG; set; }
        public List<ManagerModel> Managers { get; set; }
        public SalesmanModel()
        {
            
        }
        public SalesmanModel(bool forAdd) : this()
        {
            if (forAdd)
            {
                using (var context = new MMDbContext())
                {
                    MaxSalesCode = context.GetMaxUserCode("sales").FirstOrDefault();
                    var resultString = Regex.Match(MaxSalesCode, @"\d+").Value;
                    var icode = int.Parse(resultString) + 1;
                    if (icode < 10)
                    {
                        resultString = icode.ToString().PadLeft(2, '0'); //RIGHT HERE!!!
                    }
                    MaxSalesCode = String.Concat("sales", resultString);
                }
            }
        }
    }
}
