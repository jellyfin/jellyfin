using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Dlna.Channels
{
    public class DlnaChannelFactory : IChannelFactory
    {
        public IEnumerable<IChannel> GetChannels()
        {
            // Check config here
            // If user wants all channels separate, return them all
            // If user wants one parent channel, return just that one

            return new List<IChannel>()
            {
                //new DummyChannel("test 1"),
                //new DummyChannel("test 2")
            };
        }
    }

    public class DummyChannel : IChannel
    {
        private readonly string _name;

        public DummyChannel(string name)
        {
            _name = name;
        }

        public string Name
        {
            get { return _name; }
        }

        public string Description
        {
            get { return "Dummy Channel"; }
        }

        public string DataVersion
        {
            get { return "1"; }
        }

        public string HomePageUrl
        {
            get { return "http://www.google.com"; }
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.GeneralAudience; }
        }

        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {

            };
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        public Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ChannelItemResult());
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            return Task.FromResult(new DynamicImageResponse());
        }

        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return new List<ImageType>();
        }
    }
}
