using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Jellyfin.Data.Enums;
using Xunit;
using Xunit.Sdk;

namespace Jellyfin.Server.Implementations.Tests.BaseItem
{
    public class BaseItemKindTests
    {
        [Theory]
        [ClassData(typeof(GetBaseItemDescendants))]
        public void BaseItemKindEnumTest(Type baseItemType)
        {
            var enumValue = Enum.Parse<BaseItemKind>(baseItemType.Name);
            Assert.True(Enum.IsDefined(typeof(BaseItemKind), enumValue));
        }

        [Theory]
        [ClassData(typeof(GetBaseItemDescendants))]
        public void GetBaseKindEnumTest(Type baseItemDescendantType)
        {
            var defaultConstructor = baseItemDescendantType.GetConstructor(Type.EmptyTypes);
            var instance = (MediaBrowser.Controller.Entities.BaseItem)defaultConstructor!.Invoke(null);
            var exception = Record.Exception(() => instance.GetBaseItemKind());
            Assert.Null(exception);
        }

        private class GetBaseItemDescendants : IEnumerable<object?[]>
        {
            private static bool IsProjectAssemblyName(string? name)
            {
                if (name == null)
                {
                    return false;
                }

                return name.StartsWith("Jellyfin", StringComparison.OrdinalIgnoreCase)
                       || name.StartsWith("Emby", StringComparison.OrdinalIgnoreCase)
                       || name.StartsWith("MediaBrowser", StringComparison.OrdinalIgnoreCase);
            }

            public IEnumerator<object?[]> GetEnumerator()
            {
                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in loadedAssemblies)
                {
                    if (IsProjectAssemblyName(assembly.FullName))
                    {
                        var baseItemTypes = assembly.GetTypes()
                            .Where(targetType => targetType.IsClass
                                                 && !targetType.IsAbstract
                                                 && targetType.IsSubclassOf(typeof(MediaBrowser.Controller.Entities.BaseItem)));
                        foreach (var baseItemType in baseItemTypes)
                        {
                            yield return new object?[] { baseItemType };
                        }
                    }
                }

                var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (path == null)
                {
                    throw new NullException("Assembly location is null");
                }

                foreach (string dll in Directory.GetFiles(path, "*.dll"))
                {
                    var assembly = Assembly.LoadFile(dll);
                    if (IsProjectAssemblyName(assembly.FullName))
                    {
                        var baseItemTypes = assembly.GetTypes()
                            .Where(targetType => targetType.IsClass
                                                 && !targetType.IsAbstract
                                                 && targetType.IsSubclassOf(typeof(MediaBrowser.Controller.Entities.BaseItem)));
                        foreach (var baseItemType in baseItemTypes)
                        {
                            yield return new object?[] { baseItemType };
                        }
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
