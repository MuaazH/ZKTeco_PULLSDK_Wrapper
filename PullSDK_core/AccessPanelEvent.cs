namespace PullSDK_core;

public class AccessPanelEvent
{
    public readonly AccessPanelDoorsStatus? DoorsStatus;
    public readonly AccessPanelRtEvent[] Events;

    public AccessPanelEvent(AccessPanelDoorsStatus? doorsStatus, AccessPanelRtEvent[] events)
    {
        this.DoorsStatus = doorsStatus;
        this.Events = events;
    }
}