using MMDAL;
using MMLib.Models;
using MMLib.Models.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLib.Helpers
{
	public class UserHelper
	{
		public static List<RoleType> GetUserRoles(SessUser user)
		{			
			return user.Roles;
		}
		public static List<RoleType> GetUserRoles(SysUser user)
		{
			var userrolecodes = user.UserRole.Split(',');
			List<RoleType> roletypes = [];
			foreach (var rolecode in userrolecodes)
			{
				Enum.TryParse(rolecode, out RoleType roletype);
				roletypes.Add(roletype);
			}
			return roletypes;
		}
		public static bool CheckIfAdmin(SysUser user)
		{
			var Roles = GetUserRoles(user);
			return Roles.Any(x => x == RoleType.Admin && x != RoleType.SalesManager);
		}
		public static bool CheckIfAdmin(List<RoleType> Roles)
		{
			return Roles.Any(x => x == RoleType.Admin && x != RoleType.SalesManager);
		}
		public static bool CheckIfARAdmin(SessUser user)
		{
			var Roles = GetUserRoles(user);
			return Roles.Any(x => x == RoleType.ARAdmin);
		}
		
	}
}
