#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Entities
{
    public static class PeopleHelper
    {
        public static void AddPerson(ICollection<PersonInfo> people, PersonInfo person)
        {
            ArgumentNullException.ThrowIfNull(person);
            ArgumentException.ThrowIfNullOrEmpty(person.Name);

            person.Name = person.Name.Trim();

            // Normalize
            if (string.Equals(person.Role, PersonType.GuestStar, StringComparison.OrdinalIgnoreCase))
            {
                person.Type = PersonKind.GuestStar;
            }
            else if (string.Equals(person.Role, PersonType.Director, StringComparison.OrdinalIgnoreCase))
            {
                person.Type = PersonKind.Director;
            }
            else if (string.Equals(person.Role, PersonType.Producer, StringComparison.OrdinalIgnoreCase))
            {
                person.Type = PersonKind.Producer;
            }
            else if (string.Equals(person.Role, PersonType.Writer, StringComparison.OrdinalIgnoreCase))
            {
                person.Type = PersonKind.Writer;
            }

            var cleanName = person.Name.GetCleanValue();

            // Check for dupes based on the combination of Name, Type and Role.
            // One pass, so every collected credit's name is cleaned once rather than per lookup.
            var sameCredits = people.Where(p => IsSameCredit(p, person, cleanName)).ToList();
            var existing = sameCredits.FirstOrDefault(p => string.Equals(p.Role ?? string.Empty, person.Role ?? string.Empty, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                if (string.IsNullOrEmpty(person.Role))
                {
                    existing = sameCredits.FirstOrDefault();
                }
                else
                {
                    // If the person already exists without a role and we have one, fill it in
                    existing = sameCredits.FirstOrDefault(p => string.IsNullOrEmpty(p.Role));
                    if (existing is not null)
                    {
                        existing.Role = person.Role;
                    }
                }
            }

            if (existing is null)
            {
                people.Add(person);
                return;
            }

            // If the type is GuestStar and there's already an Actor entry, then promote it to avoid dupes
            if (person.Type == PersonKind.GuestStar)
            {
                existing.Type = PersonKind.GuestStar;
            }

            MergeExisting(existing, person);
        }

        private static bool IsSameCredit(PersonInfo existing, PersonInfo person, string cleanName)
        {
            // Actor and GuestStar describe the same credit, a guest star is just a promoted actor.
            if (existing.Type != person.Type && !(IsCastKind(existing.Type) && IsCastKind(person.Type)))
            {
                return false;
            }

            // The clean value is lower case, so names equal but for case need no cleaning.
            if (!string.Equals(existing.Name, person.Name, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(existing.Name.GetCleanValue(), cleanName, StringComparison.Ordinal))
            {
                return false;
            }

            // One cast can hold two humans of one name, and only the ids say so.
            return existing.FindConflictingProvider(person.ProviderIds) is null;
        }

        private static bool IsCastKind(PersonKind kind)
            => kind is PersonKind.Actor or PersonKind.GuestStar;

        private static void MergeExisting(PersonInfo existing, PersonInfo person)
        {
            existing.SortOrder = person.SortOrder ?? existing.SortOrder;
            existing.ImageUrl = person.ImageUrl ?? existing.ImageUrl;

            foreach (var id in person.ProviderIds)
            {
                existing.SetProviderId(id.Key, id.Value);
            }
        }
    }
}
