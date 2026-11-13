using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Emby.Server.Implementations.Serialization;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Serialization
{
    public class MyXmlSerializerTests
    {
        private readonly MyXmlSerializer _serializer = new();

        [Fact]
        public void SerializeToFile_OverwritesDestinationOnSuccess()
        {
            using var dir = new TempDir();
            var path = Path.Combine(dir.Path, "ok.xml");
            File.WriteAllText(path, "stale contents");

            _serializer.SerializeToFile(new SerializableObject { Value = 42 }, path);

            var roundTripped = (SerializableObject?)_serializer.DeserializeFromFile(typeof(SerializableObject), path);
            Assert.NotNull(roundTripped);
            Assert.Equal(42, roundTripped.Value);
        }

        [Fact]
        public void SerializeToFile_PreservesOriginalFileWhenSerializationThrows()
        {
            using var dir = new TempDir();
            var path = Path.Combine(dir.Path, "config.xml");
            File.WriteAllText(path, "<previous intact configuration/>");

            Assert.ThrowsAny<InvalidOperationException>(() =>
                _serializer.SerializeToFile(new ThrowingObject(), path));

            Assert.Equal("<previous intact configuration/>", File.ReadAllText(path));
            Assert.Empty(Directory.EnumerateFiles(dir.Path, "*.tmp"));
        }

        [Fact]
        public void SerializeToFile_LeavesNoTempFileWhenSerializationThrows()
        {
            using var dir = new TempDir();
            var path = Path.Combine(dir.Path, "config2.xml");

            Assert.ThrowsAny<InvalidOperationException>(() =>
                _serializer.SerializeToFile(new ThrowingObject(), path));

            // The destination was never written and no temp debris survived.
            Assert.False(File.Exists(path));
            Assert.Empty(Directory.EnumerateFiles(dir.Path, "*.tmp"));
        }

        [Fact]
        public void SerializeToStream_SameNamedTypesFromDifferentAssemblies_BothSerialize()
        {
            // Mirrors two versions of one plugin loaded side by side: same type full
            // name, distinct Type instances. Keying the serializer cache by type name
            // makes the second type reuse the first type's serializer and fail with
            // InvalidCastException; keying by the Type instance keeps both working.
            var typeA = EmitSameNamedType();
            var typeB = EmitSameNamedType();
            Assert.Equal(typeA.FullName, typeB.FullName);
            Assert.NotSame(typeA, typeB);

            using var streamA = new MemoryStream();
            _serializer.SerializeToStream(Activator.CreateInstance(typeA)!, streamA);
            using var streamB = new MemoryStream();
            _serializer.SerializeToStream(Activator.CreateInstance(typeB)!, streamB);
        }

        private static Type EmitSameNamedType()
        {
            var assemblyName = new AssemblyName("Jellyfin.Emit." + Guid.NewGuid().ToString("N"));
            var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var module = assembly.DefineDynamicModule(assemblyName.Name!);
            var type = module.DefineType("Jellyfin.Emitted.SameNamedConfig", TypeAttributes.Public | TypeAttributes.Sealed);
            return type.CreateType()!;
        }

        public sealed class SerializableObject
        {
            public int Value { get; set; }
        }

        public sealed class ThrowingObject
        {
            // XmlSerializer writes the declaration, then reads read-write properties;
            // the getter throwing mid-serialize is the failure mode these tests pin down.
            public string Bomb
            {
                get => throw new InvalidOperationException("boom");
                set { }
            }
        }

        private sealed class TempDir : IDisposable
        {
            public TempDir()
            {
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "jellyfin-myxmlserializer-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path);
            }

            public string Path { get; }

            public void Dispose() => Directory.Delete(Path, recursive: true);
        }
    }
}
