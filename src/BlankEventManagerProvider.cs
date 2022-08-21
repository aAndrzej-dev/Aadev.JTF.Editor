using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aadev.JTF.Editor
{
    internal class BlankEventManagerProvider : IEventManagerProvider
    {
        private Dictionary<IIdentifiersManager, EventManager> identifiersEventManagersMap;

        public BlankEventManagerProvider()
        {
            identifiersEventManagersMap = new Dictionary<IIdentifiersManager, EventManager>();
        }

        public EventManager GetEventManager(IIdentifiersManager identifiersManager)
        {
            if (identifiersEventManagersMap.ContainsKey(identifiersManager))
                return identifiersEventManagersMap[identifiersManager];
            EventManager? em = new EventManager(identifiersManager);

            identifiersEventManagersMap.Add(identifiersManager, em);

            return em;
        }
    }
}
