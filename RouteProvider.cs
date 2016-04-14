using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.ExternalAuth.FacebookAccountKit
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("Plugin.ExternalAuth.FacebookAccountKit.Login",
                 "Plugins/ExternalAuthFacebookAccountKit/Login",
                 new { controller = "ExternalAuthFacebookAccountKit", action = "Login" },
                 new[] { "Nop.Plugin.ExternalAuth.FacebookAccountKit.Controllers" }
            );

            routes.MapRoute("Plugin.ExternalAuth.FacebookAccountKit.LoginCallback",
                 "Plugins/ExternalAuthFacebook/LoginCallback",
                 new { controller = "ExternalAuthFacebookAccountKit", action = "LoginCallback" },
                 new[] { "Nop.Plugin.ExternalAuth.FacebookAccountKit.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
