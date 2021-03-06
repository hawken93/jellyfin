using System;
using System.Linq;
using Jellyfin.Api.Constants;
using Jellyfin.Api.ModelBinders;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Filters controller.
    /// </summary>
    [Route("")]
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class FilterController : BaseJellyfinApiController
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterController"/> class.
        /// </summary>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        public FilterController(ILibraryManager libraryManager, IUserManager userManager)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
        }

        /// <summary>
        /// Gets legacy query filters.
        /// </summary>
        /// <param name="userId">Optional. User id.</param>
        /// <param name="parentId">Optional. Parent id.</param>
        /// <param name="includeItemTypes">Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimited.</param>
        /// <param name="mediaTypes">Optional. Filter by MediaType. Allows multiple, comma delimited.</param>
        /// <response code="200">Legacy filters retrieved.</response>
        /// <returns>Legacy query filters.</returns>
        [HttpGet("Items/Filters")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<QueryFiltersLegacy> GetQueryFiltersLegacy(
            [FromQuery] Guid? userId,
            [FromQuery] string? parentId,
            [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] string[] includeItemTypes,
            [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] string[] mediaTypes)
        {
            var parentItem = string.IsNullOrEmpty(parentId)
                ? null
                : _libraryManager.GetItemById(parentId);

            var user = userId.HasValue && !userId.Equals(Guid.Empty)
                ? _userManager.GetUserById(userId.Value)
                : null;

            if (includeItemTypes.Length == 1
                && (string.Equals(includeItemTypes[0], nameof(BoxSet), StringComparison.OrdinalIgnoreCase)
                    || string.Equals(includeItemTypes[0], nameof(Playlist), StringComparison.OrdinalIgnoreCase)
                    || string.Equals(includeItemTypes[0], nameof(Trailer), StringComparison.OrdinalIgnoreCase)
                    || string.Equals(includeItemTypes[0], "Program", StringComparison.OrdinalIgnoreCase)))
            {
                parentItem = null;
            }

            var item = string.IsNullOrEmpty(parentId)
                ? user == null
                    ? _libraryManager.RootFolder
                    : _libraryManager.GetUserRootFolder()
                : parentItem;

            var query = new InternalItemsQuery
            {
                User = user,
                MediaTypes = mediaTypes,
                IncludeItemTypes = includeItemTypes,
                Recursive = true,
                EnableTotalRecordCount = false,
                DtoOptions = new DtoOptions
                {
                    Fields = new[] { ItemFields.Genres, ItemFields.Tags },
                    EnableImages = false,
                    EnableUserData = false
                }
            };

            var itemList = ((Folder)item!).GetItemList(query);
            return new QueryFiltersLegacy
            {
                Years = itemList.Select(i => i.ProductionYear ?? -1)
                    .Where(i => i > 0)
                    .Distinct()
                    .OrderBy(i => i)
                    .ToArray(),

                Genres = itemList.SelectMany(i => i.Genres)
                    .DistinctNames()
                    .OrderBy(i => i)
                    .ToArray(),

                Tags = itemList
                    .SelectMany(i => i.Tags)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(i => i)
                    .ToArray(),

                OfficialRatings = itemList
                    .Select(i => i.OfficialRating)
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(i => i)
                    .ToArray()
            };
        }

        /// <summary>
        /// Gets query filters.
        /// </summary>
        /// <param name="userId">Optional. User id.</param>
        /// <param name="parentId">Optional. Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
        /// <param name="includeItemTypes">Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimited.</param>
        /// <param name="isAiring">Optional. Is item airing.</param>
        /// <param name="isMovie">Optional. Is item movie.</param>
        /// <param name="isSports">Optional. Is item sports.</param>
        /// <param name="isKids">Optional. Is item kids.</param>
        /// <param name="isNews">Optional. Is item news.</param>
        /// <param name="isSeries">Optional. Is item series.</param>
        /// <param name="recursive">Optional. Search recursive.</param>
        /// <response code="200">Filters retrieved.</response>
        /// <returns>Query filters.</returns>
        [HttpGet("Items/Filters2")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<QueryFilters> GetQueryFilters(
            [FromQuery] Guid? userId,
            [FromQuery] string? parentId,
            [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] string[] includeItemTypes,
            [FromQuery] bool? isAiring,
            [FromQuery] bool? isMovie,
            [FromQuery] bool? isSports,
            [FromQuery] bool? isKids,
            [FromQuery] bool? isNews,
            [FromQuery] bool? isSeries,
            [FromQuery] bool? recursive)
        {
            var parentItem = string.IsNullOrEmpty(parentId)
                ? null
                : _libraryManager.GetItemById(parentId);

            var user = userId.HasValue && !userId.Equals(Guid.Empty)
                ? _userManager.GetUserById(userId.Value)
                : null;

            if (includeItemTypes.Length == 1
                && (string.Equals(includeItemTypes[0], nameof(BoxSet), StringComparison.OrdinalIgnoreCase)
                    || string.Equals(includeItemTypes[0], nameof(Playlist), StringComparison.OrdinalIgnoreCase)
                    || string.Equals(includeItemTypes[0], nameof(Trailer), StringComparison.OrdinalIgnoreCase)
                    || string.Equals(includeItemTypes[0], "Program", StringComparison.OrdinalIgnoreCase)))
            {
                parentItem = null;
            }

            var filters = new QueryFilters();
            var genreQuery = new InternalItemsQuery(user)
            {
                IncludeItemTypes = includeItemTypes,
                DtoOptions = new DtoOptions
                {
                    Fields = Array.Empty<ItemFields>(),
                    EnableImages = false,
                    EnableUserData = false
                },
                IsAiring = isAiring,
                IsMovie = isMovie,
                IsSports = isSports,
                IsKids = isKids,
                IsNews = isNews,
                IsSeries = isSeries
            };

            if ((recursive ?? true) || parentItem is UserView || parentItem is ICollectionFolder)
            {
                genreQuery.AncestorIds = parentItem == null ? Array.Empty<Guid>() : new[] { parentItem.Id };
            }
            else
            {
                genreQuery.Parent = parentItem;
            }

            if (includeItemTypes.Length == 1
                && (string.Equals(includeItemTypes[0], nameof(MusicAlbum), StringComparison.OrdinalIgnoreCase)
                    || string.Equals(includeItemTypes[0], nameof(MusicVideo), StringComparison.OrdinalIgnoreCase)
                    || string.Equals(includeItemTypes[0], nameof(MusicArtist), StringComparison.OrdinalIgnoreCase)
                    || string.Equals(includeItemTypes[0], nameof(Audio), StringComparison.OrdinalIgnoreCase)))
            {
                filters.Genres = _libraryManager.GetMusicGenres(genreQuery).Items.Select(i => new NameGuidPair
                {
                    Name = i.Item1.Name,
                    Id = i.Item1.Id
                }).ToArray();
            }
            else
            {
                filters.Genres = _libraryManager.GetGenres(genreQuery).Items.Select(i => new NameGuidPair
                {
                    Name = i.Item1.Name,
                    Id = i.Item1.Id
                }).ToArray();
            }

            return filters;
        }
    }
}
