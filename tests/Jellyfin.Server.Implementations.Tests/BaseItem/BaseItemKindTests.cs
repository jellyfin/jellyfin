using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.BaseItem
{
    public class BaseItemKindTests
    {
        [Theory]
        [ClassData(typeof(GetBaseItemDescendant))]
        public void BaseKindEnumTest(Type baseItemDescendantType)
        {
            var defaultConstructor = baseItemDescendantType.GetConstructor(Type.EmptyTypes);

            Assert.NotNull(defaultConstructor);
            if (defaultConstructor != null)
            {
                var instance = (MediaBrowser.Controller.Entities.BaseItem)defaultConstructor.Invoke(null);
                var exception = Record.Exception(() => instance.GetBaseItemKind());
                Assert.Null(exception);
            }
        }

        private static bool IsProjectAssemblyName(string? name)
        {
            if (name == null)
            {
                return false;
            }

            return name.Contains("Jellyfin", StringComparison.InvariantCulture)
                || name.Contains("Emby", StringComparison.InvariantCulture)
                || name.Contains("MediaBrowser", StringComparison.InvariantCulture)
                || name.Contains("RSSDP", StringComparison.InvariantCulture);
        }

        private class GetBaseItemDescendant : IEnumerable<object?[]>
        {
            public IEnumerator<object?[]> GetEnumerator()
            {
                var projectAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(x => IsProjectAssemblyName(x.FullName));

                foreach (var projectAssembly in projectAssemblies)
                {
                    var baseItemDescendantTypes = projectAssembly.GetTypes()
                         .Where(targetType => targetType.IsClass && !targetType.IsAbstract && targetType.IsSubclassOf(typeof(MediaBrowser.Controller.Entities.BaseItem)));

                    foreach (var descendantType in baseItemDescendantTypes)
                    {
                        yield return new object?[] { descendantType };
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
