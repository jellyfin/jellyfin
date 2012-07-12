using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Common.Json;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Controller
{
    public class UserController
    {
        public string UsersPath { get; set; }

        public UserController(string usersPath)
        {
            UsersPath = usersPath;
        }

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

        private User GetFromDirectory(string path)
        {
            string file = Path.Combine(path, "user.js");

            return JsonSerializer.Deserialize<User>(file);
        }

        public void CreateUser(User user)
        {
            user.Id = Guid.NewGuid();

            user.DateCreated = user.DateModified = DateTime.Now;

            string userFolder = Path.Combine(UsersPath, user.Id.ToString());

            Directory.CreateDirectory(userFolder);

            JsonSerializer.Serialize(user, Path.Combine(userFolder, "user.js"));
        }
    }
}
