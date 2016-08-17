using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Xml;
using System.Web.Http.Description;

using System.Web.Http.Cors;
using System.Runtime.Caching;

namespace GuildfordBoroughCouncil.Lookup.Api.Controllers.v1
{
    [EnableCors("http://www2.guildford.gov.uk,https://www2.guildford.gov.uk,http://cusco.other.gov.uk", "*", "*")]
    [RoutePrefix("v1")]
    public class BankHolidaysAndClosuresController : ApiController
    {
        [HttpGet]
        [Route("~/bankholidaysandclosures")] // Legacy
        [Route("bank-holidays-closures")]
        [ResponseType(typeof(IEnumerable<DateTime>))]
        public IHttpActionResult List()
        {
            // https://www.gov.uk/bank-holidays
            // http://loop.guildford.gov.uk/Lists/Bank%20holidays%20and%20office%20closures/AllItems.aspx

            #region Check cache to see if the records are there
            var c = MemoryCache.Default;

            var CachedDates = (List<DateTime>)c.Get("BankHolidaysAndClosures");

            if (CachedDates != null && CachedDates.Count() > 0)
            {
                Serilog.Log.Information("BankHolidaysAndClosures cache hit");
                return Ok(CachedDates);
            }
            else
            {
                Serilog.Log.Information("BankHolidaysAndClosures cache miss");
            }
            #endregion

            #region Query SharePoint

            var Dates = new List<DateTime>();

            using (var Sp = new Sp12Lists.ListsSoapClient())
            {
                Sp.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
                Sp.ClientCredentials.Windows.ClientCredential = CredentialCache.DefaultNetworkCredentials;

                var Items = Sp.GetListItems("Bank holidays and office closures", "{54AF36FD-5D3A-4FC3-9EDA-576BBD88C21E}", null, null, null, null, null);

                var xmlDocResult = new XmlDocument();
                xmlDocResult.LoadXml(Items.InnerXml);
                XmlNodeList rows = xmlDocResult.GetElementsByTagName("z:row");

                foreach (XmlNode attribute in rows)
                {
                    var StartDate = Convert.ToDateTime(attribute.Attributes["ows_EventDate"].Value);
                    var EndDate = Convert.ToDateTime(attribute.Attributes["ows_EndDate"].Value);

                    while (StartDate <= EndDate)
                    {
                        Dates.Add(StartDate);
                        StartDate = StartDate.AddDays(1);
                    }
                }

                rows = null;
                xmlDocResult = null;
            }

            #endregion 

            #region Add data to MemoryCache for 6 hours
            try
            {
                c.Add("BankHolidaysAndClosures", Dates, new CacheItemPolicy() { AbsoluteExpiration = new DateTimeOffset(DateTime.Now.AddHours(6)) });
            }
            catch { }
            #endregion

            return Ok(Dates);
        }
    }
}