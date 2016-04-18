using System.Web.Routing;
using Nop.Core.Plugins;
using Nop.Services.Authentication.External;
using Nop.Services.Configuration;
using Nop.Services.Localization;

namespace Nop.Plugin.ExternalAuth.FacebookAccountKit
{
    /// <summary>
    /// Facebook account kit externalAuth processor
    /// </summary>
    public class FacebookExternalAuthAccountKitMethod:BasePlugin, IExternalAuthenticationMethod
    {
        #region Fields

        private readonly ISettingService _settingService;

        #endregion

        #region Ctor

        public FacebookExternalAuthAccountKitMethod(ISettingService settingService)
        {
            _settingService = settingService;
        }

        #endregion

        #region Method


        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName,
            out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "ExternalAuthFacebookAccountKit";
            routeValues = new RouteValueDictionary
            {
                {"Namespaces", "Nop.Plugin.ExternalAuth.FacebookAccountKit.Controllers"},
                {"area", null}
            };
        }

        /// <summary>
        /// Gets a route for displaying plugin in public store
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetPublicInfoRoute(out string actionName, out string controllerName,
            out RouteValueDictionary routeValues)
        {
            actionName = "PublicInfo";
            controllerName = "ExternalAuthFacebookAccountKit";
            routeValues = new RouteValueDictionary
            {
                {"Namespaces", "Nop.Plugin.ExternalAuth.FacebookAccountKit.Controllers"},
                {"area", null}
            };
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            //settings
            var settings = new FacebookExternalAuthAccountKitSettings
            {
                FacebookAppId = 123
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.ExternalAuth.FacebookAccountKit.Login", "Login with Facebook Account Kit");
            this.AddOrUpdatePluginLocaleResource("Plugins.ExternalAuth.FacebookAccountKit.AppId", "App ID/API Key");
            this.AddOrUpdatePluginLocaleResource("Plugins.ExternalAuth.FacebookAccountKit.AppId.Hint",
                "Enter your app ID/API key here. You can find it on your FaceBook application page.");
            this.AddOrUpdatePluginLocaleResource("Plugins.ExternalAuth.FacebookAccountKit.ClientSecret", "Account Kit App Secret");
            this.AddOrUpdatePluginLocaleResource("Plugins.ExternalAuth.FacebookAccountKit.ClientSecret.Hint",
                "Enter your Account Kit App Secret here. You can find it on your FaceBook application page.");

            base.Install();
        }

        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<FacebookExternalAuthAccountKitSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.ExternalAuth.FacebookAccountKit.Login");
            this.DeletePluginLocaleResource("Plugins.ExternalAuth.FacebookAccountKit.ClientKeyIdentifier");
            this.DeletePluginLocaleResource("Plugins.ExternalAuth.FacebookAccountKit.ClientKeyIdentifier.Hint");
            this.DeletePluginLocaleResource("Plugins.ExternalAuth.FacebookAccountKit.ClientSecret");
            this.DeletePluginLocaleResource("Plugins.ExternalAuth.FacebookAccountKit.ClientSecret.Hint");

            base.Uninstall();
        }

        #endregion

    }
}
