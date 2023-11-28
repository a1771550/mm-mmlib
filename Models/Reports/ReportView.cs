using System;

namespace MMLib.Models
{
	[Serializable]
	public class ReportView
	{
		public int Id { get; set; }
		public string Code { get; set; }
		public string Name { get; set; }
	}
}