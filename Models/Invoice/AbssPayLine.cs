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
        [Name("Cheque Account")]
        [Index(0)]
        public string ChequeAccountNo { get; set; }

        [Name("Cheque #")]
        [Index(1)]
        public string sipChequeNo { get; set; }

        [Name("Date")]
        [Index(2)]
        public string Date4ABSS
        {
            get
            {
                return payTime != null ? CommonHelper.FormatDate4ABSS(payTime, dateformat) : string.Empty;
            }
        }

        [Name("Co./Last Name")]
        [Index(3)]
        public string SupplierName { get; set; }

        [Name("First Name")]
        [Index(4)]
        public string FirstName { get; set; }


        [Name("Addr 1 - Line 1")]
        [Index(5)]
        public string Addr1Line1 { get; set; }

        [Name("       - Line 2")]
        [Index(6)]
        public string Addr1Line2 { get; set; }

        [Name("       - Line 3")]
        [Index(7)]
        public string Addr1Line3 { get; set; }

        [Name("       - Line 4")]
        [Index(8)]
        public string Addr1Line4 { get; set; }

        [Name("Memo")]
        [Index(9)]
        public string Remark { get; set; }

        [Name("Allocation Account #")]
        [Index(10)]
        public string AllocationAccountNo => null;

        [Name("Amount")]
        [Index(11)]
        public double? AmtPaid { get { return sipAmt == null ? 0 : Convert.ToDouble((decimal)sipAmt); } }

        [Name("Job #")]
        [Index(12)]
        public string Job => string.Empty;

        [Name("Printed")]
        [Index(13)]
        public string Printed => string.Empty;

        [Name("Currency Code")]
        [Index(14)]
        public string CurrencyCode { get; set; }

        [Name("Exchange Rate")]
        [Index(15)]
        public double? ExchangeRate { get; set; }

        [Name("Allocation Memo")]
        [Index(16)]
        public string AllocationMemo => string.Empty;

        [Name("Category")]
        [Index(17)]
        public string Category => "HKMM";

        [Name("Card ID")]
        [Index(18)]
        public string CardID { get; set; }

        [Name("Record ID")]
        [Index(19)]
        public int RecordID { get; set; }

        [Name("Delivery Status")]
        [Index(20)]
        public string DeliveryStatus { get { return "P"; } }

        [Ignore]
        public string pstCode { get; set; }

        [Ignore]
        public long payId { get; set; }
        [Ignore]
        public Nullable<int> seq { get; set; } = null;
        [Ignore]
        public DateTime payTime { get; set; }
        [Ignore]
        public string dateformat { get; set; }

        [Ignore]
        public decimal? sipAmt { get; set; }

    }
}
