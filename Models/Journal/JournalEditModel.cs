using CommonLib.Helpers;
using Dapper;
using MMCommonLib.BaseModels;
using MMDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Data.SqlClient;
using System.Configuration;
using PagedList;
using System.Runtime.Remoting.Contexts;
using MMLib.Models.MYOB;
using MMLib.Models.Job;
using System.Data.Entity.Validation;
using System.Text;
using ModelHelper = MMLib.Helpers.ModelHelper;

namespace MMLib.Models.Journal
{
	public class JournalEditModel : PagingBaseModel
	{
		public JournalModel Journal { get; set; }
		public List<JournalModel> JournalList { get; set; }
		public List<JournalLnView> JournalLnList { get; set; }
		public IPagedList<JournalLnView> PagingJournalList { get { return JournalLnList != null && JournalLnList.Count > 0 ? JournalLnList.ToPagedList((int)PageNo, PageSize) : null; } }

		public List<MyobJobModel> JobList { get; set; }
		public Dictionary<string, List<AccountModel>> DicAcAccounts;
		public JournalEditModel() { }
		public JournalEditModel(string Id = "") : this()
		{
			JournalLnList = new List<JournalLnView>();
			Get(Id);
		}
		public void Get(string Id = "")
		{
			using var connection = new SqlConnection(ConnectionString);
			connection.Open();

			Journal = new JournalModel();
			if (string.IsNullOrEmpty(Id))
			{
				var lastordercode = connection.QueryFirstOrDefault<string>(@"EXEC dbo.GetLatestJournalNumber @apId=@apId", new { apId });
				var newcode = CommonHelper.GenOrderNumber(ConfigurationManager.AppSettings["JournalPrefix"], int.Parse(ConfigurationManager.AppSettings["MaxJournalNumberLength"]), lastordercode);
				Journal.JournalNumber = newcode;

			}
			else
			{
				JournalLnList = connection.Query<JournalLnView>(@"EXEC dbo.GetJournalById @InvoiceId=@InvoiceId,@apId=@apId", new { Id, apId }).ToList();
				var journalLn = JournalLnList.FirstOrDefault();
				Journal.Id = journalLn.mainId;
				Journal.JournalNumber = journalLn.JournalNumber;
				Journal.Memo = journalLn.Memo;
				Journal.JournalDate = journalLn.JournalDate;
			}

			JobList = connection.Query<MyobJobModel>(@"EXEC dbo.GetMyobJobList @apId=@apId", new { apId }).ToList();

			DicAcAccounts = new Dictionary<string, List<AccountModel>>();
			//GetAccountClassificationIDList
			var acIdList = connection.Query<string>(@"EXEC dbo.GetAccountClassificationIDList @apId=@apId", new { apId }).ToList();
			foreach (var ac in acIdList)
			{
				DicAcAccounts[ac] = new List<AccountModel>();
			}
			//GetAccountList	
			List<AccountModel> AccountList = connection.Query<AccountModel>(@"EXEC dbo.GetAccountList @apId=@apId", new { apId }).ToList();
			foreach (var key in DicAcAccounts.Keys)
			{
				foreach (var account in AccountList)
				{
					if (account.AccountClassificationID == key)
					{
						DicAcAccounts[key].Add(account);
					}
				}
			}
		}
		public void GetList(int sortCol = 5, string sortOrder = "desc", string keyword = null)
		{
			using var connection = new SqlConnection(ConnectionString);
			connection.Open();
			JournalLnList = connection.Query<JournalLnView>(@"EXEC dbo.GetJournalList @apId=@apId,@sortCol=@sortCol,@sortOrder=@sortOrder,@keyword=@keyword", new { apId, sortCol, sortOrder, keyword }).ToList();
		}

		public void Edit(JournalModel model, List<JournalLnView> JournalLns)
		{
			using var context = new MMDbContext();
			MMDAL.Journal journal;

			using (var transaction = context.Database.BeginTransaction())
			{
				try
				{
					if (string.IsNullOrEmpty(model.Id))
					{
						journal = new MMDAL.Journal
						{
							Id = CommonHelper.GenerateNonce(152, false),
							JournalNumber = model.JournalNumber,
							JournalDate = CommonHelper.GetDateFrmString4SQL(model.strDate),
							Memo = model.Memo,
							IsCheckOut = false,
							AccountProfileId = AccountProfileId,
							CreateTime = DateTime.Now
						};

						context.Journals.Add(journal);

						List<JournalLn> journalLns = new List<JournalLn>();

						foreach (var item in JournalLns)
						{
							journalLns.Add(new JournalLn
							{
								JournalNumber = model.JournalNumber,
								Seq = item.Seq,
								AccountNumber = item.AccountNumber,
								DebitExTaxAmount = item.DebitExTaxAmount,
								CreditExTaxAmount = item.CreditExTaxAmount,
								JobID = item.JobID,
								AllocationMemo = item.AllocationMemo,
								AccountProfileId = apId,
								CreateTime = DateTime.Now,
							});
						}

						context.JournalLns.AddRange(journalLns);
					}
					else
					{
						journal = context.Journals.Find(model.Id);
						journal.Memo = model.Memo;
						journal.ModifyTime = DateTime.Now;

						foreach (var item in JournalLns)
						{
							var currentLn = context.JournalLns.Find(item.Id);
							if (currentLn != null)
							{
								currentLn.AccountNumber = item.AccountNumber;
								currentLn.DebitExTaxAmount = item.DebitExTaxAmount;
								currentLn.CreditExTaxAmount = item.CreditExTaxAmount; currentLn.JobID = item.JobID; currentLn.AllocationMemo = item.AllocationMemo;
								currentLn.ModifyTime = DateTime.Now;
							}
						}
					}

					context.SaveChanges();

					ModelHelper.WriteLog(context, "Import Item data from Central done", "ImportFrmCentral");
					transaction.Commit();
				}
				catch (DbEntityValidationException e)
				{
					transaction.Rollback();
					StringBuilder sb = new StringBuilder();
					foreach (var eve in e.EntityValidationErrors)
					{
						sb.AppendFormat("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
							eve.Entry.Entity.GetType().Name, eve.Entry.State);
						foreach (var ve in eve.ValidationErrors)
						{
							sb.AppendFormat("- Property: \"{0}\", Value: \"{1}\", Error: \"{2}\"",
				ve.PropertyName,
				eve.Entry.CurrentValues.GetValue<object>(ve.PropertyName),
				ve.ErrorMessage);
						}
					}
					//throw new Exception(sb.ToString());
					ModelHelper.WriteLog(context, string.Format("Import Item data from Central failed:{0}", sb.ToString()), "ImportFrmCentral");
					context.SaveChanges();
				}
			}
		}

