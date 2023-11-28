using CommonLib.Helpers;
using System;

namespace MMLib.Models.Attendance
{
	public class AttendanceModel
	{
		public string saName { get; set; }
		public string saCheckInTime { get; set; }
		public string saCheckOutTime { get; set; }
		public string receivedDateTime { get; set; }
		public string receiveddate { get; set; }
		public string id { get; set; }
		//public string Id {  get; set; }
		public string saId { get; set; }	
		public string saReceivedDateTime { get; set; }		
		public DateTime? saDate { get; set; }
		public string DateDisplay { get { return saDate == null ? "N/A" : CommonHelper.FormatDate((DateTime)saDate); } }
		
	}
}
