This was built using Sqlite source code.

Change configuration to release. Go to Sqlite2013 project, change target framework to .net 4.5, set symbols to

NET_45;SQLITE_STANDARD;USE_PREPARE_V2;THROW_ON_DISPOSED;PRELOAD_NATIVE_LIBRARY;TRACE_PRELOAD;TRACE_SHARED;TRACE_WARNING

In SqliteNetSettings.targets set

UseInteropDll = false
UseSqliteStandard = true