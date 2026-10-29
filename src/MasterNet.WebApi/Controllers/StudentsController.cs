using Core.MediatOR.Contracts;
using MasterNet.Application.Core;
using MasterNet.Application.Students.GetStudent;
using MasterNet.Application.Students.GetStudents;
using MasterNet.Application.Students.StudentUpdate;
using MasterNet.WebApi.Extensions;
using MasterNet.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using static MasterNet.Application.Students.GetStudent.GetStudentQuery;
using static MasterNet.Application.Students.GetStudents.GetStudentsQuery;
using static MasterNet.Application.Students.StudentCreate.CreateStudentCommand;
using static MasterNet.Application.Students.StudentDelete.StudentDeleteCommand;
using static MasterNet.Application.Students.StudentUpdate.StudentUpdateCommand;

namespace MasterNet.WebApi.Controllers;

[ApiController]
[Route("api/students")]
public class StudentsController : ControllerBase
{
    private readonly IMediatOR _sender;

    public StudentsController(IMediatOR sender)
    {
        _sender = sender;
    }

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType<PagedList<StudentResponse>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> PaginationStudents(
        [FromQuery] GetStudentsRequest request,
        CancellationToken cancellationToken
    )
    {
        var query = new GetStudentsQueryRequest { StudentsRequest = request };
        var result = await _sender.Send(query, cancellationToken);

        return this.FromResult(result);
    }

    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType<Guid>((int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> StudentCreate(
        [FromBody] CreateStudentRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new CreateStudentCommandRequest(request.FirstName, request.LastName, request.Email, request.CourseIds);
        var result = await _sender.Send(command, cancellationToken);
        return this.FromResult(result, StatusCodes.Status201Created);
    }

    [AllowAnonymous]
    [HttpPut("{id}")]
    [ProducesResponseType<Guid>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> StudentUpdate(
        Guid id,
        [FromBody] StudentUpdateRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new StudentUpdateCommandRequest(request, id);
        var result = await _sender.Send(command, cancellationToken);
        return this.FromResult(result);
    }

    [AllowAnonymous]
    [HttpDelete("{id}")]
    [ProducesResponseType<bool>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> StudentDelete(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var command = new StudentDeleteCommandRequest(id);
        var result = await _sender.Send(command, cancellationToken);
        return this.FromResult(result);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    [ProducesResponseType<StudentResponse>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> StudentGet(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var query = new GetStudentQueryRequest { Id = id };
        var result = await _sender.Send(query, cancellationToken);
        return this.FromResult(result);
    }
}
