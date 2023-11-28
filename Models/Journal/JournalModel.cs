using CommonLib.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLib.Models.Journal
{
	public class JournalModel
	{
		public string Id { get; set; }
		public string JournalNumber { get; set; }

		
		public string strDate { get; set; }
		public DateTime JournalDate { get; set; }
		public string Memo { get; set; }
		public bool Inclusive { get; set; }
		
		public string TaxCode { get; set; }
		public decimal? ImportDutyAmount { get; set; }
		public string CurrencyCode { get; set; }
		public decimal? ExchangeRate { get; set; }
		public string Category { get; set; }

		public string DateDisplay { get { return CommonHelper.FormatDate(JournalDate); } }

		public bool IsCheckOut { get; set; }
	}
}