		public static void Delete(string Id)
		{
			using var context = new MMDbContext();
			var journal = context.Journals.Find(Id);
			context.Journals.Remove(journal);
			var journalLns = context.JournalLns.Where(x => x.JournalNumber == journal.JournalNumber);
			context.JournalLns.RemoveRange(journalLns);
			context.SaveChanges();
		}

		public static List<string> GetUploadSqlList(bool includeUploaded, int lang, ComInfo comInfo, int apId, MMDbContext context, SqlConnection connection, DateTime frmdate, DateTime todate, ref DataTransferModel dmodel)
		{
			List<string> sqllist = new List<string>();

			var location = dmodel.SelectedLocation;
			string device = dmodel.Device;
			bool approvalmode = (bool)comInfo.ApprovalMode;

			bool ismultiuser = (bool)comInfo.MyobMultiUser;
			string currencycode = comInfo.DefaultCurrencyCode;
			double exchangerate = (double)comInfo.DefaultExchangeRate;
			//var JobList = connection.Query<MyobJobModel>(@"EXEC dbo.GetJobList @apId=@apId", new { apId }).ToList();

			#region Local Variables   
			string sql = "";

			List<JournalModel> JournalModels = new List<JournalModel>();
			List<JournalLnView> JournalLnViews = new List<JournalLnView>();

			string accountno = "";
			var dateformatcode = context.AppParams.FirstOrDefault(x => x.AccountProfileId == apId && x.appParam == "DateFormat").appVal;
			accountno = context.ComInfoes.FirstOrDefault().comAccountNo;
			HashSet<string> journalcodes = new HashSet<string>();
			#endregion

			#region Journal
			//JournalNumber,JournalDate,Memo,Inclusive,AccountNumber,DebitExTaxAmount,DebitIncTaxAmount,CreditExTaxAmount,CreditIncTaxAmount,Job,CurrencyCode,ExchangeRate,AllocationMemo
			JournalModels = (from j in context.Journals
							 where j.JournalDate >= frmdate && j.JournalDate <= todate
						   && j.AccountProfileId == apId
							 select j
					   ).ToList().
					   Select(j => new JournalModel()
					   {
						   Id = j.Id,	
						   IsCheckOut = j.IsCheckOut,
						   JournalNumber = j.JournalNumber,
					   }
					).ToList();

			if (JournalModels.Count > 0)
			{
				if (!includeUploaded)
				{
					JournalModels = JournalModels.Where(x => !x.IsCheckOut).ToList();
				}			

				JournalLnViews = (from sl in context.JournalLns
								  join s in context.Journals
								  on sl.JournalNumber equals s.JournalNumber
								  join job in context.MyobJobs
								  on sl.JobID equals job.JobID
								  where s.JournalDate >= frmdate && s.JournalDate <= todate && s.AccountProfileId == apId
								  && sl.AccountProfileId == apId
								 
								  select new JournalLnView
								  {
									  JournalDate = s.JournalDate,
									  Memo = s.Memo,
									  Inclusive = s.Inclusive,
									  Id = sl.Id,
									  JournalNumber = sl.JournalNumber,
									  Seq = sl.Seq,
									  DebitExTaxAmount = sl.DebitExTaxAmount,
									  DebitIncTaxAmount = sl.DebitIncTaxAmount,
									  CreditExTaxAmount = sl.CreditExTaxAmount,
									  CreditIncTaxAmount = sl.CreditIncTaxAmount,
									  AllocationMemo = sl.AllocationMemo,
									  AccountNumber = sl.AccountNumber,
									  JobName = job.JobName,
								  }
									).ToList();
			}
			#endregion

			if (JournalLnViews.Count > 0)
			{
				foreach (var journal in JournalModels)
				{
					dmodel.CheckOutIds_Journal.Add(journal.Id);
				}

				var GroupedJournalLnList = JournalLnViews.GroupBy(x => x.JournalNumber);

				List<string> columns = null;
				string strcolumn = "";
				string value = "";

				foreach (var group in GroupedJournalLnList)
				{
					sql = $"INSERT INTO Import_General_Journals({ModelHelper.sqlfields4Journal})  VALUES(";
					List<string> values = new List<string>();
					bool isdeposit = group.FirstOrDefault().JournalNumber.ToLower().IndexOf('d') >= 0;

					foreach (var item in group)
					{
						ModelHelper.GenSql4JournalItem(dateformatcode, out columns, out strcolumn, out value, ref values, item, ModelHelper.sqlfields4Journal);
					}

					sql += string.Join(",", values) + ")";
					sqllist.Add(sql);
				}
			}

			return sqllist;
		}

	}
}
