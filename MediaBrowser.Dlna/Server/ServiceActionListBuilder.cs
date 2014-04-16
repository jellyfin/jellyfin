using MediaBrowser.Dlna.Common;
using System.Collections.Generic;

namespace MediaBrowser.Dlna.Server
{
    public class ServiceActionListBuilder
    {
        public IEnumerable<ServiceAction> GetActions()
        {
            var list = new List<ServiceAction>
            {
                GetGetSystemUpdateIDAction(),
                GetSearchCapabilitiesAction(),
                GetSortCapabilitiesAction(),
                GetBrowseAction(),
                GetX_GetFeatureListAction(),
                GetXSetBookmarkAction()
            };

            return list;
        }

        private ServiceAction GetGetSystemUpdateIDAction()
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

        private ServiceAction GetSearchCapabilitiesAction()
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

        private ServiceAction GetSortCapabilitiesAction()
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

        private ServiceAction GetX_GetFeatureListAction()
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
