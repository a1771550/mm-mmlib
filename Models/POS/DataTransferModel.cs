using MMLib.Models.POS.MYOB;
using MMLib.Models.Purchase;
using MMLib.Models.Supplier;
using System;
using System.Collections.Generic;

namespace MMLib.Models
{
    public class DataTransferModel
    {
		public PurchaseModel Purchase { get; set; } 

        public List<long> CheckOutIds_Supplier { get; set; }
		public HashSet<long> CheckOutIds_InvoiceLine { get; set; }
		public HashSet<long> CheckOutIds_PayLine { get; set; }
        public DateTime FrmToDate { get; set; }
        public DateTime ToDate { get; set; }
        public string SelectedLocation { get; set; }
        public List<PurchaseModel> PurchaseModels { get; set; }
        public bool includeUploaded { get; set; }
      
        public List<MyobSupplierModel> Supplierlist { get; set; }
      
		public DataTransferModel()
        {         
            CheckOutIds_Supplier = new List<long>();
            Supplierlist = new List<MyobSupplierModel>();
            CheckOutIds_InvoiceLine = new HashSet<long>();
            CheckOutIds_PayLine = new HashSet<long>();
        }
    }
}
