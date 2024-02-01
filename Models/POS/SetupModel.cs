using MMCommonLib.BaseModels;
using MMDAL;
using MMLib.Models.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MMLib.Models
{
	public class SetupModel:PagingBaseModel
	{
		public List<FuncView> MainFuncs { get; set; }
		public List<UserModel> Users { get; set; }
        public int? LicensedUserCount { get; set; }
		public string BtnClass { get; set; }

        public void GetUserList(int? isactive=1)
        {
            using (var context = new MMDbContext())
            {
                Users = UserEditModel.GetUserList(isactive);
                LicensedUserCount = context.GetLicensedUserCount().FirstOrDefault();
                BtnClass = LicensedUserCount >= (int)ComInfo.POSLic ? "linkdisabled" : "";
            }
        }
    }
}