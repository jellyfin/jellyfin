using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Jellyfin.Extensions.Json;
using Jellyfin.LiveTv.Listings.SchedulesDirectDtos;
using Xunit;

namespace Jellyfin.LiveTv.Tests.SchedulesDirect
{
    public class SchedulesDirectDeserializeTests
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public SchedulesDirectDeserializeTests()
        {
            _jsonOptions = JsonDefaults.Options;
        }

        /// <summary>
        /// /token response.
        /// </summary>
        [Fact]
        public void Deserialize_Token_Response_Live_Success()
        {
            var bytes = File.ReadAllBytes("Test Data/SchedulesDirect/token_live_response.json");
            var tokenDto = JsonSerializer.Deserialize<TokenDto>(bytes, _jsonOptions);

            Assert.NotNull(tokenDto);
            Assert.Equal(0, tokenDto!.Code);
            Assert.Equal("OK", tokenDto.Message);
            Assert.Equal("AWS-SD-web.1", tokenDto.ServerId);
            Assert.Equal(new DateTime(2016, 08, 23, 13, 55, 25, DateTimeKind.Utc), tokenDto.TokenTimestamp);
            Assert.Equal("f3fca79989cafe7dead71beefedc812b", tokenDto.Token);
        }

        /// <summary>
        /// /token response.
        /// </summary>
        [Fact]
        public void Deserialize_Token_Response_Offline_Success()
        {
            var bytes = File.ReadAllBytes("Test Data/SchedulesDirect/token_offline_response.json");
            var tokenDto = JsonSerializer.Deserialize<TokenDto>(bytes, _jsonOptions);

            Assert.NotNull(tokenDto);
            Assert.Equal(3_000, tokenDto!.Code);
            Assert.Equal("Server offline for maintenance.", tokenDto.Message);
            Assert.Equal("20141201.web.1", tokenDto.ServerId);
            Assert.Equal(new DateTime(2015, 04, 23, 00, 03, 32, DateTimeKind.Utc), tokenDto.TokenTimestamp);
            Assert.Equal("CAFEDEADBEEFCAFEDEADBEEFCAFEDEADBEEFCAFE", tokenDto.Token);
            Assert.Equal("SERVICE_OFFLINE", tokenDto.Response);
        }

        /// <summary>
        /// /schedules request.
        /// </summary>
        [Fact]
        public void Serialize_Schedule_Request_Success()
        {
            var expectedString = File.ReadAllText("Test Data/SchedulesDirect/schedules_request.json").Trim();

            var requestObject = new RequestScheduleForChannelDto[]
            {
                new RequestScheduleForChannelDto
                {
                    StationId = "20454",
                    Date = new[]
                    {
                        "2015-03-13",
                        "2015-03-17"
                    }
                },
                new RequestScheduleForChannelDto
                {
                    StationId = "10021",
                    Date = new[]
                    {
                        "2015-03-12",
                        "2015-03-13"
                    }
                }
            };

            var requestString = JsonSerializer.Serialize(requestObject, _jsonOptions);
            Assert.Equal(expectedString, requestString);
        }

        /// <summary>
        /// /schedules response.
        /// </summary>
        [Fact]
        public void Deserialize_Schedule_Response_Success()
        {
            var bytes = File.ReadAllBytes("Test Data/SchedulesDirect/schedules_response.json");
            var days = JsonSerializer.Deserialize<IReadOnlyList<DayDto>>(bytes, _jsonOptions);

            Assert.NotNull(days);
            Assert.Single(days);

            var dayDto = days[0];
            Assert.Equal("20454", dayDto.StationId);
            Assert.Equal(2, dayDto.Programs.Count);

            Assert.Equal("SH005371070000", dayDto.Programs[0].ProgramId);
            Assert.Equal(new DateTime(2015, 03, 03, 00, 00, 00, DateTimeKind.Utc), dayDto.Programs[0].AirDateTime);
            Assert.Equal(1_800, dayDto.Programs[0].Duration);
            Assert.Equal("Sy8HEMBPcuiAx3FBukUhKQ", dayDto.Programs[0].Md5);
            Assert.True(dayDto.Programs[0].New);
            Assert.Equal(2, dayDto.Programs[0].AudioProperties.Count);
            Assert.Equal("stereo", dayDto.Programs[0].AudioProperties[0]);
            Assert.Equal("cc", dayDto.Programs[0].AudioProperties[1]);
            Assert.Single(dayDto.Programs[0].VideoProperties);
            Assert.Equal("hdtv", dayDto.Programs[0].VideoProperties[0]);
        }

