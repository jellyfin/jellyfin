using MediaBrowser.Common.Localization;
using System.ComponentModel.Composition;

namespace MediaBrowser.Controller.Localization
{
    [Export(typeof(LocalizedStringData))]
    public class BaseStrings : LocalizedStringData
    {
        public BaseStrings()
        {
            ThisVersion = "1.0002";
            Prefix = LocalizedStrings.BasePrefix;
        }



        //Config Panel
        public string ConfigConfig = "Configuration";
        public string VersionConfig = "Version";
        public string MediaOptionsConfig = "Media Options";
        public string ThemesConfig = "Theme Options";
        public string ParentalControlConfig = "Parental Control";
        public string ContinueConfig = "Continue";
        public string ResetDefaultsConfig = "Reset Defaults";
        public string ClearCacheConfig = "Clear Cache";
        public string UnlockConfig = "Unlock";
        public string GeneralConfig = "General";
        public string EnableScreenSaverConfig = "Screen Saver";
        public string SSTimeOutConfig = "Timeout (mins)";
        public string TrackingConfig = "Tracking";
        public string AssumeWatchedIfOlderThanConfig = "Assume Played If Older Than";
        public string MetadataConfig = "Metadata";
        public string EnableInternetProvidersConfig = "Allow Internet Providers";
        public string UpdatesConfig = "Updates";
        public string AutomaticUpdatesConfig = "Check For Updates";
        public string LoggingConfig = "Logging";
        public string BetaUpdatesConfig = "Beta Updates";
        public string GlobalConfig = "Global";
        public string EnableEHSConfig = "Enable EHS";
        public string ShowClockConfig = "Show Clock";
        public string DimUnselectedPostersConfig = "Dim Unselected Posters";
        public string HideFocusFrameConfig = "Hide Focus Frame";
        public string AlwaysShowDetailsConfig = "Always Show Details";
        public string ExcludeRemoteContentInSearchesConfig = "Exclude Remote Content In Searches";
        public string EnhancedMouseSupportConfig = "Enhanced Mouse Support";
        public string ViewsConfig = "Views";
        public string PosterGridSpacingConfig = "Poster Grid Spacing";
        public string ThumbWidthSplitConfig = "Thumb Width Split";
        public string BreadcrumbCountConfig = "Breadcrumb Count";
        public string ShowFanArtonViewsConfig = "Show Fan Art on Views";
        public string ShowInitialFolderBackgroundConfig = "Show Initial Folder Background";
        public string ShowThemeBackgroundConfig = "Show Theme Background";
        public string ShowHDOverlayonPostersConfig = "Show HD Overlay on Posters";
        public string ShowIcononRemoteContentConfig = "Show Icon on Remote Content";
        public string EnableAdvancedCmdsConfig = "Enable Advanced Commands";
        public string MediaTrackingConfig = "Media Tracking";
        public string RememberFolderIndexingConfig = "Remember Folder Indexing";
        public string ShowUnwatchedCountConfig = "Show Unplayed Count";
        public string WatchedIndicatoronFoldersConfig = "Played Indicator on Folders";
        public string HighlightUnwatchedItemsConfig = "Highlight Unplayed Items";
        public string WatchedIndicatoronVideosConfig = "Played Indicator on Items";
        public string WatchedIndicatorinDetailViewConfig = "Played Indicator in Detail View";
        public string DefaultToFirstUnwatchedItemConfig = "Default To First Unplayed Item";
        public string GeneralBehaviorConfig = "General Behavior";
        public string AllowNestedMovieFoldersConfig = "Allow Nested Movie Folders";
        public string AutoEnterSingleFolderItemsConfig = "Auto Enter Single Folder Items";
        public string MultipleFileBehaviorConfig = "Multiple File Behavior";
        public string TreatMultipleFilesAsSingleMovieConfig = "Treat Multiple Files As Single Movie";
        public string MultipleFileSizeLimitConfig = "Multiple File Size Limit";
        public string MBThemeConfig = "Media Browser Theme";
        public string VisualThemeConfig = "Visual Theme";
        public string ColorSchemeConfig = "Color Scheme *";
        public string FontSizeConfig = "Font Size *";
        public string RequiresRestartConfig = "* Requires a restart to take effect.";
        public string ThemeSettingsConfig = "Theme Specific Settings";
        public string ShowConfigButtonConfig = "Show Config Button";
        public string AlphaBlendingConfig = "Alpha Blending";
        public string SecurityPINConfig = "Security PIN";
        public string PCUnlockedTxtConfig = "Parental Controls are Temporarily Unlocked.  You cannot change values unless you re-lock.";
        public string RelockBtnConfig = "Re-Lock";
        public string EnableParentalBlocksConfig = "Enable Parental Blocks";
        public string MaxAllowedRatingConfig = "Max Allowed Rating ";
        public string BlockUnratedContentConfig = "Block Unrated Content";
        public string HideBlockedContentConfig = "Hide Blocked Content";
        public string UnlockonPINEntryConfig = "Unlock on PIN Entry";
        public string UnlockPeriodHoursConfig = "Unlock Period (Hours)";
        public string EnterNewPINConfig = "Enter New PIN";
        public string RandomizeBackdropConfig = "Randomize";
        public string RotateBackdropConfig = "Rotate";
        public string UpdateLibraryConfig = "Update Library";
        public string BackdropSettingsConfig = "Backdrop Settings";
        public string BackdropRotationIntervalConfig = "Rotation Time";
        public string BackdropTransitionIntervalConfig = "Transition Time";
        public string BackdropLoadDelayConfig = "Load Delay";
        public string AutoScrollTextConfig = "Auto Scroll Overview";
        public string SortYearsAscConfig = "Sort by Year in Ascending Order";
        public string AutoValidateConfig = "Automatically Validate Items";
        public string SaveLocalMetaConfig = "Save Locally";
        public string HideEmptyFoldersConfig = "Hide Empty TV Folders";


