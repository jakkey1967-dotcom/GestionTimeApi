using GestionTime.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionTime.Api.Controllers;

[ApiController]
[Route("api/v1/tags")]
[Authorize]
public class TagsController : ControllerBase
{
    private readonly GestionTimeDbContext _db;
    private readonly ILogger<TagsController> _logger;

    public TagsController(GestionTimeDbContext db, ILogger<TagsController> logger)
    {
        _db = db;
        _logger = logger;
    }
}
