using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WriteABlog
{
    internal class TextCompletionLogger: DelegatingHandler
    {        
        private ILogger _logger;
        public TextCompletionLogger(HttpMessageHandler innerHandler, ILogger logger)
            : base(innerHandler)
        {
            _logger = logger;
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response;
            if (request.Content != null)
            {
                string content = await request.Content.ReadAsStringAsync();
                _logger.LogInformation("Request content: {Content}", content);
            }
            
            response = await base.SendAsync(request, cancellationToken);
            return response;
        }        
    }
}
