using System;

namespace MediaBrowser.Model.ApiClient
{
    public static class ApiHelpers
    {
        /// <summary>
        /// Gets the name of the slug.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>System.String.</returns>
        public static string GetSlugName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            return name.Replace('/', '-').Replace('?', '-').Replace('&', '-');
        }
    }
}
