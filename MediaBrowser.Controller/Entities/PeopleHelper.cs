using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Entities
{
    public static class PeopleHelper
    {
        public static void AddPerson(List<PersonInfo> people, PersonInfo person)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            if (string.IsNullOrEmpty(person.Name))
            {
                throw new ArgumentException("The person's name was empty or null.", nameof(person));
            }

            // Normalize
            if (string.Equals(person.Role, PersonType.GuestStar, StringComparison.OrdinalIgnoreCase))
            {
                person.Type = PersonType.GuestStar;
            }
            else if (string.Equals(person.Role, PersonType.Director, StringComparison.OrdinalIgnoreCase))
            {
                person.Type = PersonType.Director;
            }
            else if (string.Equals(person.Role, PersonType.Producer, StringComparison.OrdinalIgnoreCase))
            {
                person.Type = PersonType.Producer;
            }
            else if (string.Equals(person.Role, PersonType.Writer, StringComparison.OrdinalIgnoreCase))
            {
                person.Type = PersonType.Writer;
            }

            // If the type is GuestStar and there's already an Actor entry, then update it to avoid dupes
            if (string.Equals(person.Type, PersonType.GuestStar, StringComparison.OrdinalIgnoreCase))
            {
                var existing = people.FirstOrDefault(p => p.Name.Equals(person.Name, StringComparison.OrdinalIgnoreCase) && p.Type.Equals(PersonType.Actor, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    existing.Type = PersonType.GuestStar;
                    MergeExisting(existing, person);
                    return;
                }
            }

            if (string.Equals(person.Type, PersonType.Actor, StringComparison.OrdinalIgnoreCase))
            {
                // If the actor already exists without a role and we have one, fill it in
                var existing = people.FirstOrDefault(p => p.Name.Equals(person.Name, StringComparison.OrdinalIgnoreCase) && (p.Type.Equals(PersonType.Actor, StringComparison.OrdinalIgnoreCase) || p.Type.Equals(PersonType.GuestStar, StringComparison.OrdinalIgnoreCase)));
                if (existing == null)
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
                            string.Equals(p.Name, person.Name, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(p.Type, person.Type, StringComparison.OrdinalIgnoreCase));

                // Check for dupes based on the combination of Name and Type
                if (existing == null)
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

        public static bool ContainsPerson(List<PersonInfo> people, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            foreach (var i in people)
            {
                if (string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
