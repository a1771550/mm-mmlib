using CommonLib.Helpers;
using CommonLib.Models;
using Dapper;
using MMCommonLib.BaseModels;
using MMDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MMLib.Models.Job
{
	public class JobEditModel:PagingBaseModel
	{
		public string CurrentOldestDate { get; set; }
		public List<string> JobIdList { get; set; }
		public JobEditModel(string strfrmdate, string strtodate, string keyword = "")
		{
			SearchDates = new SearchDates();
			CommonHelper.GetSearchDates(strfrmdate, strtodate, ref SearchDates);
			DateFromTxt = SearchDates.DateFromTxt;
			DateToTxt = SearchDates.DateToTxt;
			using var connection = new Microsoft.Data.SqlClient.SqlConnection(DefaultConnection);
			connection.Open();
			CurrentOldestDate = connection.QueryFirstOrDefault<string>(@"EXEC dbo.GetJobOldestDate @apId=@apId", new { apId });
			JobIdList = connection.Query<string>(@"EXEC dbo.GetJobIdList @apId=@apId", new { apId }).ToList();
		}

		public static void Save(List<JobModel> model, string frmdate, string todate, int apId)
		{
			using var connection = new Microsoft.Data.SqlClient.SqlConnection(defaultConnection);
			connection.Open();

			if (model.Count > 0)
			{
				SessUser user = HttpContext.Current.Session["User"] as SessUser;
				var jobs = new List<MMDAL.Job>();

				using var context = new MMDbContext();

				var jobIdList = context.Jobs.Select(x => x.joId).ToList();

				var dateTime = DateTime.Now;
				foreach (var jo in model)
				{
					if (!string.IsNullOrEmpty(jo.joId) && jobIdList.Contains(jo.joId)) continue;

					if (!string.IsNullOrEmpty(jo.joId) && !string.IsNullOrEmpty(jo.joClient))
					{
						DateTime jodate = CommonHelper.GetDateFrmString(jo.receiveddate);
						var redatetime = CommonHelper.GetDateTimeString4Enquiry(jo.receivedDateTime);

						var Id = CommonHelper.GenerateNonce(152, false);

						double? workinghrs = 0;
						if (!string.IsNullOrEmpty(jo.joTime))
						{
							var arr = jo.joTime.Split('-');
							var startTime = arr[0].Trim();
							var endTime = arr[1].Trim();	
							workinghrs = DateTime.Parse(endTime).Subtract(DateTime.Parse(startTime)).TotalHours;
						}
						 
						MMDAL.Job job = new MMDAL.Job
						{
							Id = Id,
							joId = jo.joId,
							joStaffName = jo.joStaffName,
							joStaffEmail = jo.joStaffEmail,
							joClient = jo.joClient,
							joTime = jo.joTime,
							joWorkingHrs = workinghrs,
							joDate = jodate,
							joReceivedDateTime = redatetime,
							joDateFrm = frmdate ?? "",
							joDateTo = todate ?? "",
							CreateTime = dateTime,
							AccountProfileId = apId
						};
						jobs.Add(job);
					}
				}
				context.Jobs.AddRange(jobs);
				context.SaveChanges();
			}
		}
	}
}
