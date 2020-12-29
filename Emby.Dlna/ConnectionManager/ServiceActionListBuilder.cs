#pragma warning disable CS1591

using System.Collections.Generic;
using Emby.Dlna.Common;

namespace Emby.Dlna.ConnectionManager
{
    /// <summary>
    /// Defines the <see cref="ServiceActionListBuilder" />.
    /// </summary>
    public static class ServiceActionListBuilder
    {
        /// <summary>
        /// Returns an enumerable of the ConnectionManagar:1 DLNA actions.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{ServiceAction}"/>.</returns>
        public static IEnumerable<ServiceAction> GetActions()
        {
            var list = new List<ServiceAction>
            {
                GetCurrentConnectionInfo(),
                GetProtocolInfo(),
                GetCurrentConnectionIDs(),
                ConnectionComplete(),
                PrepareForConnection()
            };

            return list;
        }

        /// <summary>
        /// Returns the action details for "PrepareForConnection".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction PrepareForConnection()
        {
            var action = new ServiceAction
            {
                Name = "PrepareForConnection"
            };

            action.ArgumentList.Add(new Argument
            {
                Name = "RemoteProtocolInfo",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_ProtocolInfo"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "PeerConnectionManager",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_ConnectionManager"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "PeerConnectionID",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_ConnectionID"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "Direction",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_Direction"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "ConnectionID",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_ConnectionID"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "AVTransportID",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_AVTransportID"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "RcsID",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_RcsID"
            });

            return action;
        }

        /// <summary>
        /// Returns the action details for "GetCurrentConnectionInfo".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetCurrentConnectionInfo()
        {
            var action = new ServiceAction
            {
                Name = "GetCurrentConnectionInfo"
            };

            action.ArgumentList.Add(new Argument
            {
                Name = "ConnectionID",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_ConnectionID"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "RcsID",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_RcsID"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "AVTransportID",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_AVTransportID"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "ProtocolInfo",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_ProtocolInfo"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "PeerConnectionManager",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_ConnectionManager"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "PeerConnectionID",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_ConnectionID"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "Direction",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_Direction"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "Status",
                Direction = "out",
                RelatedStateVariable = "A_ARG_TYPE_ConnectionStatus"
            });

            return action;
        }

        /// <summary>
        /// Returns the action details for "GetProtocolInfo".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetProtocolInfo()
        {
            var action = new ServiceAction
            {
                Name = "GetProtocolInfo"
            };

            action.ArgumentList.Add(new Argument
            {
                Name = "Source",
                Direction = "out",
                RelatedStateVariable = "SourceProtocolInfo"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "Sink",
                Direction = "out",
                RelatedStateVariable = "SinkProtocolInfo"
            });

            return action;
        }

        /// <summary>
        /// Returns the action details for "GetCurrentConnectionIDs".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetCurrentConnectionIDs()
        {
            var action = new ServiceAction
            {
                Name = "GetCurrentConnectionIDs"
            };

            action.ArgumentList.Add(new Argument
            {
                Name = "ConnectionIDs",
                Direction = "out",
                RelatedStateVariable = "CurrentConnectionIDs"
            });

            return action;
        }

        /// <summary>
        /// Returns the action details for "ConnectionComplete".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction ConnectionComplete()
        {
            var action = new ServiceAction
            {
                Name = "ConnectionComplete"
            };

            action.ArgumentList.Add(new Argument
            {
                Name = "ConnectionID",
                Direction = "in",
                RelatedStateVariable = "A_ARG_TYPE_ConnectionID"
            });

            return action;
        }
    }
}