        //EHS        
        public string RecentlyWatchedEHS = "last played";
        public string RecentlyAddedEHS = "last added";
        public string RecentlyAddedUnwatchedEHS = "last added unplayed";
        public string WatchedEHS = "Played";
        public string AddedEHS = "Added";
        public string UnwatchedEHS = "Unplayed";
        public string AddedOnEHS = "Added on";
        public string OnEHS = "on";
        public string OfEHS = "of";
        public string NoItemsEHS = "No Items To Show";
        public string VariousEHS = "(various)";

        //Context menu
        public string CloseCMenu = "Close";
        public string PlayMenuCMenu = "Play Menu";
        public string ItemMenuCMenu = "Item Menu";
        public string PlayAllCMenu = "Play All";
        public string PlayAllFromHereCMenu = "Play All From Here";
        public string ResumeCMenu = "Resume";
        public string MarkUnwatchedCMenu = "Mark Unplayed";
        public string MarkWatchedCMenu = "Mark Played";
        public string ShufflePlayCMenu = "Shuffle Play";

        //Media Detail Page
        public string GeneralDetail = "General";
        public string ActorsDetail = "Actors";
        public string ArtistsDetail = "Artists";
        public string PlayDetail = "Play";
        public string ResumeDetail = "Resume";
        public string RefreshDetail = "Refresh";
        public string PlayTrailersDetail = "Trailer";
        public string CacheDetail = "Cache 2 xml";
        public string DeleteDetail = "Delete";
        public string TMDBRatingDetail = "TMDb Rating";
        public string OutOfDetail = "out of";
        public string DirectorDetail = "Director";
        public string ComposerDetail = "Composer";
        public string HostDetail = "Host";
        public string RuntimeDetail = "Runtime";
        public string NextItemDetail = "Next";
        public string PreviousItemDetail = "Previous";
        public string FirstAiredDetail = "First aired";
        public string LastPlayedDetail = "Last played";
        public string TrackNumberDetail = "Track";

        public string DirectedByDetail = "Directed By: ";
        public string WrittenByDetail = "Written By: ";
        public string ComposedByDetail = "Composed By: ";

