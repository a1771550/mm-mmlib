using MMCommonLib.BaseModels;
using MMDAL;
using MMLib.Models.User;
using PagedList;
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
        public IPagedList<UserModel> PagingUserList { get; set; }
        public int? LicensedUserCount { get; set; }
		public string BtnClass { get; set; }

        
    }
}