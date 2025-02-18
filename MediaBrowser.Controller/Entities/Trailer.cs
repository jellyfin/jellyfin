#nullable disable

#pragma warning disable CA1819, CS1591

using System;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Entities;

/// <summary>
/// Class Trailer.
/// </summary>
public class Trailer : Video, IHasLookupInfo<TrailerInfo>
{
    public Trailer()
    {
        TrailerTypes = [];
    }

    public TrailerType[] TrailerTypes { get; set; }

    public override double GetDefaultPrimaryImageAspectRatio()
        => 2.0 / 3;

    public override UnratedItem GetBlockUnratedType()
    {
        return UnratedItem.Trailer;
    }

    public TrailerInfo GetLookupInfo()
    {
        var info = GetItemLookupInfo<TrailerInfo>();

        if (!IsInMixedFolder && IsFileProtocol)
        {
            info.Name = System.IO.Path.GetFileName(ContainingFolderPath);
        }

        return info;
    }

    public override bool BeforeMetadataRefresh(bool replaceAllMetadata)
    {
        var hasChanges = base.BeforeMetadataRefresh(replaceAllMetadata);

        if (!ProductionYear.HasValue)
        {
            var info = LibraryManager.ParseName(Name);

            var yearInName = info.Year;

            if (yearInName.HasValue)
            {
                ProductionYear = yearInName;
                hasChanges = true;
            }
            else
            {
                // Try to get the year from the folder name
                if (!IsInMixedFolder)
                {
                    info = LibraryManager.ParseName(System.IO.Path.GetFileName(ContainingFolderPath));

                    yearInName = info.Year;

                    if (yearInName.HasValue)
                    {
                        ProductionYear = yearInName;
                        hasChanges = true;
                    }
                }
            }
        }

        return hasChanges;
    }
}
