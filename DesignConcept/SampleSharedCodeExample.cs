namespace Test
{
    public interface IRequiredInTwoPlugins
    {
        string value { get; set; }
    }

    [ SharedCodeAttribute("IRequiredInTwoPlugins"]
    public class RequiredInTwoPlugins : IRequiredInTwoPlugins, IOtherInterfaceNotToUse
    {
        property string Value { get; set; }
    }
}



