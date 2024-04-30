using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskBroker
{
    public class ActionLogger : ILogger
    {
        Action<string, object[]> _logError;
        Action<string, object[]> _logDebug;
        public ActionLogger(Action<string, object[]> logError, Action<string, object[]> logDebug)
        {
            _logError = logError;
            _logDebug = logDebug;
        }

        public void UpdateLoggers(Action<string, object[]> logError, Action<string, object[]> logDebug)
        {
            _logError = logError;
            _logDebug = logDebug;
        }

        public void DebugFormat(string format, params object[] args)
        {
            _logDebug(format, args);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            _logError(format, args);
        }
    }
}
