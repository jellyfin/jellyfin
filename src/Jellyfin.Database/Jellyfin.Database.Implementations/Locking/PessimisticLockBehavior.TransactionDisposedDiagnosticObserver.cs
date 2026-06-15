using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jellyfin.Database.Implementations.Locking;

public partial class PessimisticLockBehavior
{
    private sealed partial class DbLock
    {
        private sealed class TransactionDisposedDiagnosticObserver : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>
        {
            private readonly object _syncLock = new();
            // The behavior is process-lifetime, so diagnostic subscriptions are intentionally kept
            // for the process lifetime too. Dropping them would make dispose-only transactions
            // invisible and could leave their write lock held.
            private readonly List<IDisposable> _subscriptions = [];

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(DiagnosticListener value)
            {
                if (!value.Name.Equals("Microsoft.EntityFrameworkCore", StringComparison.Ordinal))
                {
                    return;
                }

                lock (_syncLock)
                {
                    _subscriptions.Add(value.Subscribe(this));
                }
            }

            public void OnNext(KeyValuePair<string, object?> value)
            {
                // EF emits this event when a transaction object is disposed without an explicit
                // commit, rollback, or failure callback.
                if (value.Key.EndsWith(".TransactionDisposed", StringComparison.Ordinal)
                    && value.Value is TransactionEventData eventData)
                {
                    EndTransactionLock(eventData.Transaction);
                }
            }
        }
    }
}
