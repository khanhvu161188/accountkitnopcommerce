using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.ExternalAuth.FacebookAccountKit.Core
{
    public static class Provider
    {
        public static string SystemName
        {
            get
            {
                return "ExternalAuth.FacebookAccountKit";
            }
        }

        public static string Version
        {
            get { return "v1.0"; }
        }
    }
}
