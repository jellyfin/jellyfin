#pragma warning disable CS1591

using System.Collections.Generic;
using Emby.Dlna.Common;

namespace Emby.Dlna.ContentDirectory
{
    public class ServiceActionListBuilder
    {
        public IEnumerable<ServiceAction> GetActions()
        {
            return new[]
            {
                GetSearchCapabilitiesAction(),
                GetSortCapabilitiesAction(),
                GetGetSystemUpdateIDAction(),
                GetBrowseAction(),
                GetSearchAction(),
                GetX_GetFeatureListAction(),
                GetXSetBookmarkAction(),
                GetBrowseByLetterAction()
            };
        }

        private static ServiceAction GetGetSystemUpdateIDAction()
        {
            var action = new ServiceAction
            {
                Name = "GetSystemUpdateID"
            };

            action.ArgumentList.Add(new Argument
            {
                Name = "Id",
                Direction = "out",
                RelatedStateVariable = "SystemUpdateID"
            });

            return action;
        }

        private static ServiceAction GetSearchCapabilitiesAction()
        {
            var action = new ServiceAction
            {
                Name = "GetSearchCapabilities"
            };

            action.ArgumentList.Add(new Argument
            {
                Name = "SearchCaps",
                Direction = "out",
                RelatedStateVariable = "SearchCapabilities"
            });

            return action;
        }

        private static ServiceAction GetSortCapabilitiesAction()
        {
            var action = new ServiceAction
            {
                Name = "GetSortCapabilities"
            };

            action.ArgumentList.Add(new Argument
            {
                Name = "SortCaps",
                Direction = "out",
                RelatedStateVariable = "SortCapabilities"
            });

            return action;
        }

        private static ServiceAction GetX_GetFeatureListAction()
        {
            var action = new ServiceAction
            {
                Name = "X_GetFeatureList"
            };

            action.ArgumentList.Add(new Argument
            {
                Name = "FeatureList",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_Featurelist"
            });

            return action;
        }

        private static ServiceAction GetSearchAction()
        {
            var action = new ServiceAction
            {
                Name = "Search"
            };

            action.ArgumentList.Add(new Argument
            {
                Name = "ContainerID",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_ObjectID"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "SearchCriteria",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_SearchCriteria"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "Filter",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_Filter"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "StartingIndex",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_Index"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "RequestedCount",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_Count"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "SortCriteria",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_SortCriteria"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "Result",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_Result"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "NumberReturned",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_Count"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "TotalMatches",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_Count"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "UpdateID",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_UpdateID"
            });

            return action;
        }

        private ServiceAction GetBrowseAction()
        {
            var action = new ServiceAction
            {
                Name = "Browse"
            };

            action.ArgumentList.Add(new Argument
            {
                Name = "ObjectID",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_ObjectID"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "BrowseFlag",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_BrowseFlag"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "Filter",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_Filter"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "StartingIndex",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_Index"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "RequestedCount",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_Count"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "SortCriteria",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_SortCriteria"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "Result",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_Result"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "NumberReturned",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_Count"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "TotalMatches",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_Count"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "UpdateID",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_UpdateID"
            });

            return action;
        }

        private ServiceAction GetBrowseByLetterAction()
        {
            var action = new ServiceAction
            {
                Name = "X_BrowseByLetter"
            };

            action.ArgumentList.Add(new Argument
            {
                Name = "ObjectID",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_ObjectID"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "BrowseFlag",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_BrowseFlag"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "Filter",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_Filter"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "StartingLetter",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_BrowseLetter"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "RequestedCount",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_Count"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "SortCriteria",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_SortCriteria"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "Result",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_Result"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "NumberReturned",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_Count"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "TotalMatches",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_Count"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "UpdateID",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_UpdateID"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "StartingIndex",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_Index"
            });

            return action;
        }

        private ServiceAction GetXSetBookmarkAction()
        {
            var action = new ServiceAction
            {
                Name = "X_SetBookmark"
            };

            action.ArgumentList.Add(new Argument
            {
                Name = "CategoryType",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_CategoryType"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "RID",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_RID"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "ObjectID",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_ObjectID"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "PosSecond",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_PosSec"
            });

            return action;
        }
    }
}