        /// <summary>
        /// /programs response.
        /// </summary>
        [Fact]
        public void Deserialize_Program_Response_Success()
        {
            var bytes = File.ReadAllBytes("Test Data/SchedulesDirect/programs_response.json");
            var programDtos = JsonSerializer.Deserialize<IReadOnlyList<ProgramDetailsDto>>(bytes, _jsonOptions);

            Assert.NotNull(programDtos);
            Assert.Equal(2, programDtos!.Count);
            Assert.Equal("EP000000060003", programDtos[0].ProgramId);
            Assert.Single(programDtos[0].Titles);
            Assert.Equal("'Allo 'Allo!", programDtos[0].Titles[0].Title120);
            Assert.Equal("Series", programDtos[0].EventDetails?.SubType);
            Assert.Equal("en", programDtos[0].Descriptions?.Description1000[0].DescriptionLanguage);
            Assert.Equal("A disguised British Intelligence officer is sent to help the airmen.", programDtos[0].Descriptions?.Description1000[0].Description);
            Assert.Equal(new DateTime(1985, 11, 04), programDtos[0].OriginalAirDate);
            Assert.Single(programDtos[0].Genres);
            Assert.Equal("Sitcom", programDtos[0].Genres[0]);
            Assert.Equal("The Poloceman Cometh", programDtos[0].EpisodeTitle150);
            Assert.Equal(2, programDtos[0].Metadata[0].Gracenote?.Season);
            Assert.Equal(3, programDtos[0].Metadata[0].Gracenote?.Episode);
            Assert.Equal(13, programDtos[0].Cast.Count);
            Assert.Equal("383774", programDtos[0].Cast[0].PersonId);
            Assert.Equal("392649", programDtos[0].Cast[0].NameId);
            Assert.Equal("Gorden Kaye", programDtos[0].Cast[0].Name);
            Assert.Equal("Actor", programDtos[0].Cast[0].Role);
            Assert.Equal("01", programDtos[0].Cast[0].BillingOrder);
            Assert.Equal(3, programDtos[0].Crew.Count);
            Assert.Equal("354407", programDtos[0].Crew[0].PersonId);
            Assert.Equal("363281", programDtos[0].Crew[0].NameId);
            Assert.Equal("David Croft", programDtos[0].Crew[0].Name);
            Assert.Equal("Director", programDtos[0].Crew[0].Role);
            Assert.Equal("01", programDtos[0].Crew[0].BillingOrder);
        }

        /// <summary>
        /// /metadata/programs response.
        /// </summary>
        [Fact]
        public void Deserialize_Metadata_Programs_Response_Success()
        {
            var bytes = File.ReadAllBytes("Test Data/SchedulesDirect/metadata_programs_response.json");
            var showImagesDtos = JsonSerializer.Deserialize<IReadOnlyList<ShowImagesDto>>(bytes, _jsonOptions);

            Assert.NotNull(showImagesDtos);
            Assert.Single(showImagesDtos!);
            Assert.Equal("SH00712240", showImagesDtos[0].ProgramId);
            Assert.Equal(4, showImagesDtos[0].Data.Count);
            Assert.Equal("135", showImagesDtos[0].Data[0].Width);
            Assert.Equal("180", showImagesDtos[0].Data[0].Height);
            Assert.Equal("assets/p282288_b_v2_aa.jpg", showImagesDtos[0].Data[0].Uri);
            Assert.Equal("Sm", showImagesDtos[0].Data[0].Size);
            Assert.Equal("3x4", showImagesDtos[0].Data[0].Aspect);
            Assert.Equal("Banner-L3", showImagesDtos[0].Data[0].Category);
            Assert.Equal("yes", showImagesDtos[0].Data[0].Text);
            Assert.Equal("true", showImagesDtos[0].Data[0].Primary);
            Assert.Equal("Series", showImagesDtos[0].Data[0].Tier);
        }

