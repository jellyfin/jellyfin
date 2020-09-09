using System.Collections.Generic;
using Emby.Dlna.Common;

namespace Emby.Dlna.MediaReceiverRegistrar
{
    /// <summary>
    /// Defines the <see cref="ServiceActionListBuilder" />.
    /// </summary>
    public static class ServiceActionListBuilder
    {
        /// <summary>
        /// Returns a list of services that this instance provides.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{ServiceAction}"/>.</returns>
        public static IEnumerable<ServiceAction> GetActions()
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

        /// <summary>
        /// Returns the IsValidated property.
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
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

        /// <summary>
        /// Returns the IsAuthorized property.
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
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

        /// <summary>
        /// Returns the RegisterDevice service.
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
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

        /// <summary>
        /// Returns the GetValidationSucceededUpdateID service.
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
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

        /// <summary>
        /// Returns the GetGetAuthorizationDeniedUpdateID service.
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetGetAuthorizationDeniedUpdateID()
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

        /// <summary>
        /// Returns the GetValidationRevokedUpdateID service.
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetGetValidationRevokedUpdateID()
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

        /// <summary>
        /// Returns the GetAuthorizationGrantedUpdateID service.
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetGetAuthorizationGrantedUpdateID()
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
