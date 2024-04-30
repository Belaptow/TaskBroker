using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskBroker;

namespace TestingSuite
{
    public static class DirectumProxy
    {
        public static IBroker Resolve()
        {
            return null;//new BrokerService();
        }
        public static bool Wait(Guid queueId, Func<bool> work)
        {


            return false;
        }
    }
}
