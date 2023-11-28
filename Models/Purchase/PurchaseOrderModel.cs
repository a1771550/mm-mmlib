using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLib.Models.Purchase
{
    public class PurchaseOrderModel:MMDAL.Purchase
    {
        public string itmCode { get; set; }
        public string itmDesc { get; set; }
        public string itmName { get; set; }
        public string supName { get; set; }
        public string ItemNameDesc { get; set; }
        public string supContact { get; set; }
        public string supFirstName { get; set; }    
        public bool? IsApproved { get; set; }
        public string ApprovedBy { get; set; }
        public bool? IsRejected { get; set; }
        public string RejectedBy { get; set; }
        public string Reason { get; set; }
        public bool? PassedToManager { get; set; }
        public bool? EmailNotified { get; set; }
    }
}
