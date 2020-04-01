using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Session;

namespace MediaBrowser.Controller.Syncplay
{
    /// <summary>
    /// Class GroupInfo.
    /// </summary>
    public class GroupInfo
    {
        /// <summary>
        /// Default ping value used for users.
        /// </summary>
        public readonly long DefaulPing = 500;
        /// <summary>
        /// Gets or sets the group identifier.
        /// </summary>
        /// <value>The group identifier.</value>
        public readonly Guid GroupId = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the playing item.
        /// </summary>
        /// <value>The playing item.</value>
        public BaseItem PlayingItem { get; set; }

        /// <summary>
        /// Gets or sets whether playback is paused.
        /// </summary>
        /// <value>Playback is paused.</value>
        public bool IsPaused { get; set; }

        /// <summary>
        /// Gets or sets the position ticks.
        /// </summary>
        /// <value>The position ticks.</value>
        public long PositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the last activity.
        /// </summary>
        /// <value>The last activity.</value>
        public DateTime LastActivity { get; set; }

        /// <summary>
        /// Gets the partecipants.
        /// </summary>
        /// <value>The partecipants.</value>
        public readonly ConcurrentDictionary<string, GroupMember> Partecipants =
        new ConcurrentDictionary<string, GroupMember>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if a user is in this group.
        /// </summary>
        /// <value><c>true</c> if the user is in this group; <c>false</c> otherwise.</value>
        public bool ContainsUser(string sessionId)
        {
            return Partecipants.ContainsKey(sessionId);
        }

        /// <summary>
        /// Adds the user to the group.
        /// </summary>
        /// <param name="user">The session.</param>
        public void AddUser(SessionInfo user)
        {
            if (ContainsUser(user.Id.ToString())) return;
            var member = new GroupMember();
            member.Session = user;
            member.Ping = DefaulPing;
            member.IsBuffering = false;
            Partecipants[user.Id.ToString()] = member;
        }

        /// <summary>
        /// Removes the user from the group.
        /// </summary>
        /// <param name="user">The session.</param>

        public void RemoveUser(SessionInfo user)
        {
            if (!ContainsUser(user.Id.ToString())) return;
            GroupMember member;
            Partecipants.Remove(user.Id.ToString(), out member);
        }

        /// <summary>
        /// Updates the ping of a user.
        /// </summary>
        /// <param name="user">The session.</param>
        /// <param name="ping">The ping.</param>
        public void UpdatePing(SessionInfo user, long ping)
        {
            if (!ContainsUser(user.Id.ToString())) return;
            Partecipants[user.Id.ToString()].Ping = ping;
        }

        /// <summary>
        /// Gets the highest ping in the group.
        /// </summary>
        /// <value name="user">The highest ping in the group.</value>
        public long GetHighestPing()
        {
            long max = Int64.MinValue;
            foreach (var user in Partecipants.Values)
            {
                max = Math.Max(max, user.Ping);
            }
            return max;
        }

        /// <summary>
        /// Sets the user's buffering state.
        /// </summary>
        /// <param name="user">The session.</param>
        /// <param name="isBuffering">The state.</param>
        public void SetBuffering(SessionInfo user, bool isBuffering)
        {
            if (!ContainsUser(user.Id.ToString())) return;
            Partecipants[user.Id.ToString()].IsBuffering = isBuffering;
        }

        /// <summary>
        /// Gets the group buffering state.
        /// </summary>
        /// <value><c>true</c> if there is a user buffering in the group; <c>false</c> otherwise.</value>
        public bool IsBuffering()
        {
            foreach (var user in Partecipants.Values)
            {
                if (user.IsBuffering) return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the group is empty.
        /// </summary>
        /// <value><c>true</c> if the group is empty; <c>false</c> otherwise.</value>
        public bool IsEmpty()
        {
            return Partecipants.Count == 0;
        }
    }
}
