#nullable disable

#pragma warning disable CS1591

using System;
using System.IO;
using System.Linq;
using Emby.Naming.Book;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;

namespace Emby.Server.Implementations.Library.Resolvers.Books
{
    public class BookResolver : ItemResolver<Book>
    {
        private readonly string[] _validExtensions = { ".azw", ".azw3", ".cb7", ".cbr", ".cbt", ".cbz", ".epub", ".mobi", ".pdf" };

        protected override Book Resolve(ItemResolveArgs args)
        {
            var collectionType = args.GetCollectionType();

            // Only process items that are in a collection folder containing books
            if (collectionType != CollectionType.books)
            {
                return null;
            }

            if (args.IsDirectory)
            {
                return GetBook(args);
            }

            var extension = Path.GetExtension(args.Path.AsSpan());

            if (!_validExtensions.Contains(extension, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var result = BookFileNameParser.Parse(Path.GetFileNameWithoutExtension(args.Path));

            return new Book
            {
                Path = args.Path,
                Name = result.Name ?? string.Empty,
                IndexNumber = result.Index,
                ProductionYear = result.Year,
                SeriesName = result.SeriesName ?? Path.GetFileName(Path.GetDirectoryName(args.Path)),
                IsInMixedFolder = true,
            };
        }

        private Book GetBook(ItemResolveArgs args)
        {
            var bookFiles = args.FileSystemChildren.Where(f =>
            {
                var fileExtension = Path.GetExtension(f.FullName.AsSpan());

                return _validExtensions.Contains(
                    fileExtension,
                    StringComparison.OrdinalIgnoreCase);
            }).ToList();

            // directory is only considered a book when it contains exactly one supported file
            // other library structures with multiple books to a directory will get picked up as individual files
            if (bookFiles.Count != 1)
            {
                return null;
            }

            var result = BookFileNameParser.Parse(Path.GetFileName(args.Path));

            return new Book
            {
                Path = bookFiles[0].FullName,
                Name = result.Name ?? string.Empty,
                IndexNumber = result.Index,
                ProductionYear = result.Year,
                SeriesName = result.SeriesName ?? string.Empty,
            };
        }
    }
}
