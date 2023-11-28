using CommonLib.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLib.Models.Job
{
	public class JobModel
	{
		public string joClient { get; set; }
		public string joTime { get; set; }
		public double? joWorkingHrs { get; set; }
		public string joAttachements { get; set; }
		public string receivedDateTime { get; set; }
		public string receiveddate { get; set; }
		public string id { get; set; }
		public string joId { get; set; }
		public string joReceivedDateTime { get; set; }
		public string joStaffName { get; set; }
		public string joStaffEmail { get; set; }
		
		public DateTime? joDate { get; set; }
		public string DateDisplay { get { return joDate == null ? "N/A" : CommonHelper.FormatDate((DateTime)joDate); } }
	}
}
