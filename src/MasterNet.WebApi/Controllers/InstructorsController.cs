using Core.MediatOR.Contracts;
using MasterNet.Application.Core;
using MasterNet.Application.Instructors.GetInstructors;
using MasterNet.Application.Instructors.InstructorCreate;
using MasterNet.Application.Instructors.InstructorUpdate;
using MasterNet.Domain.Security;
using MasterNet.WebApi.Extensions;
using MasterNet.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using static MasterNet.Application.Instructors.GetInstructors.GetInstructorsQuery;
using static MasterNet.Application.Instructors.InstructorCreate.InstructorCreateCommand;
using static MasterNet.Application.Instructors.InstructorUpdate.InstructorUpdateCommand;

namespace MasterNet.WebApi.Controllers;

[ApiController]
[Route("api/instructors")]
public class InstructorsController : ControllerBase
{
    private readonly IMediatOR _sender;

    public InstructorsController(IMediatOR sender)
    {
        _sender = sender;
    }

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType<PagedList<InstructorResponse>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> PaginationInstructor
    (
        [FromQuery] GetInstructorsRequest request,
        CancellationToken cancellationToken
    )
    {
        var query = new GetInstructorsQueryRequest
        {
            InstructorRequest = request
        };
        var results = await _sender.Send(query, cancellationToken);
        return this.FromResult(results);
    }

    [Authorize(Policy = PolicyMaster.INSTRUCTOR_CREATE)]
    [HttpPost]
    [ProducesResponseType<Guid>((int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> CreateInstructor
    (
        [FromBody] CreateInstructorRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new InstructorCreateCommandRequest
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email
        };

        var result = await _sender.Send(command, cancellationToken);
        return this.FromResult(result, StatusCodes.Status201Created);
    }

    [Authorize(Policy = PolicyMaster.INSTRUCTOR_UPDATE)]
    [HttpPut("{id}")]
    [ProducesResponseType<Guid>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> UpdateInstructor(
        Guid id,
        [FromBody] InstructorUpdateRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new InstructorUpdateCommandRequest(id, request);
        var result = await _sender.Send(command, cancellationToken);
        return this.FromResult(result);
    }
}
