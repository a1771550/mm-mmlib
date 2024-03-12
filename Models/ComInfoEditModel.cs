using CommonLib.Helpers;
using Dapper;
using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using MMDAL;
using MMCommonLib.BaseModels;

namespace MMLib.Models
{
    public class ComInfoEditModel:PagingBaseModel
    {
        public static async Task<bool> Save(ComInfoModel model)
        {
            DateTime dtnow = await CommonHelper.GetCachedDateTimeAsync();           
            int apId = int.Parse(ConfigurationManager.AppSettings["apId"]);
            using (var context = new ProxyDbContext())
            {
                ComInfo comInfo = context.ComInfoes.FirstOrDefault(x => x.AccountProfileId == apId);
                if (comInfo == null) return false;               
                else
                {
                    comInfo.comName = model.comName;
                    comInfo.comEmail = model.comEmail;
                    comInfo.comPhone = model.comPhone;
                    comInfo.MYOBDb = model.MYOBDb;                   
                    comInfo.MYOBExe = model.MYOBExe;
                    comInfo.MYOBKey = model.MYOBKey;
                    comInfo.MYOBUID = model.MYOBUID;
                    comInfo.MYOBPASS = model.MYOBPASS;
                    comInfo.MYOBDriver = model.MYOBDriver;
                    comInfo.PrimaryLocation = model.PrimaryLocation;
                    comInfo.ModifyTime = dtnow;
                    await context.SaveChangesAsync();
                    return true;
                }
            }
        }
        static string _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
     
        public static ComInfoModel GetByApId(int apId)
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
            connection.Open();
            return connection.QueryFirstOrDefault<ComInfoModel>(@"EXEC dbo.GetComInfoByApId @apId=@apId", new { apId });
        }
    }
}
