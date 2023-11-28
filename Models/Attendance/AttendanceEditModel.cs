using CommonLib.Helpers;
using CommonLib.Models;
using Dapper;
using MMCommonLib.BaseModels;
using MMDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MMLib.Models.Attendance
{
	public class AttendanceEditModel : PagingBaseModel
	{
		public string CurrentOldestDate { get; set; }
		public List<string> AttdIdList { get; set; }
		public AttendanceEditModel(string strfrmdate, string strtodate, string keyword = "")
		{
			SearchDates = new SearchDates();
			CommonHelper.GetSearchDates(strfrmdate, strtodate, ref SearchDates);
			DateFromTxt = SearchDates.DateFromTxt;
			DateToTxt = SearchDates.DateToTxt;
			using var connection = new Microsoft.Data.SqlClient.SqlConnection(DefaultConnection);
			connection.Open();			
			CurrentOldestDate = connection.QueryFirstOrDefault<string>(@"EXEC dbo.GetAttdOldestDate @apId=@apId", new { apId });
			AttdIdList = connection.Query<string>(@"EXEC dbo.GetAttdIdList @apId=@apId", new { apId }).ToList();			
		}

		public static void Save(List<AttendanceModel> model, string frmdate, string todate, int apId)
		{ 
			using var connection = new Microsoft.Data.SqlClient.SqlConnection(defaultConnection);
			connection.Open();

			if (model.Count > 0)
			{
				SessUser user = HttpContext.Current.Session["User"] as SessUser;
				var attendances = new List<MMDAL.Attendance>();
				
				using var context = new MMDbContext();
				
				var currentAttendanceIdList = context.Attendances.Select(x=>x.saId).ToList();
				
				var dateTime = DateTime.Now;
				foreach (var attd in model)
				{
					if(!string.IsNullOrEmpty(attd.saId) && currentAttendanceIdList.Contains(attd.saId)) continue;

					DateTime sadate = CommonHelper.GetDateFrmString(attd.receiveddate);
					var redatetime = CommonHelper.GetDateTimeString4Enquiry(attd.receivedDateTime);

					var Id = CommonHelper.GenerateNonce(152, false);
					MMDAL.Attendance attendance = new MMDAL.Attendance
					{
						Id = Id,
						saId = attd.saId,
						saName = attd.saName,	
						saCheckInTime = attd.saCheckInTime,
						saCheckOutTime = attd.saCheckOutTime,
						saDate = sadate,
						saReceivedDateTime = redatetime,
						saDateFrm = frmdate ?? "",
						saDateTo = todate ?? "",
						CreateTime = dateTime,
						AccountProfileId = apId
					};
					attendances.Add(attendance);
				}
				context.Attendances.AddRange(attendances);
				context.SaveChanges();
			}
		}
	}
}
