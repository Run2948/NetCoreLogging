using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Log4NetSolution.Common
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            var loggingBuilder = new StringBuilder();

            if (context.HttpContext.Request.GetDisplayUrl() != null)
                loggingBuilder.AppendLine($"\tUrl: {context.HttpContext.Request.GetDisplayUrl()}");

            loggingBuilder.AppendLine($"\tIp: {context.HttpContext.Connection.RemoteIpAddress}");

#if DEBUG
            foreach (var key in context.HttpContext.Request.Headers.Keys)
            {
                loggingBuilder.AppendLine($"\t{key}: {context.HttpContext.Request.Headers[key]}");
            }
#endif

            loggingBuilder.AppendLine($"\tError Message: {context.Exception.Message}");
            if (context.Exception.InnerException != null)
            {
                PrintInnerException(context.Exception.InnerException, loggingBuilder);
            }

            loggingBuilder.AppendLine($"\tError HelpLink: {context.Exception.HelpLink}");
            loggingBuilder.AppendLine($"\tError StackTrace: {context.Exception.StackTrace}");

            _logger.LogError(loggingBuilder.ToString());
        }

        public void PrintInnerException(Exception ex, StringBuilder loggingBuilder)
        {
            loggingBuilder.AppendLine($"\tError InnerMessage: {ex.Message}");
            if (ex.InnerException != null)
            {
                PrintInnerException(ex.InnerException, loggingBuilder);
            }
        }
    }
}