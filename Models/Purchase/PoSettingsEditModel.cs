using Dapper;
using Microsoft.Data.SqlClient;
using MMDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLib.Models.Purchase
{
	public class PoSettingsEditModel : PagingBaseModel
	{
		public PoSettingsEditModel() { }
		public List<PoQtyAmtModel> PoQtyAmtList { get; set; }

		public static void Edit(List<PoQtyAmtModel> PoSettings, int poThreshold)
		{
			using var context = new MMDbContext();
			ComInfo ComInfo = context.ComInfoes.Find(comInfo.comCode);
			ComInfo.PoThreshold = poThreshold;
			foreach(var item in PoSettings)
			{
				PoQtyAmt poQtyAmt = context.PoQtyAmts.Find(item.Id);
				decimal? qtymax = item.Qty == -1 ? null : item.AmtMax;
				poQtyAmt.Qty = item.Qty;
				poQtyAmt.AmtMin = item.AmtMin;
				poQtyAmt.AmtMax = qtymax;
				poQtyAmt.ModifyTime = DateTime.Now;
			}
			context.SaveChanges();
		}

		public void GetList()
		{           //GetPoQtyAmtList
			PoQtyAmtList = GetPoQtyAmtList();
		}

		public static List<PoQtyAmtModel> GetPoQtyAmtList()
		{
			using var connection = new SqlConnection(defaultConnection);
			if(connection.State == System.Data.ConnectionState.Closed)connection.Open();			
			return connection.Query<PoQtyAmtModel>(@"EXEC dbo.GetPoQtyAmtList @apId=@apId", new { apId }).ToList();
		}
	}
}
