using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;
using Jellyfin.Server.Implementations.Tests.Item;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

/// <summary>
/// A string in a key or an index that has a maximum length is rejected on save when it is inserted or changed to a
/// longer value, on every provider. The column of an indexed string has its length only on MySQL and MariaDB, so the
/// tests that need a row that is already stored with a longer value write it around the save hook and are skipped
/// there. Such a row stays readable and can be saved again as long as the value is not changed, also when the entity
/// is attached or updated as a whole and so has all of its properties marked as modified.
/// </summary>
public sealed class EnforceKeyLengthsTests : JellyfinDbTestFixture
{
    public static TheoryData<string, string, int> KeyClassColumns
    {
        get
        {
            using var context = OfflineContexts.Sqlite();
            var data = new TheoryData<string, string, int>();
            foreach (var entityType in context.GetService<IDesignTimeModel>().Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (IsKeyClass(property) && property.GetMaxLength() is { } maxLength)
                    {
                        data.Add(entityType.Name, property.Name, maxLength);
                    }
                }
            }

            return data;
        }
    }

    [Theory]
    [InlineData("UserData", "CustomDataKey", 256)]
    [InlineData("PeopleBaseItemMap", "Role", 400)]
    [InlineData("BaseItemProvider", "ProviderId", 128)]
    [InlineData("ItemValue", "Value", 512)]
    [InlineData("CustomItemDisplayPreferences", "Key", 256)]
    [InlineData("ApiKey", "AccessToken", 128)]
    [InlineData("Device", "AccessToken", 128)]
    [InlineData("DeviceOptions", "DeviceId", 256)]
    [InlineData("BaseItemEntity", "Type", 256)]
    [InlineData("BaseItemEntity", "MediaType", 64)]
    public void KeyClassColumn_HasItsBound(string entityName, string propertyName, int maxLength)
    {
        using var context = OfflineContexts.Sqlite();
        var property = context.GetService<IDesignTimeModel>().Model.GetEntityTypes()
            .Single(e => e.ClrType.Name == entityName)
            .FindProperty(propertyName)!;

        Assert.True(IsKeyClass(property));
        Assert.Equal(maxLength, property.GetMaxLength());
    }

    [Theory]
    [MemberData(nameof(KeyClassColumns))]
    public void AddedEntity_OneOverTheBound_IsRejected(string entityName, string propertyName, int maxLength)
    {
        using var context = CreateDbContext();
        var entry = AddEntity(context, entityName);
        entry.Property(propertyName).CurrentValue = new string('a', maxLength + 1);

        var exception = Assert.Throws<InvalidOperationException>(() => context.SaveChanges());

        Assert.Contains(entry.Metadata.DisplayName(), exception.Message, StringComparison.Ordinal);
        Assert.Contains(propertyName, exception.Message, StringComparison.Ordinal);
        Assert.Contains(maxLength.ToString(CultureInfo.InvariantCulture), exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(KeyClassColumns))]
    public void AddedEntity_AtTheBound_IsAccepted(string entityName, string propertyName, int maxLength)
    {
        using var context = CreateDbContext();
        var entry = AddEntity(context, entityName);
        entry.Property(propertyName).CurrentValue = new string('a', maxLength);

        var exception = Record.Exception(() => JellyfinDbContext.EnforceKeyLengths(context.ChangeTracker.Entries().ToList()));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveChanges_AddedKeyOverTheBound_IsRejectedAndNothingIsStored(bool useAsync)
    {
        using (var context = CreateDbContext())
        {
            context.ApiKeys.Add(new ApiKey("key") { AccessToken = new string('t', 129) });

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => SaveAsync(context, useAsync)).ConfigureAwait(true);

            Assert.Contains("ApiKey.AccessToken", exception.Message, StringComparison.Ordinal);
            Assert.Contains("128", exception.Message, StringComparison.Ordinal);
        }

        using (var context = CreateDbContext())
        {
            Assert.Empty(context.ApiKeys);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveChanges_AddedKeyAtTheBound_IsStoredWhole(bool useAsync)
    {
        var token = new string('t', 128);

        using (var context = CreateDbContext())
        {
            context.ApiKeys.Add(new ApiKey("key") { AccessToken = token });
            await SaveAsync(context, useAsync).ConfigureAwait(true);
        }

        using (var context = CreateDbContext())
        {
            Assert.Equal(token, context.ApiKeys.Single().AccessToken);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveChanges_ModifiedKeyOverTheBound_IsRejectedAndTheStoredValueKept(bool useAsync)
    {
        var token = new string('t', 128);

        using (var context = CreateDbContext())
        {
            context.ApiKeys.Add(new ApiKey("key") { AccessToken = token });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        using (var context = CreateDbContext())
        {
            context.ApiKeys.Single().AccessToken = new string('u', 129);

            await Assert.ThrowsAsync<InvalidOperationException>(() => SaveAsync(context, useAsync)).ConfigureAwait(true);
        }

        using (var context = CreateDbContext())
        {
            Assert.Equal(token, context.ApiKeys.AsNoTracking().Single().AccessToken);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveChanges_StoredKeyOverTheBound_StaysReadableAndSavesWhileUnchanged(bool useAsync)
    {
        var token = new string('t', 200);
        await StoreApiKeyWithAccessTokenAsync(token).ConfigureAwait(true);

        using (var context = CreateDbContext())
        {
            var key = context.ApiKeys.Single();
            Assert.Equal(token, key.AccessToken);
            key.Name = "renamed";
            await SaveAsync(context, useAsync).ConfigureAwait(true);
        }

        using (var context = CreateDbContext())
        {
            var key = context.ApiKeys.AsNoTracking().Single();
            Assert.Equal("renamed", key.Name);
            Assert.Equal(token, key.AccessToken);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveChanges_StoredKeyOverTheBoundUpdatedAsAWhole_SavesWhileUnchanged(bool useAsync)
    {
        var token = new string('t', 200);
        await StoreApiKeyWithAccessTokenAsync(token).ConfigureAwait(true);
        ApiKey key;
        using (var context = CreateDbContext())
        {
            key = await context.ApiKeys.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        using (var context = CreateDbContext())
        {
            key.Name = "renamed";
            context.ApiKeys.Update(key);
            await SaveAsync(context, useAsync).ConfigureAwait(true);
        }

        using (var context = CreateDbContext())
        {
            var stored = context.ApiKeys.AsNoTracking().Single();
            Assert.Equal("renamed", stored.Name);
            Assert.Equal(token, stored.AccessToken);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveChanges_StoredKeyOverTheBoundAttachedAsModified_SavesWhileUnchanged(bool useAsync)
    {
        var token = new string('t', 200);
        await StoreApiKeyWithAccessTokenAsync(token).ConfigureAwait(true);
        ApiKey key;
        using (var context = CreateDbContext())
        {
            key = await context.ApiKeys.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        using (var context = CreateDbContext())
        {
            key.Name = "renamed";
            context.Attach(key).State = EntityState.Modified;
            await SaveAsync(context, useAsync).ConfigureAwait(true);
        }

        using (var context = CreateDbContext())
        {
            var stored = context.ApiKeys.AsNoTracking().Single();
            Assert.Equal("renamed", stored.Name);
            Assert.Equal(token, stored.AccessToken);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveChanges_StoredKeyOverTheBoundChangedToAShorterValue_IsSaved(bool useAsync)
    {
        await StoreApiKeyWithAccessTokenAsync(new string('t', 200)).ConfigureAwait(true);

        using (var context = CreateDbContext())
        {
            context.ApiKeys.Single().AccessToken = new string('u', 128);
            await SaveAsync(context, useAsync).ConfigureAwait(true);
        }

        using (var context = CreateDbContext())
        {
            Assert.Equal(new string('u', 128), context.ApiKeys.AsNoTracking().Single().AccessToken);
        }
    }

    [Fact]
    public async Task SaveChanges_DeviceWithAStoredIdOverTheBoundUpdatedAsAWhole_SavesWhileUnchanged()
    {
        var deviceId = new string('d', 300);
        var userId = await StoreDeviceWithIdAsync(deviceId).ConfigureAwait(true);
        Device device;
        using (var context = CreateDbContext())
        {
            device = await context.Devices.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        using (var context = CreateDbContext())
        {
            device.DeviceName = "renamed";
            context.Devices.Update(device);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        using (var context = CreateDbContext())
        {
            var stored = context.Devices.AsNoTracking().Single(e => e.UserId.Equals(userId));
            Assert.Equal("renamed", stored.DeviceName);
            Assert.Equal(deviceId, stored.DeviceId);
        }
    }

    [Fact]
    public async Task SaveChanges_UserWithAStoredNameOverTheBoundAttachedAsModified_SavesWhileUnchanged()
    {
        SkipWhereIndexedTextIsBounded();
        var username = new string('u', 300);
        var userId = Guid.NewGuid();
        using (var context = CreateDbContext())
        {
            context.Users.Add(new User("user", "auth", "reset") { Id = userId });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            await context.Users
                .ExecuteUpdateAsync(e => e.SetProperty(f => f.Username, username), TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
        }

        User user;
        using (var context = CreateDbContext())
        {
            user = await context.Users.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        using (var context = CreateDbContext())
        {
            user.AuthenticationProviderId = "renamed";
            context.Attach(user).State = EntityState.Modified;
            await context.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        using (var context = CreateDbContext())
        {
            var stored = context.Users.AsNoTracking().Single();
            Assert.Equal("renamed", stored.AuthenticationProviderId);
            Assert.Equal(username, stored.Username);
        }
    }

    [Fact]
    public void SaveChanges_ColumnOutsideAnyIndex_IsNotLimited()
    {
        var overview = new string('o', 5000);

        using (var context = CreateDbContext())
        {
            var userId = Guid.NewGuid();
            context.Users.Add(new User("user", "auth", "reset") { Id = userId });
            context.ActivityLogs.Add(new ActivityLog("Logged", "Test", userId) { Overview = overview });
            context.SaveChanges();
        }

        using (var context = CreateDbContext())
        {
            Assert.Equal(overview, context.ActivityLogs.Single().Overview);
        }
    }

    private static bool IsKeyClass(IReadOnlyProperty property)
    {
        return property.ClrType == typeof(string) && (property.IsKey() || property.GetContainingIndexes().Any());
    }

    private static EntityEntry AddEntity(JellyfinDbContext context, string entityName)
    {
        var entityType = context.GetService<IDesignTimeModel>().Model.FindEntityType(entityName)!;
        var entity = RuntimeHelpers.GetUninitializedObject(entityType.ClrType);
        foreach (var key in entityType.GetProperties().Where(p => p.ClrType == typeof(string) && p.IsKey()))
        {
            key.PropertyInfo!.SetValue(entity, string.Empty);
        }

        return context.Add(entity);
    }

#pragma warning disable CA1849 // The synchronous overload is one of the two cases under test.
    private static Task SaveAsync(JellyfinDbContext context, bool useAsync)
    {
        return useAsync
            ? context.SaveChangesAsync(TestContext.Current.CancellationToken)
            : Task.FromResult(context.SaveChanges());
    }
#pragma warning restore CA1849

    private static void SkipWhereIndexedTextIsBounded()
    {
        Assert.SkipWhen(
            TestDatabaseEnvironment.Provider is TestDbProvider.MySql or TestDbProvider.MariaDb,
            "The column of an indexed string has its maximum length on this engine, so no row can be stored over it.");
    }

    private async Task StoreApiKeyWithAccessTokenAsync(string token)
    {
        SkipWhereIndexedTextIsBounded();
        using var context = CreateDbContext();
        context.ApiKeys.Add(new ApiKey("key"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Written around the save hook, as a row stored before the bound existed.
        await context.ApiKeys
            .ExecuteUpdateAsync(e => e.SetProperty(f => f.AccessToken, token), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
    }

    private async Task<Guid> StoreDeviceWithIdAsync(string deviceId)
    {
        SkipWhereIndexedTextIsBounded();
        var userId = Guid.NewGuid();
        using var context = CreateDbContext();
        context.Users.Add(new User("user", "auth", "reset") { Id = userId });
        context.Devices.Add(new Device(userId, "app", "1.0", "name", "device"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Written around the save hook, as a row stored before the bound existed.
        await context.Devices
            .ExecuteUpdateAsync(e => e.SetProperty(f => f.DeviceId, deviceId), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        return userId;
    }
}
