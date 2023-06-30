using System.Collections.Generic;

namespace Aadev.JTF.Editor;
internal class EventManagerContext
{

    private readonly Dictionary<IdentifiersManager, EventManager> imEmMap = new Dictionary<IdentifiersManager, EventManager>();

    public EventManagerContext? Parent { get; }

    public EventManagerContext(EventManagerContext? parent)
    {
        Parent = parent;
    }

    public EventManager GetOrCreate(IdentifiersManager identifiersManager)
    {
        if (imEmMap.TryGetValue(identifiersManager, out EventManager? manager))
        {
            return manager;
        }

        EventManager? p = null;
        IdentifiersManager? ip = identifiersManager.Parent;
        while (ip is not null)
        {
            p = GetOrNull(ip);
            if (p is not null)
                break;
            ip = ip.Parent;
        }

        EventManager newEm2 = new EventManager(identifiersManager, p);
        imEmMap.Add(identifiersManager, newEm2);
        return newEm2;
    }

    public EventManager? GetOrNull(IdentifiersManager? identifiersManager)
    {
        if (identifiersManager is null)
            return null;

        if (imEmMap.TryGetValue(identifiersManager, out EventManager? manager))
        {
            return manager;
        }

        return Parent?.GetOrNull(identifiersManager);
    }
}