        //Display Prefs
        public string ViewDispPref = "View";
        public string ViewSearch = "Search";
        public string CoverFlowDispPref = "Cover Flow";
        public string DetailDispPref = "Detail";
        public string PosterDispPref = "Poster";
        public string ThumbDispPref = "Thumb";
        public string ThumbStripDispPref = "Thumb Strip";
        public string ShowLabelsDispPref = "Show Labels";
        public string VerticalScrollDispPref = "Vertical Scroll";
        public string UseBannersDispPref = "Use Banners";
        public string UseCoverflowDispPref = "Use Coverflow Style";
        public string ThumbSizeDispPref = "Thumb Size";
        public string NameDispPref = "Name";
        public string DateDispPref = "Date";
        public string RatingDispPref = "User Rating";
        public string OfficialRatingDispPref = "Rating";
        public string RuntimeDispPref = "Runtime";
        public string UnWatchedDispPref = "Unplayed";
        public string YearDispPref = "Year";
        public string NoneDispPref = "None";
        public string PerformerDispPref = "Performer";
        public string ActorDispPref = "Actor";
        public string GenreDispPref = "Genre";
        public string DirectorDispPref = "Director";
        public string StudioDispPref = "Studio";

        //Dialog boxes
        //public string BrokenEnvironmentDial = "Application will now close due to broken MediaCenterEnvironment object, possibly due to 5 minutes of idle time and/or running with TVPack installed.";
        //public string InitialConfigDial = "Initial configuration is complete, please restart Media Browser";
        //public string DeleteMediaDial = "Are you sure you wish to delete this media item?";
        //public string DeleteMediaCapDial = "Delete Confirmation";
        //public string NotDeletedDial = "Item NOT Deleted.";
        //public string NotDeletedCapDial = "Delete Cancelled by User";
        //public string NotDelInvalidPathDial = "The selected media item cannot be deleted due to an invalid path. Or you may not have sufficient access rights to perform this command.";
        //public string DelFailedDial = "Delete Failed";
        //public string NotDelUnknownDial = "The selected media item cannot be deleted due to an unknown error.";
        //public string NotDelTypeDial = "The selected media item cannot be deleted due to its Item-Type or you have not enabled this feature in the configuration file.";
        //public string FirstTimeDial = "As this is the first time you have run Media Browser please setup the inital configuration";
        //public string FirstTimeCapDial = "Configure";
        //public string EntryPointErrorDial = "Media Browser could not launch directly into ";
        //public string EntryPointErrorCapDial = "Entrypoint Error";
        //public string CriticalErrorDial = "Media Browser encountered a critical error and had to shut down: ";
        //public string CriticalErrorCapDial = "Critical Error";
        //public string ClearCacheErrorDial = "An error occured during the clearing of the cache, you may wish to manually clear it from {0} before restarting Media Browser";
        //public string RestartMBDial = "Please restart Media Browser";
        //public string ClearCacheDial = "Are you sure you wish to clear the cache?\nThis will erase all cached and downloaded information and images.";
        //public string ClearCacheCapDial = "Clear Cache";
        //public string CacheClearedDial = "Cache Cleared";
        //public string ResetConfigDial = "Are you sure you wish to reset all configuration to defaults?";
        //public string ResetConfigCapDial = "Reset Configuration";
        //public string ConfigResetDial = "Configuration Reset";
        //public string UpdateMBDial = "Please visit www.mediabrowser.tv/download to install the new version.";
        //public string UpdateMBCapDial = "Update Available";
        //public string UpdateMBExtDial = "There is an update available for Media Browser.  Please update Media Browser next time you are at your MediaCenter PC.";
        //public string DLUpdateFailDial = "Media Browser will operate normally and prompt you again the next time you load it.";
        //public string DLUpdateFailCapDial = "Update Download Failed";
        //public string UpdateSuccessDial = "Media Browser must now exit to apply the update.  It will restart automatically when it is done";
        //public string UpdateSuccessCapDial = "Update Downloaded";
        //public string CustomErrorDial = "Customisation Error";
        //public string ConfigErrorDial = "Reset to default?";
        //public string ConfigErrorCapDial = "Error in configuration file";
        //public string ContentErrorDial = "There was a problem playing the content. Check location exists";
        //public string ContentErrorCapDial = "Content Error";
        //public string CannotMaximizeDial = "We can not maximize the window! This is a known bug with Windows 7 and TV Pack, you will have to restart Media Browser!";
        //public string IncorrectPINDial = "Incorrect PIN Entered";
        //public string ContentProtected = "Content Protected";
        //public string CantChangePINDial = "Cannot Change PIN";
        //public string LibraryUnlockedDial = "Library Temporarily Unlocked.  Will Re-Lock in {0} Hour(s) or on Application Re-Start";
        //public string LibraryUnlockedCapDial = "Unlock";
        //public string PINChangedDial = "PIN Successfully Changed";
        //public string PINChangedCapDial = "PIN Change";
        //public string EnterPINToViewDial = "Please Enter PIN to View Protected Content";
        //public string EnterPINToPlayDial = "Please Enter PIN to Play Protected Content";
        //public string EnterCurrentPINDial = "Please Enter CURRENT PIN.";
        //public string EnterNewPINDial = "Please Enter NEW PIN (exactly 4 digits).";
        //public string EnterPINDial = "Please Enter PIN to Unlock Library";
        //public string NoContentDial = "No Content that can be played in this context.";
        //public string FontsMissingDial = "CustomFonts.mcml as been patched with missing values";
        //public string StyleMissingDial = "{0} has been patched with missing values";
        //public string ManualRefreshDial = "Library Update Started.  Will proceed in the background.";
        //public string ForcedRebuildDial = "Your library is currently being migrated by the service.  The service will re-start when it is finished and you may then run Media Browser.";
        //public string ForcedRebuildCapDial = "Library Migration";
        //public string RefreshFailedDial = "The last service refresh process failed.  Please run a manual refresh from the service.";
        //public string RefreshFailedCapDial = "Service Refresh Failed";
        //public string RebuildNecDial = "This version of Media Browser requires a re-build of your library.  It has started automatically in the service.  Some information may be incomplete until this process finishes.";
        //public string MigrateNecDial = "This version of Media Browser requires a migration of your library.  It has started automatically in the service.  The service will restart when it is complete and you may then run Media Browser.";
        //public string RebuildFailedDial = "There was an error attempting to tell the service to re-build your library.  Please run the service and do a manual refresh with the cache clear options selected.";
        //public string MigrateFailedDial = "There was an error attempting to tell the service to re-build your library.  Please run the service and do a manual refresh with the cache clear options selected.";
        //public string RefreshFolderDial = "Refresh all contents too?";
        //public string RefreshFolderCapDial = "Refresh Folder";

