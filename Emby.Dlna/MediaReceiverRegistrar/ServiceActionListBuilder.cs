#pragma warning disable CS1591

using System.Collections.Generic;
using Emby.Dlna.Common;

namespace Emby.Dlna.MediaReceiverRegistrar
{
    public class ServiceActionListBuilder
    {
        public IEnumerable<ServiceAction> GetActions()
        {
            return new[]
            {
                GetIsValidated(),
                GetIsAuthorized(),
                GetRegisterDevice(),
                GetGetAuthorizationDeniedUpdateID(),
                GetGetAuthorizationGrantedUpdateID(),
                GetGetValidationRevokedUpdateID(),
                GetGetValidationSucceededUpdateID()
            };
        }

        private static ServiceAction GetIsValidated()
        {
            var action = new ServiceAction
            {
                Name = "IsValidated"
            };

            action.ArgumentList.Add(new Argument
            {
                Name = "DeviceID",
                Direction = "in"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "Result",
                Direction = "out"
            });

            return action;
        }

        private static ServiceAction GetIsAuthorized()
        {
            var action = new ServiceAction
            {
                Name = "IsAuthorized"
            };

            action.ArgumentList.Add(new Argument
            {
                Name = "DeviceID",
                Direction = "in"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "Result",
                Direction = "out"
            });

            return action;
        }

        private static ServiceAction GetRegisterDevice()
        {
            var action = new ServiceAction
            {
                Name = "RegisterDevice"
            };

            action.ArgumentList.Add(new Argument
            {
                Name = "RegistrationReqMsg",
                Direction = "in"
            });

            action.ArgumentList.Add(new Argument
            {
                Name = "RegistrationRespMsg",
                Direction = "out"
            });

            return action;
        }

        private static ServiceAction GetGetValidationSucceededUpdateID()
        {
            var action = new ServiceAction
            {
                Name = "GetValidationSucceededUpdateID"
            };

            action.ArgumentList.Add(new Argument
            {
                Name = "ValidationSucceededUpdateID",
                Direction = "out"
            });

            return action;
        }

        private ServiceAction GetGetAuthorizationDeniedUpdateID()
        {
            var action = new ServiceAction
            {
                Name = "GetAuthorizationDeniedUpdateID"
            };

            action.ArgumentList.Add(new Argument
            {
                Name = "AuthorizationDeniedUpdateID",
                Direction = "out"
            });

            return action;
        }

        private ServiceAction GetGetValidationRevokedUpdateID()
        {
            var action = new ServiceAction
            {
                Name = "GetValidationRevokedUpdateID"
            };

            action.ArgumentList.Add(new Argument
            {
                Name = "ValidationRevokedUpdateID",
                Direction = "out"
            });

            return action;
        }

        private ServiceAction GetGetAuthorizationGrantedUpdateID()
        {
            var action = new ServiceAction
            {
                Name = "GetAuthorizationGrantedUpdateID"
            };

            action.ArgumentList.Add(new Argument
            {
                Name = "AuthorizationGrantedUpdateID",
                Direction = "out"
            });

            return action;
        }
    }
}
