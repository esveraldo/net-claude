using MasterNet.Domain.Security;
using MasterNet.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace MasterNet.WebApi.Controllers;

[Authorize(Policy = PolicyMaster.COURSE_READ)]
[Route("odata/courses")]
public class CourseReportController : ODataController
{
    private readonly MasterNetDbContext _dbContext;

    public CourseReportController(MasterNetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [EnableQuery(PageSize = 50, MaxExpansionDepth = 2, MaxTop = 100)]
    [HttpGet]
    public IActionResult Get() => Ok(_dbContext.Courses!.AsQueryable());
}
