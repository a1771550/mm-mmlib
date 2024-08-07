﻿using MMDAL;
using MMLib.Models;
using MMLib.Models.User;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MMLib.Helpers
{
    public class UserHelper
    {
        public static List<RoleType> GetUserRoles(UserModel user)
        {
            var userrolecodes = user.UserRole.Split(',').ToList();
            List<RoleType> roletypes = [];
            foreach (var rolecode in userrolecodes)
            {
                Enum.TryParse(rolecode, out RoleType roletype);
                roletypes.Add(roletype);
            }
            return roletypes;
        }
        public static List<RoleType> GetUserRoles(SessUser user)
        {
            return user.Roles;
        }
        public static List<RoleType> GetUserRoles(SysUser user)
        {
            var userrolecodes = user.UserRole.Split(',').ToList();
            List<RoleType> roletypes = [];
            foreach (var rolecode in userrolecodes)
            {
                Enum.TryParse(rolecode, out RoleType roletype);
                roletypes.Add(roletype);
            }
            return roletypes;
        }
        public static bool CheckIfSystemAdmin(SysUser user)
        {
            var Roles = GetUserRoles(user);
            return Roles.Any(x => x == RoleType.SystemAdmin);
        }

        public static bool CheckIfFinDept(SessUser user)
        {
            var Roles = GetUserRoles(user);
            return Roles.Any(x => x == RoleType.FinanceDept);
        }
        public static bool CheckIfDeptHead(SessUser user)
        {
            var Roles = GetUserRoles(user);
            return Roles.Any(x => x == RoleType.DeptHead);
        }
        public static bool CheckIfARAdmin(SessUser user)
        {
            var Roles = GetUserRoles(user);
            return Roles.Any(x => x == RoleType.ARAdmin);
        }

        public static bool CheckIfStaff(SessUser user)
        {
            var Roles = GetUserRoles(user);
            return Roles.Any(x => x == RoleType.Staff);
        }
        public static bool CheckIfDB(SessUser user)
        {
            var Roles = GetUserRoles(user);
            return Roles.Any(x => x == RoleType.DirectorBoard);
        }
        public static bool CheckIfMD(SessUser user)
        {
            var Roles = GetUserRoles(user);
            return Roles.Any(x => x == RoleType.MuseumDirector);
        }
        public static bool CheckIfMD(UserModel user)
        {
            var Roles = GetUserRoles(user);
            return Roles.Any(x => x == RoleType.MuseumDirector);
        }
        public static bool CheckIfDirector(SysUser user)
        {
            var Roles = GetUserRoles(user);
            return Roles.Any(x => x == RoleType.MuseumDirector || x == RoleType.DirectorBoard);
        }
        public static bool CheckIfDirector(SessUser user)
        {
            var Roles = GetUserRoles(user);
            return Roles.Any(x => x == RoleType.MuseumDirector || x == RoleType.DirectorBoard);
        }
        public static bool CheckIfDA(SysUser user)
        {
            var Roles = GetUserRoles(user);
            return Roles.Any(x => x == RoleType.DirectorAssistant);
        }
        public static bool CheckIfDA(SessUser user)
        {
            var Roles = GetUserRoles(user);
            return Roles.Any(x => x == RoleType.DirectorAssistant);
        }
        public static bool CheckIfTD(SessUser user)
        {
            var Roles = GetUserRoles(user);
            return Roles.Any(x => x == RoleType.TransientMD);
        }
        public static bool CheckIfDA(UserModel user)
        {
            var Roles = GetUserRoles(user);
            return Roles.Any(x => x == RoleType.DirectorAssistant);
        }
    }
}
