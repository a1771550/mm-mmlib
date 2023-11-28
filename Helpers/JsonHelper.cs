using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLib.Helpers
{
    public class JsonHelper
    {
        public static string Test()
        {
            List<string> test = new List<string>();
            test.Add("test1");
            test.Add("test2");
            return System.Text.Json.JsonSerializer.Serialize(test);
        }
    }
}
