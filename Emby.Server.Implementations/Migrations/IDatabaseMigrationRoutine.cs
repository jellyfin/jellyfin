using Emby.Server.Implementations.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Emby.Server.Implementations.Migrations;

/// <summary>
/// Defines a migration that operates on the Database.
/// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
internal interface IDatabaseMigrationRoutine : IMigrationRoutine
#pragma warning restore CS0618 // Type or member is obsolete
{
}
