using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonLib.Helpers;
using CsvHelper;
using CsvHelper.Configuration.Attributes;

namespace MMLib.Models.Invoice
{
    public class AbssPayLine
    {
        [Name("Co./Last Name")]
        [Index(0)]
        public string SupplierName { get; set; }

        [Name("First Name")]
        [Index(1)]
        public string FirstName { get; set; }


        [Name("Payee - Line 1")]
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

        [Name("Payment Account #")]
        [Index(6)]
        public string ChequeAccountNo { get; set; }

        [Name("Cheque Number")]
        [Index(7)]
        public string sipChequeNo { get; set; }

        [Name("Payment Date")]
        [Index(8)]
        public string Date4ABSS
        {
            get
            {
                return payTime != null ? CommonHelper.FormatDate4ABSS(payTime, dateformat) : string.Empty;
            }
        }

        [Name("Statement Text")]
        [Index(9)]
        public string AllocationMemo => string.Empty;

        [Name("Purchase #")]
        [Index(10)]
        public string pstCode => string.Empty;

        [Name("Supplier's #")]
        [Index(11)]
        public string InvoiceId { get; set; }

        [Name("Bill Date")]
        [Index(12)]
        public string PurchaseDate4ABSS
        {
            get
            {
                return pstPurchaseDate != null ? CommonHelper.FormatDate4ABSS(pstPurchaseDate, dateformat) : string.Empty;
            }
        }

        [Name("Amount Applied")]
        [Index(13)]
        public double? AmtPaid { get { return sipAmt == null ? 0 : Convert.ToDouble((decimal)sipAmt); } }

        [Name("Memo")]
        [Index(14)]
        public string Remark { get; set; }

        [Name("Already Printed")]
        [Index(15)]
        public string Printed => string.Empty;

        [Name("Currency Code")]
        [Index(16)]
        public string CurrencyCode { get; set; }

        [Name("Exchange Rate")]
        [Index(17)]
        public double? ExchangeRate { get; set; }

        [Name("Card ID")]
        [Index(18)]
        public string CardID => string.Empty;

        [Name("Record ID")]
        [Index(19)]
        public int RecordID { get; set; }

        [Name("Delivery Status")]
        [Index(20)]
        public string DeliveryStatus { get { return "P"; } }


        [Ignore]
        public string AllocationAccountNo => null;

        [Ignore]
        public string Job => string.Empty;

        [Ignore]
        public string Category => "HKMM";

        [Ignore]
        public long payId { get; set; }
        [Ignore]
        public Nullable<int> seq { get; set; } = null;
        [Ignore]
        public DateTime payTime { get; set; }
        [Ignore]
        public char dateformat { get; set; }

        [Ignore]
        public decimal? sipAmt { get; set; }

        [Ignore]
        public DateTime pstPurchaseDate { get; set; }
    }
}
