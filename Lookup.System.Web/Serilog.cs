using System;
using System.Diagnostics;
using Microsoft.Owin.Logging;
using Owin;
using ISerilogLogger = Serilog.ILogger;

using System.Threading.Tasks;
using System.Collections.Generic;
using Serilog.Context;

namespace Lookup.System.Web
{

    /// <summary>
    /// Adds a RequestId property to the logging context during request processing.
    /// </summary>
    public class RequestContextMiddleware
    {
        /// <summary>
        /// The property name carrying the request ID.
        /// </summary>
        public const string DefaultRequestIdPropertyName = "RequestId";

        readonly Func<IDictionary<string, object>, Task> _next;
        readonly string _propertyName;

        /// <summary>
        /// Construct the middleware.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="propertyName"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public RequestContextMiddleware(Func<IDictionary<string, object>, Task> next, string propertyName = DefaultRequestIdPropertyName)
        {
            if (next == null)
            {
                throw new ArgumentNullException("next");
            }
            _next = next;
            _propertyName = string.IsNullOrWhiteSpace(propertyName) ? DefaultRequestIdPropertyName : propertyName;
        }

        /// <summary>
        /// Process a request.
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        public async Task Invoke(IDictionary<string, object> environment)
        {
            // There is not yet a standard way to uniquely identify and correlate an owin request
            // ... hence 'RequestId' https://github.com/owin/owin/issues/21
            using (LogContext.PushProperty(_propertyName, Guid.NewGuid()))
            {
                await _next(environment);
            }
        }
    }

    public static class OwinWithSerilog
    {
        public static IAppBuilder UseSerilogRequestContext(this IAppBuilder app, string propertyName = RequestContextMiddleware.DefaultRequestIdPropertyName)
        {
            return app.Use(typeof(RequestContextMiddleware), new object[] { propertyName });
        }

        public static void UseSerilog(this IAppBuilder app, ISerilogLogger logger)
        {
            app.SetLoggerFactory(new SerilogLoggerFactory(logger));
        }
    }

    public class SerilogLoggerFactory : ILoggerFactory
    {
        private readonly ISerilogLogger _logger;

        public SerilogLoggerFactory(ISerilogLogger logger)
        {
            _logger = logger;
        }

        public ILogger Create(string name)
        {
            return new SerilogLogger(_logger);
        }
    }

    public class SerilogLogger : ILogger
    {
        private readonly ISerilogLogger _logger;
        private const string SerilogMessage = "Owin had something to say: {@OwinContext}";

        public SerilogLogger(ISerilogLogger logger)
        {
            _logger = logger;
        }

        public bool WriteCore(TraceEventType eventType, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            var log = new OwinContextLog(eventId, formatter(state, exception));

            switch (eventType)
            {
                case TraceEventType.Critical:
                    _logger.Fatal(exception, SerilogMessage, log);
                    return true;
                case TraceEventType.Error:
                    _logger.Error(exception, SerilogMessage, log);
                    return true;
                case TraceEventType.Information:
                    _logger.Information(exception, SerilogMessage, log);
                    return true;
                case TraceEventType.Warning:
                    _logger.Warning(exception, SerilogMessage, log);
                    return true;
                case TraceEventType.Verbose:
                    _logger.Verbose(exception, SerilogMessage, log);
                    return true;
                default:
                    return false;
            }
        }
    }

    public class OwinContextLog
    {
        public OwinContextLog(int eventId, string message)
        {
            EventId = eventId;
            Message = message;
        }

        public string Message { get; private set; }
        public int EventId { get; private set; }
    }
}