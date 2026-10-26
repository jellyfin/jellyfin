using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Plugins.Tmdb;
using TMDbLib.Objects.TvShows;
using Xunit;

namespace Jellyfin.Providers.Tests.Tmdb
{
    public class TmdbUtilsCastTests
    {
        private static readonly PluginConfiguration _config = new() { MaxCastMembers = 10 };

        [Fact]
        public void MapAggregateCast_MemberWithSeveralRoles_YieldsOneCreditPerRole()
        {
            var cast = new List<CastAggregate>
            {
                CreateAggregate("Megumi Toyoguchi", 1, 0, ("Tabby (voice)", 3), ("Mimiru (voice)", 12))
            };

            var people = TmdbUtils.MapAggregateCast(cast, _config, _ => null).ToArray();

            // The character they played the longest comes first, which is their own billing.
            Assert.Equal(["Mimiru (voice)", "Tabby (voice)"], people.Select(p => p.Role));
            Assert.All(people, p => Assert.Equal("Megumi Toyoguchi", p.Name));
            Assert.All(people, p => Assert.Equal(PersonKind.Actor, p.Type));
            Assert.All(people, p => Assert.Equal("1", p.GetProviderId(MetadataProvider.Tmdb)));
        }

        [Fact]
        public void MapAggregateCast_MemberWithoutARole_IsStillCredited()
        {
            var cast = new List<CastAggregate> { CreateAggregate("Uncredited Actor", 2, 0) };

            var person = Assert.Single(TmdbUtils.MapAggregateCast(cast, _config, _ => null));

            Assert.Equal(string.Empty, person.Role);
        }

        [Fact]
        public void MapAggregateCast_MoreThanConfigured_KeepsTheTopBilled()
        {
            var cast = Enumerable.Range(0, 5)
                .Select(i => CreateAggregate($"Actor {4 - i}", i + 1, 4 - i, ($"Role {4 - i}", 1)))
                .ToList();

            var people = TmdbUtils.MapAggregateCast(cast, new PluginConfiguration { MaxCastMembers = 2 }, _ => null);

            Assert.Equal(["Actor 0", "Actor 1"], people.Select(p => p.Name));
        }

        [Fact]
        public void MapAggregateCast_HideMissingCastMembers_DropsTheOnesWithoutAProfile()
        {
            var withProfile = CreateAggregate("Has Profile", 1, 0, ("Hero", 1));
            withProfile.ProfilePath = "/profile.jpg";
            var cast = new List<CastAggregate> { withProfile, CreateAggregate("No Profile", 2, 1, ("Villain", 1)) };

            var people = TmdbUtils.MapAggregateCast(
                cast,
                new PluginConfiguration { MaxCastMembers = 10, HideMissingCastMembers = true },
                _ => null);

            Assert.Equal(["Has Profile"], people.Select(p => p.Name));
        }

        [Fact]
        public void MapCast_FlatCredits_YieldOneCreditEach()
        {
            var cast = new List<Cast>
            {
                new() { Name = "Kevin Conroy", Id = 1, Order = 0, Character = " Batman (voice) " },
                new() { Name = "  ", Id = 2, Order = 1, Character = "Nobody" }
            };

            var person = Assert.Single(TmdbUtils.MapCast(cast, _config, _ => null));

            Assert.Equal("Kevin Conroy", person.Name);
            Assert.Equal("Batman (voice)", person.Role);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MapCast_NoCast_YieldsNothing(bool aggregate)
        {
            Assert.Empty(aggregate
                ? TmdbUtils.MapAggregateCast(null, _config, _ => null)
                : TmdbUtils.MapCast(null, _config, _ => null));
        }

        private static CastAggregate CreateAggregate(string name, int id, int order, params (string Character, int Episodes)[] roles)
        {
            return new CastAggregate
            {
                Name = name,
                Id = id,
                Order = order,
                Roles = roles.Select(role => new CastRole { Character = role.Character, EpisodeCount = role.Episodes }).ToList()
            };
        }
    }
}
