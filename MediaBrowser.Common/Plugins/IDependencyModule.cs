namespace MediaBrowser.Common.Plugins
{
    public interface IDependencyModule
    {
        void BindDependencies(IDependencyContainer container);
    }
}