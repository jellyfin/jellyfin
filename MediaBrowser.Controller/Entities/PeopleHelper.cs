#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
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

            // Check for dupes based on the combination of Name, Type and Role.
            var existing = people.FirstOrDefault(p => IsSameCredit(p, person)
                && string.Equals(p.Role ?? string.Empty, person.Role ?? string.Empty, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                if (string.IsNullOrEmpty(person.Role))
                {
                    existing = people.FirstOrDefault(p => IsSameCredit(p, person));
                }
                else
                {
                    // If the person already exists without a role and we have one, fill it in
                    existing = people.FirstOrDefault(p => IsSameCredit(p, person) && string.IsNullOrEmpty(p.Role));
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

        private static bool IsSameCredit(PersonInfo existing, PersonInfo person)
        {
            if (!string.Equals(existing.Name, person.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Actor and GuestStar describe the same credit, a guest star is just a promoted actor.
            if (IsCastKind(existing.Type) && IsCastKind(person.Type))
            {
                return true;
            }

            return existing.Type == person.Type;
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
