<<<<<<< HEAD
using System;
=======
using System.Net.Mime;
>>>>>>> origin/master
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api
{
    /// <summary>
    /// Base api controller for the API setting a default route.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    public class BaseJellyfinApiController : ControllerBase
    {
    }
}