        /// <summary>
        /// /headends response.
        /// </summary>
        [Fact]
        public void Deserialize_Headends_Response_Success()
        {
            var bytes = File.ReadAllBytes("Test Data/SchedulesDirect/headends_response.json");
            var headendsDtos = JsonSerializer.Deserialize<IReadOnlyList<HeadendsDto>>(bytes, _jsonOptions);

            Assert.NotNull(headendsDtos);
            Assert.Equal(8, headendsDtos!.Count);
            Assert.Equal("CA00053", headendsDtos[0].Headend);
            Assert.Equal("Cable", headendsDtos[0].Transport);
            Assert.Equal("Beverly Hills", headendsDtos[0].Location);
            Assert.Equal(2, headendsDtos[0].Lineups.Count);
            Assert.Equal("Time Warner Cable - Cable", headendsDtos[0].Lineups[0].Name);
            Assert.Equal("USA-CA00053-DEFAULT", headendsDtos[0].Lineups[0].Lineup);
            Assert.Equal("/20141201/lineups/USA-CA00053-DEFAULT", headendsDtos[0].Lineups[0].Uri);
        }

        /// <summary>
        /// /lineups response.
        /// </summary>
        [Fact]
        public void Deserialize_Lineups_Response_Success()
        {
            var bytes = File.ReadAllBytes("Test Data/SchedulesDirect/lineups_response.json");
            var lineupsDto = JsonSerializer.Deserialize<LineupsDto>(bytes, _jsonOptions);

            Assert.NotNull(lineupsDto);
            Assert.Equal(0, lineupsDto!.Code);
            Assert.Equal("20141201.web.1", lineupsDto.ServerId);
            Assert.Equal(new DateTime(2015, 04, 17, 14, 22, 17, DateTimeKind.Utc), lineupsDto.LineupTimestamp);
            Assert.Equal(5, lineupsDto.Lineups.Count);
            Assert.Equal("GBR-0001317-DEFAULT", lineupsDto.Lineups[0].Lineup);
            Assert.Equal("Freeview - Carlton - LWT (Southeast)", lineupsDto.Lineups[0].Name);
            Assert.Equal("DVB-T", lineupsDto.Lineups[0].Transport);
            Assert.Equal("London", lineupsDto.Lineups[0].Location);
            Assert.Equal("/20141201/lineups/GBR-0001317-DEFAULT", lineupsDto.Lineups[0].Uri);

            Assert.Equal("DELETED LINEUP", lineupsDto.Lineups[4].Name);
            Assert.True(lineupsDto.Lineups[4].IsDeleted);
        }

        /// <summary>
        /// /lineup/:id response.
        /// </summary>
        [Fact]
        public void Deserialize_Lineup_Response_Success()
        {
            var bytes = File.ReadAllBytes("Test Data/SchedulesDirect/lineup_response.json");
            var channelDto = JsonSerializer.Deserialize<ChannelDto>(bytes, _jsonOptions);

            Assert.NotNull(channelDto);
            Assert.Equal(2, channelDto!.Map.Count);
            Assert.Equal("24326", channelDto.Map[0].StationId);
            Assert.Equal("001", channelDto.Map[0].Channel);
            Assert.Equal("BBC ONE South", channelDto.Map[0].ProviderCallsign);
            Assert.Equal("1", channelDto.Map[0].LogicalChannelNumber);
            Assert.Equal("providerCallsign", channelDto.Map[0].MatchType);
        }
    }
}
