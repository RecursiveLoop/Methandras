using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary
{
    public abstract class Runner
    {
        private readonly ILogger<Runner> _logger;

        protected string _action;

        public Runner(ILogger<Runner> logger)
        {
            _logger = logger;
        }

        public virtual async Task DoAction(string RoleARN)
        {

            _logger.LogDebug(20, "Executing - {Action}", _action);
        }

        public virtual async Task<T> DoAction<T>(string RoleARN) where T : class
        {

            _logger.LogDebug(20, "Executing - {Action}", _action);
            return null;
        }
    }
}
