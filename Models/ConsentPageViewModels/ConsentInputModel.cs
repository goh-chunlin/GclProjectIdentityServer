using System.Collections.Generic;

namespace GclProjectIdentityServer.Models.ConsentPageViewModels
{
    public class ConsentInputModel
    {
        public string Button { get; set; }

        public IEnumerable<string> ScopesConsented { get; set; }

        public bool RememberConsent { get; set; }
        
        public string ReturnUrl { get; set; }
    }
}
