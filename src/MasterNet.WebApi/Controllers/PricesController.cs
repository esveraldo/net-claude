using Core.MediatOR.Contracts;
using MasterNet.Application.Core;
using MasterNet.Application.Prices.GetPrices;
using MasterNet.Application.Prices.PriceCreate;
using MasterNet.Domain.Security;
using MasterNet.WebApi.Extensions;
using MasterNet.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using static MasterNet.Application.Prices.GetPrices.GetPricesQuery;
using static MasterNet.Application.Prices.PriceCreate.CreatePriceCommand;

namespace MasterNet.WebApi.Controllers;

[ApiController]
[Route("api/prices")]
public class PricesController : ControllerBase
{
    private readonly IMediatOR _sender;

    public PricesController(IMediatOR sender)
    {
        _sender = sender;
    }

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType<PagedList<PriceResponse>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> PaginationPrice
    (
        [FromQuery] GetPricesRequest request,
        CancellationToken cancellationToken
    )
    {
        var query = new GetPricesQueryRequest
        {
            PricesRequest = request
        };
        var results = await _sender.Send(query, cancellationToken);
        return this.FromResult(results);
    }

    [Authorize(Policy = PolicyMaster.PRICE_CREATE)]
    [HttpPost]
    [ProducesResponseType<Guid>((int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> CreatePrice(
        [FromBody] CreatePriceRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new CreatePriceCommandRequest(request.Name, request.CurrentPrice, request.PromotionPrice);
        var result = await _sender.Send(command, cancellationToken);
        return this.FromResult(result, StatusCodes.Status201Created);
    }

}
