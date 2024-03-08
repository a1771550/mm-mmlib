using CommonLib.Helpers;
using CsvHelper.Configuration.Attributes;
using System;

namespace MMLib.Models.Invoice
{
    public class AbssInvoiceLine
    {
        [Name("Co./Last Name")]
        [Index(0)]
        public string SupplierName { get; set; }

        [Name("First Name")]
        [Index(1)]
        public string FirstName { get; set; }


        [Name("Addr 1 - Line 1")]
        [Index(2)]
        public string Addr1Line1 { get; set; }

        [Name("       - Line 2")]
        [Index(3)]
        public string Addr1Line2 { get; set; }

        [Name("       - Line 3")]
        [Index(4)]
        public string Addr1Line3 { get; set; }

        [Name("       - Line 4")]
        [Index(5)]
        public string Addr1Line4 { get; set; }

        [Name("Inclusive")]
        [Index(6)]
        public string Inclusive { get { return string.Empty; } }

        [Name("Purchase #")]
        [Index(7)]
        public string pstCode { get; set; }

        [Name("Date")]
        [Index(8)]
        public string Date4ABSS
        {
            get
            {               
               return pstPurchaseDate != null ? CommonHelper.FormatDate4ABSS((DateTime)pstPurchaseDate, dateformat) : string.Empty;              
            }
        }

        [Name("Supplier Invoice #")]
        [Index(9)]
        public string InvoiceId { get; set; }

        [Name("Deliver Via")]
        [Index(10)]
        public string DeliverVia { get { return string.Empty; } }

        [Name("Delivery Status")]
        [Index(11)]
        public string DeliveryStatus { get { return "A"; } }

        [Name("Description")]
        [Index(12)]
        public string ilDesc { get; set; }

        [Name("Account #")]
        [Index(13)]
        public string AccountNumber { get; set; }

        [Name("Amount")]
        [Index(14)]
        public double Amount4Abss { get { return ilAmt == null ? 0 : Convert.ToDouble((decimal)ilAmt); } }

        [Name("Inc-Tax Amount")]
        [Index(15)]
        public double IncTaxAmt { get { return Amount4Abss; } }

        [Name("Job")]
        [Index(16)]
        public string Job { get; set; }

        [Name("Comment")]
        [Index(17)]
        public string Comment { get { return string.Empty; } }

        [Name("Journal Memo")]
        [Index(18)]
        public string JournalMemo { get { return string.Empty; } }

        [Name("Delivery Date")]
        [Index(19)]
        public string DeliveryDate { get { return string.Empty; } }

        [Name("Tax Code")]
        [Index(20)]
        public string TaxCode { get { return string.Empty; } }

        [Name("Non-Tax Amount")]
        [Index(21)]
        public double? NonTaxAmt { get { return 0; } }

        [Name("Tax Amount")]
        [Index(22)]
        public double? TaxAmt { get { return null; } }

        [Name("Import Duty Amount")]
        [Index(23)]
        public double? ImportDutyAmt { get { return 0; } }

        [Name("Freight Amount")]
        [Index(24)]
        public double? FreightAmt { get { return null; } }

        [Name("Inc-Tax Freight Amount")]
        [Index(25)]
        public double? IncTaxFreightAmt { get { return null; } }

        [Name("Freight Tax Code")]
        [Index(26)]
        public string FreightTaxCode { get { return null; } }

        [Name("Freight Non-Tax Amount")]
        [Index(27)]
        public double? FreightNonTaxAmt { get { return 0; } }

        [Name("Freight Tax Amount")]
        [Index(28)]
        public double? FreightTaxAmt { get { return null; } }

        [Name("Freight Import Duty Amount")]
        [Index(29)]
        public double? FreightImportDutyAmt { get { return 0; } }

        [Name("Purchase Status")]
        [Index(30)]
        public string PurchaseStatus { get { return "B"; } }

        [Name("Currency Code")]
        [Index(31)]
        public string CurrencyCode { get; set; }

        [Name("Exchange Rate")]
        [Index(32)]
        public double? ExchangeRate { get; set; }

        [Name("Terms - Payment is Due")]
        [Index(33)]
        public int PaymentIsDue => 0;

        [Name(" - Discount Days")]
        [Index(34)]
        public int DiscountDays => 0;

        [Name(" - Balance Due Days")]
        [Index(35)]
        public int BalanceDueDays => 0;

        [Name(" - % Discount")]
        [Index(36)]
        public double? DiscPc => 0;

        [Name("Amount Paid")]
        [Index(37)]
        public double? AmtPaid => 0;

        [Name("Category")]
        [Index(38)]
        public string Category { get { return null; } }

        [Name("Card ID")]
        [Index(39)]
        public string CardID { get; set; }

        [Name("Record ID")]
        [Index(40)]
        public int RecordID { get; set; }

        [Ignore]
        public long ilId { get; set; }
        

        [Ignore]
        public DateTime? pstPurchaseDate { get; set; }
        [Ignore]
        public string dateformat { get; set; }
        [Ignore]
        public decimal? ilAmt { get; set; }

      

       
    }


    public enum PSType
    {
        forLine = 1,
        forPay = 2
    }
}
