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
        public PoSettings PoSettings { get; set; }
        public void GetList()
        {
            using var connection = new SqlConnection(defaultConnection);
            if (connection.State == System.Data.ConnectionState.Closed) connection.Open();
            PoSettings = GetPoSettings(connection);
        }

        public static PoSettings GetPoSettings(SqlConnection connection)
        {
            return new PoSettings
            {
                //MUST NOT use Session here!!!
                poThreshold = connection.QueryFirstOrDefault<decimal>(@"EXEC dbo.GetPoThreshold @apId=@apId", new { apId }),
                poSettingItems = GetPoQtyAmtList(connection)
            };
        }

        public static List<PoQtyAmtModel> GetPoQtyAmtList(SqlConnection connection)
        {            
            return connection.Query<PoQtyAmtModel>(@"EXEC dbo.GetPoQtyAmtList @apId=@apId", new { apId }).ToList();
        }


        public static void Edit(PoSettings poSettings)
        {
            using var context = new MMDbContext();
            ComInfo ComInfo = context.ComInfoes.Find(comInfo.comCode);
            ComInfo.PoThreshold = poSettings.poThreshold;         

            List<PoQtyAmt> Items = [];

            //remove current records first
            context.Database.ExecuteSqlCommand("TRUNCATE TABLE PoQtyAmt");            
            context.SaveChanges();

            Items = [];
            //add records:
            foreach (var item in poSettings.poSettingItems)
            {
                decimal? qtymax = item.Qty == -1 ? null : item.AmtMax;
                Items.Add(new PoQtyAmt
                {
                    Qty = item.Qty,
                    AmtMin = item.AmtMin,
                    AmtMax = qtymax,
                    AccountProfileId = apId,
                    CreateTime = DateTime.Now
                });
            }
            context.PoQtyAmts.AddRange(Items);
            context.SaveChanges();
        }

        public static void Remove(int Id)
        {
            using var context = new MMDbContext();
            var item = context.PoQtyAmts.Find(Id);
            if(item != null) {
                context.PoQtyAmts.Remove(item);
                context.SaveChanges();
            }
        }
    }
}
