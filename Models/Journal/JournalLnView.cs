using CommonLib.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLib.Models.Journal
{
	public class JournalLnView
	{
		public string mainId { get; set; }		
		public DateTime JournalDate { get; set; }
		public string Memo { get; set; }
		public long Id { get; set; }
		public string JournalNumber { get; set; }
		public int Seq { get; set; }
		public string AccountName { get; set; }
		public string AccountNumber { get; set; }
		public decimal? DebitExTaxAmount { get; set; }
		public decimal? DebitIncTaxAmount { get; set; }
		public decimal? CreditExTaxAmount { get; set; }
		public decimal? CreditIncTaxAmount { get; set; }
		public string JobName { get; set; }
		public int JobID { get; set; }
		public string AllocationMemo { get; set; }
		public string DateDisplay { get { return CommonHelper.FormatDate(JournalDate); } }

		public bool Inclusive { get; set; }
	}
}
