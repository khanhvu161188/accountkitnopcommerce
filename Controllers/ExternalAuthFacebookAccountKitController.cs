using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Plugins;
using Nop.Plugin.ExternalAuth.FacebookAccountKit.Models;
using Nop.Services.Authentication.External;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Security;
using RestSharp;

namespace Nop.Plugin.ExternalAuth.FacebookAccountKit.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public class ExternalAuthFacebookAccountKitController: BasePluginController
    {
        #region fields

        private readonly ISettingService _settingService;
        private readonly IOpenAuthenticationService _openAuthenticationService;
        private readonly ExternalAuthenticationSettings _externalAuthenticationSettings;
        private readonly IPermissionService _permissionService;
        private readonly IStoreContext _storeContext;
        private readonly IStoreService _storeService;
        private readonly IWorkContext _workContext;
        private readonly IPluginFinder _pluginFinder;
        private readonly ILocalizationService _localizationService;

        #region Ctor

        public ExternalAuthFacebookAccountKitController(ISettingService settingService,
            IOpenAuthenticationService openAuthenticationService,
            ExternalAuthenticationSettings externalAuthenticationSettings, IPermissionService permissionService,
            IStoreContext storeContext, IStoreService storeService, IWorkContext workContext, IPluginFinder pluginFinder,
            ILocalizationService localizationService)
        {
            _settingService = settingService;
            _openAuthenticationService = openAuthenticationService;
            _externalAuthenticationSettings = externalAuthenticationSettings;
            _permissionService = permissionService;
            _storeContext = storeContext;
            _storeService = storeService;
            _workContext = workContext;
            _pluginFinder = pluginFinder;
            _localizationService = localizationService;
        }

        #endregion


        #endregion




        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageExternalAuthenticationMethods))
                return Content("Access denied");

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var facebookExternalAuthSettings = _settingService.LoadSetting<FacebookExternalAuthAccountKitSettings>(storeScope);

            var model = new ConfigurationModel
            {
                AppId = facebookExternalAuthSettings.FacebookAppId,
                ActiveStoreScopeConfiguration = storeScope
            };

            if (storeScope > 0)
            {
                model.AppId_OverrideForStore = _settingService.SettingExists(facebookExternalAuthSettings, x => x.FacebookAppId, storeScope);
            }

            return View("~/Plugins/ExternalAuthAccountKit.Facebook/Views/ExternalAuthFacebookAccountKit/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageExternalAuthenticationMethods))
                return Content("Access denied");

            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var facebookExternalAuthSettings = _settingService.LoadSetting<FacebookExternalAuthAccountKitSettings>(storeScope);

            //save settings
            facebookExternalAuthSettings.FacebookAppId = model.AppId;
         
            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            if (model.AppId_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(facebookExternalAuthSettings, x => x.FacebookAppId, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(facebookExternalAuthSettings, x => x.FacebookAppId, storeScope);

           

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }


        [ChildActionOnly]
        public ActionResult PublicInfo()
        {
            return View("~/Plugins/ExternalAuthAccountKit.Facebook/Views/ExternalAuthFacebookAccountKit/PublicInfo.cshtml");
        }
        [HttpPost]
     
        public ActionResult PublicInfo(PublicInfoModel model)
        {
            string cookieToken = "";
            string formToken = "";
            
            string[] tokens = model.csrf_nonce.Split(':');
            if (tokens.Length == 2)
            {
                cookieToken = tokens[0].Trim();
                formToken = tokens[1].Trim();
            }
            try
            {
                AntiForgery.Validate(cookieToken, formToken);
            }
            catch (Exception ex)
            {
                ExternalAuthorizerHelper.AddErrorsToDisplay("CSRF is not correct");
                return new RedirectResult(Url.LogOn(""));
            }

             RestClient _client = new RestClient("https://graph.accountkit.com/v1.0/");

            //create request
            var request = new RestRequest("access_token", Method.GET);

            var accessToken = string.Join("|",
                new List<string> {"AA", "1690029441247631", "c54110f9c40ba130e42a151741f4379c"});

            request.AddParameter("grant_type", "authorization_code", ParameterType.QueryString);
            request.AddParameter("code", model.Code, ParameterType.QueryString);
            request.AddParameter("access_token", accessToken, ParameterType.QueryString);
            try
            {
                //execute
                var response = _client.Execute(request);

                var responseObj = JsonConvert.DeserializeObject<JObject>(response.Content);
                if (responseObj["access_token"] != null)
                {
                    var accesss_token = responseObj["access_token"].ToString();

                    var me_request = new RestRequest("me", Method.GET);
                    me_request.AddParameter("access_token", accesss_token, ParameterType.QueryString);
                    var response2 = _client.Execute(me_request);

                    var responseObj2 = JsonConvert.DeserializeObject<JObject>(response2.Content);
                    if (responseObj2["phone"]!=null)
                    {
                        var phone_num = responseObj2["phone"]["number"];
                    }
                    else if (responseObj2["email"]!=null)
                    {
                        var email_addr = responseObj2["email"]["address"];
                    }
                }

                //// get account details at /me endpoint
                //var me_endpoint_url = me_endpoint_base_url + '?access_token=' + respBody.access_token;
                //Request.get({ url: me_endpoint_url, json: true }, function(err, resp, respBody) {
                //    // send login_success.html
                //    if (respBody.phone)
                //    {
                //        view.phone_num = respBody.phone.number;
                //    }
                //    else if (respBody.email)
                //    {
                //        view.email_addr = respBody.email.address;
                //    }
                //    var html = Mustache.to_html(loadLoginSuccess(), view);
                //    response.send(html);
                //});

            }
            catch (Exception ex)
            {
                Trace.TraceError("error:" + ex.InnerException);
               
            }

            return View("~/Plugins/ExternalAuthAccountKit.Facebook/Views/ExternalAuthFacebookAccountKit/PublicInfo.cshtml");


        }

        public ActionResult Login(string returnUrl)
        {
            return View("");
        }
    }
}
