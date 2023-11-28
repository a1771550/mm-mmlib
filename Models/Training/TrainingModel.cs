using CommonLib.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLib.Models.Training
{
	public class TrainingModel
	{		
		public string trId { get; set; }
		public string trApplicant { get; set; }
		public string trCompany { get; set; }
		public string trEmail { get; set; }
		public string trIndustry { get; set; }
		public string trPhone { get; set; }
		public int trAttendance { get; set; }
		public bool trIsApproved { get; set; }
		public string trReceivedDateTime { get; set; }
		public string receivedDateTime { get; set; }
		public string receiveddate { get; set; }
		public string id { get; set; }
		public string strDate { get; set; }
		public DateTime? trDate { get; set; }
		public string DateDisplay { get { return trDate == null ? "N/A" : CommonHelper.FormatDate((DateTime)trDate); } }
	}
}
