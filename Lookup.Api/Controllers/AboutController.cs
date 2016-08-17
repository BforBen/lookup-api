using System;
using System.Web.Http;
using System.Web.Http.Description;

namespace GuildfordBoroughCouncil.Lookup.Api.Controllers
{
    /// <summary>
    /// About controller
    /// </summary>
    public class LookupAboutController : ApiController
    {
        [HttpGet]
        [Route("_about")]
        [ResponseType(typeof(string))]
        public IHttpActionResult About()
        {
            var Ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return Ok(string.Format("{0}.{1}.{2}", Ver.Major, Ver.Minor, Ver.Build));
        }
    }
}
