
namespace MediaBrowser.Common.Implementations.Security
{
    internal class SuppporterInfoResponse
    {
        public string email { get; set; }
        public string supporterKey { get; set; }
        public int totalRegs { get; set; }
        public int totalMachines { get; set; }
        public string expDate { get; set; }
        public string regDate { get; set; }
        public string planType { get; set; }
    }
}
