using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Reports
{
	/// <summary> A report stat builder. </summary>
	/// <seealso cref="T:MediaBrowser.Api.Reports.ReportBuilderBase"/>
	public class ReportStatBuilder : ReportBuilderBase
	{
		/// <summary>
		/// Initializes a new instance of the MediaBrowser.Api.Reports.ReportStatBuilder class. </summary>
		/// <param name="libraryManager"> Manager for library. </param>
		public ReportStatBuilder(ILibraryManager libraryManager)
			: base(libraryManager)
		{
		}

		/// <summary> Gets report stat result. </summary>
		/// <param name="items"> The items. </param>
		/// <param name="reportRowType"> Type of the report row. </param>
		/// <param name="topItem"> The top item. </param>
		/// <returns> The report stat result. </returns>
		public ReportStatResult GetReportStatResult(BaseItem[] items, ReportViewType reportRowType, int topItem = 5)
		{
			ReportStatResult result = new ReportStatResult();
			result = this.GetResultGenres(result, items, topItem);
			result = this.GetResultStudios(result, items, topItem);
			result = this.GetResultPersons(result, items, topItem);
			result = this.GetResultProductionYears(result, items, topItem);
			result = this.GetResulProductionLocations(result, items, topItem);
			result = this.GetResultCommunityRatings(result, items, topItem);
			result = this.GetResultParentalRatings(result, items, topItem);

			switch (reportRowType)
			{
				case ReportViewType.Season:
				case ReportViewType.Series:
				case ReportViewType.MusicAlbum:
				case ReportViewType.MusicArtist:
				case ReportViewType.Game:
					break;
				case ReportViewType.Movie:
				case ReportViewType.BoxSet:

					break;
				case ReportViewType.Book:
				case ReportViewType.Episode:
				case ReportViewType.Video:
				case ReportViewType.MusicVideo:
				case ReportViewType.Trailer:
				case ReportViewType.Audio:
				case ReportViewType.BaseItem:
				default:
					break;
			}

			result.Groups = result.Groups.OrderByDescending(n => n.Items.Count()).ToList();

			return result;
		}

		private ReportStatResult GetResultGenres(ReportStatResult result, BaseItem[] items, int topItem = 5)
		{
			this.GetGroups(result, ReportHelper.GetServerLocalizedString("HeaderGenres"), topItem,
							items.SelectMany(x => x.Genres)
								.GroupBy(x => x)
								.OrderByDescending(x => x.Count())
								.Take(topItem)
								.Select(x => new ReportStatItem
								{
									Name = x.Key,
									Value = x.Count().ToString(),
									Id = GetGenreID(x.Key)
								}));
			return result;

		}

		private ReportStatResult GetResultStudios(ReportStatResult result, BaseItem[] items, int topItem = 5)
		{
			this.GetGroups(result, ReportHelper.GetServerLocalizedString("HeaderStudios"), topItem,
									items.SelectMany(x => x.Studios)
										.GroupBy(x => x)
										.OrderByDescending(x => x.Count())
										.Take(topItem)
										.Select(x => new ReportStatItem
										{
											Name = x.Key,
											Value = x.Count().ToString(),
											Id = GetStudioID(x.Key)
										})
					);

			return result;

		}

		private ReportStatResult GetResultPersons(ReportStatResult result, BaseItem[] items, int topItem = 5)
		{
			List<string> t = new List<string> { PersonType.Actor, PersonType.Composer, PersonType.Director, PersonType.GuestStar, PersonType.Producer, PersonType.Writer, "Artist", "AlbumArtist" };
			foreach (var item in t)
			{
				this.GetGroups(result, ReportHelper.GetServerLocalizedString("Option" + item), topItem,
						items.SelectMany(x => x.People)
								.Where(n => n.Type == item)
								.GroupBy(x => x.Name)
								.OrderByDescending(x => x.Count())
								.Take(topItem)
								.Select(x => new ReportStatItem
								{
									Name = x.Key,
									Value = x.Count().ToString(),
									Id = GetPersonID(x.Key)
								})
				);
			}

			return result;
		}

		private ReportStatResult GetResultCommunityRatings(ReportStatResult result, BaseItem[] items, int topItem = 5)
		{
			this.GetGroups(result, ReportHelper.GetServerLocalizedString("LabelCommunityRating"), topItem,
					   items.Where(x => x.CommunityRating != null && x.CommunityRating > 0)
						   .GroupBy(x => x.CommunityRating)
						   .OrderByDescending(x => x.Count())
						   .Take(topItem)
						   .Select(x => new ReportStatItem
						   {
							   Name = x.Key.ToString(),
							   Value = x.Count().ToString()
						   })
			   );

			return result;
		}

		private ReportStatResult GetResultParentalRatings(ReportStatResult result, BaseItem[] items, int topItem = 5)
		{
			this.GetGroups(result, ReportHelper.GetServerLocalizedString("HeaderParentalRatings"), topItem,
					   items.Where(x => x.OfficialRating != null)
						   .GroupBy(x => x.OfficialRating)
						   .OrderByDescending(x => x.Count())
						   .Take(topItem)
						   .Select(x => new ReportStatItem
						   {
							   Name = x.Key.ToString(),
							   Value = x.Count().ToString()
						   })
			   );

			return result;
		}


		private ReportStatResult GetResultProductionYears(ReportStatResult result, BaseItem[] items, int topItem = 5)
		{
			this.GetGroups(result, ReportHelper.GetServerLocalizedString("HeaderYears"), topItem,
					items.Where(x => x.ProductionYear != null && x.ProductionYear > 0)
						.GroupBy(x => x.ProductionYear)
						.OrderByDescending(x => x.Count())
						.Take(topItem)
						.Select(x => new ReportStatItem
						{
							Name = x.Key.ToString(),
							Value = x.Count().ToString()
						})
			);

			return result;
		}

		private ReportStatResult GetResulProductionLocations(ReportStatResult result, BaseItem[] items, int topItem = 5)
		{
			this.GetGroups(result, ReportHelper.GetServerLocalizedString("HeaderCountries"), topItem,
						items.OfType<IHasProductionLocations>()
						.Where(x => x.ProductionLocations != null)
						.SelectMany(x => x.ProductionLocations)
						.GroupBy(x => x)
						.OrderByDescending(x => x.Count())
						.Take(topItem)
						.Select(x => new ReportStatItem
						{
							Name = x.Key.ToString(),
							Value = x.Count().ToString()
						})
			);

			return result;
		}


		/// <summary> Gets the groups. </summary>
		/// <param name="result"> The result. </param>
		/// <param name="header"> The header. </param>
		/// <param name="topItem"> The top item. </param>
		/// <param name="top"> The top. </param>
		private void GetGroups(ReportStatResult result, string header, int topItem, IEnumerable<ReportStatItem> top)
		{
			if (top.Count() > 0)
			{
				var group = new ReportStatGroup { Header = ReportStatGroup.FormatedHeader(header, topItem) };
				group.Items.AddRange(top);
				result.Groups.Add(group);
			}
		}
	}
}
