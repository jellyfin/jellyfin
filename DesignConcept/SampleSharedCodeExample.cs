namespace Test
{
    /// This code would be part of the dll that is loaded as required.
    
    /// This is the actual interface that will be used by the plugins. As such, it doesn't need any attribute.
    /// It's the interface that is matched with the one provided in the SharedCodeAttribute.
    public interface IRequiredInTwoPlugins
    {
        string value { get; set; }
    }

    /// This is a sample shared code class.    
    [ SharedCodeAttribute("IRequiredInTwoPlugins"]
    public class RequiredInTwoPlugins : IRequiredInTwoPlugins, IOtherInterfaceNotToUse
    {
        property string Value { get; set; }
    }
}



