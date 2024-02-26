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
		public HashSet<long> CheckOutIds_Purchase { get; set; }
		public HashSet<long> CheckOutIds_PayLn { get; set; }
        public DateTime FrmToDate { get; set; }
        public DateTime ToDate { get; set; }
        public string SelectedLocation { get; set; }
        public List<PurchaseModel> PurchaseModels { get; set; }
        public bool includeUploaded { get; set; }
      
        public List<SupplierModel> Supplierlist { get; set; }
      
		public DataTransferModel()
        {         
            CheckOutIds_Supplier = new List<long>();
            Supplierlist = new List<SupplierModel>();
            CheckOutIds_Purchase = new HashSet<long>();
            CheckOutIds_PayLn = new HashSet<long>();
        }
    }
}
