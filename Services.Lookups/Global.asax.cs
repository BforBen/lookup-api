using System.Web.Routing;

namespace GuildfordBoroughCouncil.Services.Lookups
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            RouteTable.Routes.LowercaseUrls = true;
        }
    }
}
