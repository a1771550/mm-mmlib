using CommonLib.Models.MYOB;
using MMLib.Models.MYOB;
using System;
using System.Collections.Generic;

namespace MMLib.Models.POS.MYOB
{
    public class MyobSupplierModel:MMDAL.Supplier
    {
        public List<AddressModel> AddressList { get; set; }
        public MyobTerms Terms { get; set; }
        //public string PaymentTermsDesc { get; set; }
        //public int CurrencyID { get; set; }
        //public string TaxIDNumber { get; set; }
        //public Nullable<int> TaxCodeID { get; set; }
    }
}
