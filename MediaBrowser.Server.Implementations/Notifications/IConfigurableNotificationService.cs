namespace MediaBrowser.Server.Implementations.Notifications
{
    public interface IConfigurableNotificationService
    {
        bool IsHidden { get; }
        bool IsEnabled(string notificationType);
    }
}
