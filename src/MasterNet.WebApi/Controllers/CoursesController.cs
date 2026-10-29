using Core.MediatOR.Contracts;
using MasterNet.Application.Core;
using MasterNet.Application.Courses.CourseUpdate;
using MasterNet.Application.Courses.GetCourse;
using MasterNet.Application.Courses.GetCourses;
using MasterNet.Domain.Security;
using MasterNet.WebApi.Extensions;
using MasterNet.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using static MasterNet.Application.Courses.CourseCreate.CreateCourseCommand;
using static MasterNet.Application.Courses.CourseDelete.CourseDeleteCommand;
using static MasterNet.Application.Courses.CourseReport.GetCoursesReportQuery;
using static MasterNet.Application.Courses.CourseUpdate.CourseUpdateCommand;
using static MasterNet.Application.Courses.GetCourse.GetCourseQuery;
using static MasterNet.Application.Courses.GetCourses.GetCoursesQuery;

namespace MasterNet.WebApi.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController : ControllerBase
{
    private readonly IMediatOR _sender;

    public CoursesController(IMediatOR sender)
    {
        _sender = sender;
    }

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType<PagedList<CourseResponse>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> PaginationCourses(
        [FromQuery] GetCoursesRequest request,
        CancellationToken cancellationToken
    )
    {
        var query = new GetCoursesQueryRequest { CoursesRequest = request };
        var result = await _sender.Send(query, cancellationToken);

        return this.FromResult(result);
    }


    [Authorize(Policy = PolicyMaster.COURSE_WRITE)]
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB: acota la foto adjunta al curso
    [ProducesResponseType<Guid>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> CourseCreate(
        [FromForm] CreateCourseRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new CreateCourseCommandRequest(request.Title, request.Description, request.PriceId, request.InstructorId, request.PublishedAt);

        if (request.Photo is not null)
        {
            using var ms = new MemoryStream();
            await request.Photo.CopyToAsync(ms, cancellationToken);
            command = command with { Photo = ms.ToArray() };
        }

        var result = await _sender.Send(command, cancellationToken);

        return this.FromResult(result);
    }

    [Authorize(Policy = PolicyMaster.COURSE_UPDATE)]
    [HttpPut("{id}")]
    [ProducesResponseType<Guid>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> CourseUpdate(
        Guid id,
        [FromBody] CourseUpdateRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new CourseUpdateCommandRequest(request, id);
        var result = await _sender.Send(command, cancellationToken);
        return this.FromResult(result);
    }

    [Authorize(Policy = PolicyMaster.COURSE_DELETE)]
    [HttpDelete("{id}")]
    [ProducesResponseType<bool>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> CourseDelete(
        Guid id,
        CancellationToken cancellationToken
        )
    {
        var command = new CourseDeleteCommandRequest(id);
        var result = await _sender.Send(command, cancellationToken);
        return this.FromResult(result);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    [ProducesResponseType<CourseResponse>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> CourseGet(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var query = new GetCourseQueryRequest { Id = id };
        var result = await _sender.Send(query, cancellationToken);
        return this.FromResult(result);
    }

    [AllowAnonymous]
    [HttpGet("report")]
    [Produces("text/csv")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public async Task<IActionResult> ReportCSV(CancellationToken cancellationToken)
    {
        var query = new GetCoursesReportQueryRequest();
        var bytes = await _sender.Send(query, cancellationToken);

        if (bytes is null || bytes.Length == 0)
        {
            return NoContent();
        }

        return File(bytes, "text/csv", "courses.csv");
    }
}
