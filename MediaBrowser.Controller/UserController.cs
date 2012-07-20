using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Common.Json;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Controller
{
    /// <summary>
    /// Manages users within the system
    /// </summary>
    public class UserController
    {
        /// <summary>
        /// Gets or sets the path to folder that contains data for all the users
        /// </summary>
        public string UsersPath { get; set; }

        public UserController(string usersPath)
        {
            UsersPath = usersPath;
        }

        /// <summary>
        /// Gets all users within the system
        /// </summary>
        public IEnumerable<User> GetAllUsers()
        {
            if (!Directory.Exists(UsersPath))
            {
                Directory.CreateDirectory(UsersPath);
            }

            List<User> list = new List<User>();

            foreach (string folder in Directory.GetDirectories(UsersPath, "*", SearchOption.TopDirectoryOnly))
            {
                User item = GetFromDirectory(folder);

                if (item != null)
                {
                    list.Add(item);
                }
            }

            return list;
        }

        /// <summary>
        /// Gets a User from it's directory
        /// </summary>
        private User GetFromDirectory(string path)
        {
            string file = Path.Combine(path, "user.js");

            return JsonSerializer.DeserializeFromFile<User>(file);
        }

        /// <summary>
        /// Creates a User with a given name
        /// </summary>
        public User CreateUser(string name)
        {
            var now = DateTime.Now;

            User user = new User()
            {
                Name = name,
                Id = Guid.NewGuid(),
                DateCreated = now,
                DateModified = now
            };

            user.Path = Path.Combine(UsersPath, user.Id.ToString());

            Directory.CreateDirectory(user.Path);

            JsonSerializer.SerializeToFile(user, Path.Combine(user.Path, "user.js"));

            return user;
        }
    }
}
