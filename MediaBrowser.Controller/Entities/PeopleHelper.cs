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

            // If the type is GuestStar and there's already an Actor entry, then update it to avoid dupes
            if (person.Type == PersonKind.GuestStar)
            {
                var existing = people.FirstOrDefault(p => p.Name.Equals(person.Name, StringComparison.OrdinalIgnoreCase) && p.Type == PersonKind.Actor);

                if (existing is not null)
                {
                    existing.Type = PersonKind.GuestStar;
                    MergeExisting(existing, person);
                    return;
                }
            }

            if (person.Type == PersonKind.Actor)
            {
                // If the actor already exists without a role and we have one, fill it in
                var existing = people.FirstOrDefault(p => p.Name.Equals(person.Name, StringComparison.OrdinalIgnoreCase) && (p.Type == PersonKind.Actor || p.Type == PersonKind.GuestStar));
                if (existing is null)
                {
                    // Wasn't there - add it
                    people.Add(person);
                }
                else
                {
                    // Was there, if no role and we have one - fill it in
                    if (string.IsNullOrEmpty(existing.Role) && !string.IsNullOrEmpty(person.Role))
                    {
                        existing.Role = person.Role;
                    }

                    MergeExisting(existing, person);
                }
            }
            else
            {
                var existing = people.FirstOrDefault(p =>
                    string.Equals(p.Name, person.Name, StringComparison.OrdinalIgnoreCase)
                    && p.Type == person.Type);

                // Check for dupes based on the combination of Name and Type
                if (existing is null)
                {
                    people.Add(person);
                }
                else
                {
                    MergeExisting(existing, person);
                }
            }
        }

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
