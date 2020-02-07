#pragma warning disable CS1591
#pragma warning disable SA1600

using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Dto
{
    public class RecommendationDto
    {
        public IReadOnlyCollection<BaseItemDto> Items { get; set; }

        public RecommendationType RecommendationType { get; set; }

        public string BaselineItemName { get; set; }

        public Guid CategoryId { get; set; }
    }
}
