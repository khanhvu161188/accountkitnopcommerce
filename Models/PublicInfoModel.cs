using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.ExternalAuth.FacebookAccountKit.Models
{
    public class PublicInfoModel: BaseNopModel
    {
        [Required]
        public string Code { get; set; }

        [Required]
        public string csrf_nonce { get; set; }
    }

    public class DisplayLoginModel : BaseNopModel
    {
        public bool ShowPhoneNumber { get; set; }

        public string CsrfCode { get; set; }

        public long AppId { get; set; }

        public string Version { get; set; }
    }
    
}
