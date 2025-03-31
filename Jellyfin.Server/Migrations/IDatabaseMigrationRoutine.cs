using System;
using Jellyfin.Server.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Migrations;

/// <summary>
/// Defines a migration that operates on the Database.
/// </summary>
internal interface IDatabaseMigrationRoutine : IMigrationRoutine
{
}
