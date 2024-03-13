using MYOBLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLib.Models.MYOB
{
    public class ABSSModelV2:ABSSModel
    {
        public string File { get; set; }
        public string AppExe { get; set; }
        public string Key { get; set; }
        public ABSSModelV2() { }
        public ABSSModelV2(List<string> sqllist, string Driver, string UserId, string UserPass, string File, string AppExe, string Key)
        {
            this.sqllist = sqllist;
            this.Driver = Driver;
            this.UserId = UserId;
            this.UserPass = UserPass;
            this.File = File;
            this.AppExe = AppExe;
            this.Key=Key;
        }
    }

}
