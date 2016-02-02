namespace MediaBrowser.Server.Implementations.LiveTv.TunerHosts.SatIp
{
    public class SatIpHost /*: BaseTunerHost*/
    {
        //public SatIpHost(IConfigurationManager config, ILogger logger, IJsonSerializer jsonSerializer, IMediaEncoder mediaEncoder)
        //    : base(config, logger, jsonSerializer, mediaEncoder)
        //{
        //}

        //protected override Task<IEnumerable<ChannelInfo>> GetChannelsInternal(TunerHostInfo tuner, CancellationToken cancellationToken)
        //{
        //    throw new NotImplementedException();
        //}

        public static string DeviceType
        {
            get { return "satip"; }
        }

        //public override string Type
        //{
        //    get { return DeviceType; }
        //}

        //protected override Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(TunerHostInfo tuner, string channelId, CancellationToken cancellationToken)
        //{
        //    throw new NotImplementedException();
        //}

        //protected override Task<MediaSourceInfo> GetChannelStream(TunerHostInfo tuner, string channelId, string streamId, CancellationToken cancellationToken)
        //{
        //    throw new NotImplementedException();
        //}

        //protected override Task<bool> IsAvailableInternal(TunerHostInfo tuner, string channelId, CancellationToken cancellationToken)
        //{
        //    throw new NotImplementedException();
        //}

        //protected override bool IsValidChannelId(string channelId)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
