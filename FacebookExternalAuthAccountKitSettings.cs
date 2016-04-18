using Nop.Core.Configuration;

namespace Nop.Plugin.ExternalAuth.FacebookAccountKit
{
    public class FacebookExternalAuthAccountKitSettings : ISettings
    {
        public long FacebookAppId { get; set; }
        
        public string AccountKitSecretToken { get; set; }

    }
}
