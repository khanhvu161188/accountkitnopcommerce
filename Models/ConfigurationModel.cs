using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.ExternalAuth.FacebookAccountKit.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.ExternalAuth.FacebookAccountKit.AppId")]
        public long AppId { get; set; }
        public bool AppId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.ExternalAuth.FacebookAccountKit.ClientSecret")]
        public string AccountKitSecretToken { get; set; }
        public bool AccountKitSecretToken_OverrideForStore { get; set; }
    }
}