using Core.MediatOR.Contracts;
using MasterNet.Application.Core;
using MasterNet.Application.Ratings.GetRatings;
using MasterNet.Domain.Security;
using MasterNet.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using static MasterNet.Application.Ratings.GetRatings.GetRatingsQuery;

namespace MasterNet.WebApi.Controllers;

[ApiController]
[Route("api/ratings")]
public class RatesController : ControllerBase
{
    private readonly IMediatOR _sender;

    public RatesController(IMediatOR sender)
    {
        _sender = sender;
    }

    [Authorize(Policy = PolicyMaster.RATING_READ)]
    [HttpGet]
    [ProducesResponseType<PagedList<RatingResponse>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> PaginationRatings
    (
        [FromQuery] GetRatingsRequest request,
        CancellationToken cancellationToken
    )
    {
        var query = new GetRatingsQueryRequest
        {
            RatingsRequest = request
        };
        var results = await _sender.Send(query, cancellationToken);
        return this.FromResult(results);
    }

}
