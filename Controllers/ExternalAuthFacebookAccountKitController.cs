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

        private readonly ISettingService _settingService;
        private readonly IPermissionService _permissionService;
        private readonly IStoreService _storeService;
        private readonly IWorkContext _workContext;
        private readonly ILocalizationService _localizationService;
        private readonly FacebookAccountKitProvicerAuthorizer _externalProviderAuthorizer;
        private readonly CustomerSettings _customerSettings;

        #region Ctor

        public ExternalAuthFacebookAccountKitController(ISettingService settingService, IPermissionService permissionService, IStoreService storeService, IWorkContext workContext,
            ILocalizationService localizationService, FacebookAccountKitProvicerAuthorizer externalProviderAuthorizer, CustomerSettings customerSettings)
        {
            _settingService = settingService;
            _permissionService = permissionService;
            _storeService = storeService;
            _workContext = workContext;
            _localizationService = localizationService;
            _externalProviderAuthorizer = externalProviderAuthorizer;
            _customerSettings = customerSettings;
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
                AccountKitSecretToken =  facebookExternalAuthSettings.AccountKitSecretToken,
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
            facebookExternalAuthSettings.AccountKitSecretToken = model.AccountKitSecretToken;
            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            if (model.AppId_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(facebookExternalAuthSettings, x => x.FacebookAppId, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(facebookExternalAuthSettings, x => x.FacebookAppId, storeScope);

            if (model.AccountKitSecretToken_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(facebookExternalAuthSettings, x => x.AccountKitSecretToken, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(facebookExternalAuthSettings, x => x.AccountKitSecretToken, storeScope);


            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        private string GenerateTokenHeaderValue()
        {
            string cookieToken, formToken;
            AntiForgery.GetTokens(null, out cookieToken, out formToken);
            return cookieToken + ":" + formToken;
        }
        [ChildActionOnly]
        public ActionResult PublicInfo()
        {
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var facebookExternalAuthSettings = _settingService.LoadSetting<FacebookExternalAuthAccountKitSettings>(storeScope);


            var model = new DisplayLoginModel
            {
                CsrfCode = GenerateTokenHeaderValue(),
                Version = Provider.Version,
                AppId = facebookExternalAuthSettings.FacebookAppId
            };
            if (!_customerSettings.UsernamesEnabled ||
                _customerSettings.UserRegistrationType == UserRegistrationType.EmailValidation)
            {
                model.ShowPhoneNumber = false;
            }
            else
            {
                model.ShowPhoneNumber = true;
            }
            

            return View("~/Plugins/ExternalAuthAccountKit.Facebook/Views/ExternalAuthFacebookAccountKit/PublicInfo.cshtml", model);
        }
        [HttpPost]
     
        public ActionResult PublicInfo(PublicInfoModel model,string returnUrl)
        {
            var cookieToken = "";
            var formToken = "";
            var tokens = model.csrf_nonce.Split(':');
            if (tokens.Length == 2)
            {
                cookieToken = tokens[0].Trim();
                formToken = tokens[1].Trim();
            }
            try
            {
                //validate csrf
                AntiForgery.Validate(cookieToken, formToken);
            }
            catch (Exception ex)
            {
                ExternalAuthorizerHelper.AddErrorsToDisplay("CSRF is not correct");
                return new RedirectResult(Url.LogOn(""));
            }
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var facebookExternalAuthSettings = _settingService.LoadSetting<FacebookExternalAuthAccountKitSettings>(storeScope);

            var result = _externalProviderAuthorizer.Authorize(model.Code, returnUrl, facebookExternalAuthSettings.FacebookAppId.ToString(), facebookExternalAuthSettings.AccountKitSecretToken);

            switch (result.AuthenticationStatus)
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
            if (result.Result != null)
                return result.Result;

            
            return HttpContext.Request.IsAuthenticated 
                ? new RedirectResult(!string.IsNullOrEmpty(returnUrl) ? returnUrl : "~/") 
                : new RedirectResult(Url.LogOn(returnUrl));
        }

    }
}
