namespace Aadev.JTF.Editor
{
    internal interface IEventManagerProvider
    {
        EventManager GetEventManager(IIdentifiersManager identifiersManager);
    }
}
