using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Model.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.Server.Filters
{
    internal class ManagementInterfaceFilter : IActionFilter
    {
        private readonly List<(IPAddress Host, int Port)> managementEndpoints;

        public ManagementInterfaceFilter(IConfiguration appConfig)
        {
            managementEndpoints = new List<(IPAddress Host, int Port)>();

            if (appConfig.UseManagementInterface())
            {
                var socketPath = appConfig.GetManagementInterfaceSocketPath();
                var localhostPort = appConfig.GetManagementInterfaceLocalhostPort();
                bool useDefault = true;
                if (!string.IsNullOrEmpty(socketPath))
                {
                    // TODO make this work, no idea where to get the SocketAddress or something similar
                    managementEndpoints.Add((IPAddress.Any, 0));
                }

                if (localhostPort > 0)
                {
                    managementEndpoints.Add((IPAddress.Loopback, localhostPort));
                    managementEndpoints.Add((IPAddress.IPv6Loopback, localhostPort));
                }

                if (useDefault)
                {
                    managementEndpoints.Add((IPAddress.Loopback, ServerConfiguration.DefaultManagementPort));
                    managementEndpoints.Add((IPAddress.IPv6Loopback, ServerConfiguration.DefaultManagementPort));
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var isManagementRoute = IsManagementRoute(context);
            var isManagementListenEntrypoint = IsManagementListenEntrypoint(context);

            if ((isManagementRoute && !isManagementListenEntrypoint) || (!isManagementRoute && isManagementListenEntrypoint))
            {
                context.Result = new NotFoundResult();
            }
        }

        private bool IsManagementRoute(ActionExecutingContext context)
        {
            return HasAttribute<ManagementAttribute>(context);
        }

        private bool HasAttribute<T>(ActionExecutingContext context)
        {
            var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            if (controllerActionDescriptor != null)
            {
                // Check if the attribute exists on the action method
                if (controllerActionDescriptor.MethodInfo?.GetCustomAttributes(inherit: true)?.Any(a => a.GetType().Equals(typeof(T))) ?? false)
                {
                    return true;
                }

                // Check if the attribute exists on the controller
                if (controllerActionDescriptor.ControllerTypeInfo?.GetCustomAttributes(typeof(T), true)?.Any() ?? false)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsManagementListenEntrypoint(ActionExecutingContext context)
        {
            return managementEndpoints.Contains((context.HttpContext.Connection.LocalIpAddress, context.HttpContext.Connection.LocalPort));
        }
    }
}
