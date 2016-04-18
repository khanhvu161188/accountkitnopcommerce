using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Policy;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Plugins;
using Nop.Services.Authentication.External;
using Nop.Services.Customers;
using RestSharp;

namespace Nop.Plugin.ExternalAuth.FacebookAccountKit.Core
{
    public class FacebookAccountKitProvicerAuthorizer
    {

        private readonly IOpenAuthenticationService _openAuthenticationService;
        private readonly IPluginFinder _pluginFinder;
        private readonly IStoreContext _storeContext;
        private readonly ExternalAuthenticationSettings _externalAuthenticationSettings;
        private readonly IExternalAuthorizer _authorizer;
        private readonly CustomerSettings _customerSettings;
        private readonly ICustomerService _customerService;
        
        private const string Version = "v1.0";

        private readonly RestClient _client = new RestClient("https://graph.accountkit.com/"+ Version+"/");
        private string domain;
        public FacebookAccountKitProvicerAuthorizer(IOpenAuthenticationService openAuthenticationService, ExternalAuthenticationSettings externalAuthenticationSettings, IPluginFinder pluginFinder, IStoreContext storeContext, IExternalAuthorizer authorizer, CustomerSettings customerSettings, ICustomerService customerService)
        {
            _openAuthenticationService = openAuthenticationService;
            _externalAuthenticationSettings = externalAuthenticationSettings;
            _pluginFinder = pluginFinder;
            _storeContext = storeContext;
            _authorizer = authorizer;
            _customerSettings = customerSettings;
            _customerService = customerService;
            var domainString = _storeContext.CurrentStore.Url.Replace("https://", "").Replace("http://", "");
            if (domainString.EndsWith("/"))
            {
                domainString = domainString.TrimEnd('/');
            }
            domain = "@" + domainString;
        }

        /// <summary>
        /// Authorize response
        /// </summary>
        /// <param name="accessCode"></param>
        /// <param name="returnUrl">Return URL</param>
        /// <returns>Authorize state</returns>
        public AuthorizeState Authorize(string accessCode,string returnUrl,string facebookAppId,string facebookSecretToken)
        {
     
            //create request
            var request = new RestRequest("access_token", Method.GET);

            var accessToken = string.Join("|",
                new List<string> { "AA", facebookAppId, facebookSecretToken });

            request.AddParameter("grant_type", "authorization_code", ParameterType.QueryString);
            request.AddParameter("code", accessCode, ParameterType.QueryString);
            request.AddParameter("access_token", accessToken, ParameterType.QueryString);
            try
            {
                //execute
                var response = _client.Execute(request);

                var responseObj = JsonConvert.DeserializeObject<JObject>(response.Content);
                if (responseObj["access_token"] != null)
                {
                    var accesssToken = responseObj["access_token"].ToString();

                    var getInfoRequest = new RestRequest("me", Method.GET);
                    getInfoRequest.AddParameter("access_token", accesssToken, ParameterType.QueryString);
                    var getInfoResponse = _client.Execute(getInfoRequest);

                    var getInfoResponseObj = JsonConvert.DeserializeObject<JObject>(getInfoResponse.Content);
                    var email = "";
                    var usePhoneNumberForLogin = false;
                    if (getInfoResponseObj["phone"] != null)
                    {
                        //check setting for user
                        if (!_customerSettings.UsernamesEnabled ||
                            _customerSettings.UserRegistrationType == UserRegistrationType.EmailValidation)
                        {
                            var errorRes = new AuthorizeState(returnUrl, OpenAuthenticationStatus.Error);
                            errorRes.Errors.Add("Website not support login via phone number");
                            return errorRes;
                        }
                        usePhoneNumberForLogin = true;
                        //login via phone Number
                        email = getInfoResponseObj["phone"]["number"].ToString() + domain;

                    }
                    else if (getInfoResponseObj["email"] != null)
                    {
                        //login via email Address
                        email = getInfoResponseObj["email"]["address"].ToString();
                    }

                    var processor = _openAuthenticationService.LoadExternalAuthenticationMethodBySystemName("ExternalAuth.FacebookAccountKit");
                    if (processor == null ||
                        !processor.IsMethodActive(_externalAuthenticationSettings) ||
                        !processor.PluginDescriptor.Installed ||
                        !_pluginFinder.AuthenticateStore(processor.PluginDescriptor, _storeContext.CurrentStore.Id))
                        throw new NopException("Facebook module cannot be loaded");

                    var parameters = new FacebookAuthenticationParameters(Provider.SystemName)
                    {

                        ExternalIdentifier = getInfoResponseObj["id"].ToString(),
                        OAuthToken = accesssToken,
                        OAuthAccessToken = getInfoResponseObj["id"].ToString(),

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

                    // check use phonenumber and result 
                    if (usePhoneNumberForLogin &&
                        (result.Status == OpenAuthenticationStatus.AutoRegisteredStandard ||
                         result.Status == OpenAuthenticationStatus.AutoRegisteredEmailValidation ||
                         result.Status == OpenAuthenticationStatus.AutoRegisteredAdminApproval))
                    {
                        var customer = _customerService.GetCustomerByEmail(email);
                        if (customer != null)
                        {
                            // remove domain from username
                            var phoneNumer = customer.Username.Replace(domain, "");
                            customer.Username = phoneNumer;
                            _customerService.UpdateCustomer(customer);
                        }
                    }

                    return new AuthorizeState(returnUrl, result);
             
                }
                var res = new AuthorizeState(returnUrl, OpenAuthenticationStatus.Error);
                res.Errors.Add("Cannot get access token from facebook account kit");
                return res;
            }
            catch (Exception ex)
            {
                Trace.TraceError("error:" + ex.InnerException);
                var res = new AuthorizeState(returnUrl,OpenAuthenticationStatus.Error);
                res.Errors.Add(ex.Message);
                return res;
            }
            
        }
    }
}
