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
using Nop.Plugin.ExternalAuth.FacebookAccountKit.Core;
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
        private readonly IExternalAuthorizer _authorizer;
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
            ILocalizationService localizationService, IExternalAuthorizer authorizer)
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
            _authorizer = authorizer;
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
     
        public ActionResult PublicInfo(PublicInfoModel model,string returnUrl)
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
                    var accesssToken = responseObj["access_token"].ToString();

                    var meRequest = new RestRequest("me", Method.GET);
                    meRequest.AddParameter("access_token", accesssToken, ParameterType.QueryString);
                    var response2 = _client.Execute(meRequest);

                    var responseObj2 = JsonConvert.DeserializeObject<JObject>(response2.Content);
                    var email = "";
                    if (responseObj2["phone"]!=null)
                    {
                        //login via phone Number
                        var phoneNum = responseObj2["phone"]["number"].ToString();
                        email = phoneNum += "@xxx.com";

                    }
                    else if (responseObj2["email"]!=null)
                    {
                        //login via email Address
                        email = responseObj2["email"]["address"].ToString();
                    }
                     
                    var processor = _openAuthenticationService.LoadExternalAuthenticationMethodBySystemName("ExternalAuth.FacebookAccountKit");
                    if (processor == null ||
                        !processor.IsMethodActive(_externalAuthenticationSettings) ||
                        !processor.PluginDescriptor.Installed ||
                        !_pluginFinder.AuthenticateStore(processor.PluginDescriptor, _storeContext.CurrentStore.Id))
                        throw new NopException("Facebook module cannot be loaded");

                    var parameters = new FacebookAuthenticationParameters(Provider.SystemName)
                    {

                        ExternalIdentifier = responseObj2["id"].ToString(),
                        OAuthToken = accesssToken,
                        OAuthAccessToken = responseObj2["id"].ToString(),
                        
                    };
                    //add to claim of parameter
                    var claims = new UserClaims
                    {
                        Contact = new ContactClaims()
                        {
                            Email = email
                        }
                    };
                    parameters.AddClaim(claims);


                    var result = _authorizer.Authorize(parameters);
                    //if (result.Status == OpenAuthenticationStatus.AutoRegisteredStandard)
                    //{
                    //    //cheat here
                    //    result = new AuthorizationResult(OpenAuthenticationStatus.Authenticated);
                    //}
                    var res= new AuthorizeState(returnUrl, result);
                    switch (res.AuthenticationStatus)
                    {
                        case OpenAuthenticationStatus.Error:
                            {
                                if (!result.Success)
                                    foreach (var error in result.Errors)
                                        ExternalAuthorizerHelper.AddErrorsToDisplay(error);

                                return new RedirectResult(Url.LogOn(returnUrl));
                            }
                        case OpenAuthenticationStatus.AssociateOnLogon:
                            {
                                return new RedirectResult(Url.LogOn(returnUrl));
                            }
                        case OpenAuthenticationStatus.AutoRegisteredEmailValidation:
                            {
                                //result
                                return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.EmailValidation });
                            }
                        case OpenAuthenticationStatus.AutoRegisteredAdminApproval:
                            {
                                return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.AdminApproval });
                            }
                        case OpenAuthenticationStatus.AutoRegisteredStandard:
                            {
                                return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.Standard });
                            }
                        default:
                            break;
                    }
                    if (res.Result != null)
                        return res.Result;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("error:" + ex.InnerException);
               
            }
            var isAuthenticated = HttpContext.Request.IsAuthenticated;

           
            // var returnUrl = HttpContext.Request.QueryString["returnUrl"];
            return HttpContext.Request.IsAuthenticated 
                ? new RedirectResult(!string.IsNullOrEmpty(returnUrl) ? returnUrl : "~/") 
                : new RedirectResult(Url.LogOn(returnUrl));

            //return View("~/Plugins/ExternalAuthAccountKit.Facebook/Views/ExternalAuthFacebookAccountKit/PublicInfo.cshtml");


        }

    }
}
