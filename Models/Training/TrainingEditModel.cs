using CommonLib.Helpers;
using CommonLib.Models;
using Dapper;
using MMCommonLib.BaseModels;
using MMDAL;
using MMLib.Models.Training;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MMLib.Models.Training
{
	public class TrainingEditModel : PagingBaseModel
	{
		public string CurrentOldestDate { get; set; }
		public List<string> TrainingIdList { get; set; }
		public TrainingEditModel(string strfrmdate, string strtodate, string keyword = "")
		{
			SearchDates = new SearchDates();
			CommonHelper.GetSearchDates(strfrmdate, strtodate, ref SearchDates);
			DateFromTxt = SearchDates.DateFromTxt;
			DateToTxt = SearchDates.DateToTxt;
			using var connection = new Microsoft.Data.SqlClient.SqlConnection(DefaultConnection);
			connection.Open();
			CurrentOldestDate = connection.QueryFirstOrDefault<string>(@"EXEC dbo.GetTrainingOldestDate @apId=@apId", new { apId });
			TrainingIdList = connection.Query<string>(@"EXEC dbo.GetTrainingIdList @apId=@apId", new { apId }).ToList();
		}

		public static void Save(List<TrainingModel> model, string frmdate, string todate, int apId)
		{
			using var connection = new Microsoft.Data.SqlClient.SqlConnection(defaultConnection);
			connection.Open();

			if (model.Count > 0)
			{
				SessUser user = HttpContext.Current.Session["User"] as SessUser;
				var trainings = new List<MMDAL.Training>();

				using var context = new MMDbContext();

				var trainingIdList = context.Trainings.Select(x => x.trId).ToList();

				var dateTime = DateTime.Now;
				foreach (var tr in model)
				{
					if (!string.IsNullOrEmpty(tr.trId) && trainingIdList.Contains(tr.trId)) continue;

					if (!string.IsNullOrEmpty(tr.trId) && !string.IsNullOrEmpty(tr.trApplicant))
					{
						DateTime trdate = CommonHelper.GetDateFrmString(tr.strDate);
						var redatetime = CommonHelper.GetDateTimeString4Enquiry(tr.receivedDateTime);

						var Id = CommonHelper.GenerateNonce(152, false);

						MMDAL.Training training = new MMDAL.Training
						{
							Id = Id,
							trId = tr.trId,
							trCompany = tr.trCompany,
							trApplicant = tr.trApplicant,
							trIndustry = tr.trIndustry,
							//trEmail = tr.trEmail??null,	
							//trPhone = tr.trPhone??null,
							trAttendance = tr.trAttendance,
							trIsApproved = tr.trIsApproved,
							trDate = trdate,
							trReceivedDateTime = redatetime,
							trDateFrm = frmdate ?? "",
							trDateTo = todate ?? "",
							CreateTime = dateTime,
							AccountProfileId = apId
						};
						trainings.Add(training);
					}
				}
				context.Trainings.AddRange(trainings);
				context.SaveChanges();
			}
		}
	}
}
