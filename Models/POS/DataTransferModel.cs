using MMLib.Models.Purchase;
using MMLib.Models.Supplier;
using System;
using System.Collections.Generic;

namespace MMLib.Models
{
    public class DataTransferModel
    {
		public PurchaseModel Purchase { get; set; }
		public List<DeviceModel> DeviceList { get; set; }       

        public List<long> CheckOutIds_Supplier { get; set; }
        public List<long> CheckOutIds_Customer { get; set; }
        public List<long> CheckOutIds_Item { get; set; }
        public List<long> CheckOutIds_Stock { get; set; }
		public HashSet<long> CheckOutIds_Purchase { get; set; }
		public HashSet<long> CheckOutIds_SupPayLn { get; set; }
        public DateTime FrmToDate { get; set; }
        public DateTime ToDate { get; set; }
        public string SelectedLocation { get; set; }
        public List<PurchaseModel> PurchaseModels { get; set; }
        public bool includeUploaded { get; set; }

    
        public HashSet<string> ItemCodes { get; set; }
        public List<SupplierModel> Supplierlist { get; set; }
     
        public HashSet<long> CheckOutIds_WS { get; set; }
        public string Device { get; set; }
        public HashSet<long> RetailCheckOutIds { get; set; }
		public HashSet<string> CheckOutIds_Journal { get; set; }
		public DataTransferModel()
        {
            DeviceList = new List<DeviceModel>();
            CheckOutIds_Customer = new List<long>();
            CheckOutIds_Item = new List<long>();
            CheckOutIds_Stock = new List<long>();
            CheckOutIds_Supplier = new List<long>();
            Supplierlist = new List<SupplierModel>();
            CheckOutIds_Journal = new HashSet<string>();
        }
    }
}