        //Generic
        public string Restartstr = "Restart";
        public string Errorstr = "Error";
        public string Playstr = "Play";
        public string MinutesStr = "mins"; //Minutes abbreviation
        public string HoursStr = "hrs"; //Hours abbreviation
        public string EndsStr = "Ends";
        public string KBsStr = "Kbps";  //Kilobytes per second
        public string FrameRateStr = "fps";  //Frames per second
        public string AtStr = "at";  //x at y, e.g. 1920x1080 at 25 fps
        public string Rated = "Rated";
        public string Or = "Or ";
        public string Lower = "Lower";
        public string Higher = "Higher";
        public string Search = "Search";
        public string Cancel = "Cancel";
        public string TitleContains = "Title Contains ";
        public string Any = "Any";

        //Search
        public string IncludeNested = "Include Subfolders";
        public string UnwatchedOnly = "Include Only Unwatched";
        public string FilterByRated = "Filter by Rating";

        //Profiler
        public string WelcomeProf = "Welcome to Media Browser";
        public string ProfilerTimeProf = "{1} took {2} seconds.";
        public string RefreshProf = "Refresh";
        public string SetWatchedProf = "Set Played {0}";
        public string RefreshFolderProf = "Refresh Folder and all Contents of";
        public string ClearWatchedProf = "Clear Played {0}";
        public string FullRefreshProf = "Full Library Refresh";
        public string FullValidationProf = "Full Library Validation";
        public string FastRefreshProf = "Fast Metadata refresh";
        public string SlowRefresh = "Slow Metadata refresh";
        public string ImageRefresh = "Image refresh";
        public string PluginUpdateProf = "An update is available for plug-in {0}";
        public string NoPluginUpdateProf = "No Plugin Updates Currently Available.";
        public string LibraryUnLockedProf = "Library Temporarily UnLocked. Will Re-Lock in {0} Hour(s)";
        public string LibraryReLockedProf = "Library Re-Locked";

        //Messages
        public string FullRefreshMsg = "Updating Media Library...";
        public string FullRefreshFinishedMsg = "Library update complete";

    }
}
