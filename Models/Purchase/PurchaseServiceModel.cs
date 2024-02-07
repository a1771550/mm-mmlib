using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLib.Models.Purchase
{
    public class PurchaseServiceModel:MMDAL.PurchaseService
    {
        public string AccountName { get; set; }
        public string AccountNo { get; set; }
    }

}
