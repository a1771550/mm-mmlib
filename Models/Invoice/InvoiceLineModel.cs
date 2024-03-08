using CommonLib.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using RestSharp;
using MMLib.Models.Purchase;
using MMDAL;

namespace MMLib.Models.Invoice
{
    public class InvoiceLineModel:InvoiceLine
    {
       
        public bool Ready4ABSS { get { return AccountID != null && (int)AccountID > 0; } }
       
        public List<string> FileList { get; set; }
       
        public string AmountDisplay { get { return ilAmt == null ? "" : CommonHelper.FormatNumber((decimal)ilAmt, false); } }

       
        public string DescDisplay
        {
            get
            {
                int maxlength = int.Parse(ConfigurationManager.AppSettings["MaxDescRemarkDisplayLength"]);
                return ilDesc != null && ilDesc.Length > maxlength ? string.Concat(ilDesc.Substring(0, maxlength), "...") : ilDesc ?? string.Empty;
            }
        }

       
        public string RemarkDisplay
        {
            get
            {
                int maxlength = int.Parse(ConfigurationManager.AppSettings["MaxDescRemarkDisplayLength"]);
                return Remark != null && Remark.Length > maxlength ? string.Concat(Remark.Substring(0, maxlength), "...") : Remark ?? string.Empty;
            }
        }

       
     

        
        public string pstCode { get; set; }

       
        public string PurchaseDate4ABSS { get { return pstPurchaseDate != null ? CommonHelper.FormatDate4ABSS((DateTime)pstPurchaseDate, dateformat) : string.Empty; } }

       
     

        
        public string DeliveryStatus { get; set; }

       
        public string AccountNumber { get; set; }

      
        public string SupplierName { get; set; }

      
        public double Amount4Abss { get { return ilAmt == null ? 0 : Convert.ToDouble((decimal)ilAmt); } }

       
        public double IncTaxAmount { get { return Amount4Abss; } }

       
       
       
        public DateTime pstPurchaseDate { get; set; }
       
        public string dateformat { get; set; }
    }

}
