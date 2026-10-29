using Core.MediatOR.Contracts;
using MasterNet.Application.Core;
using MasterNet.Domain.Abstractions;
using MasterNet.Domain.Prices;

namespace MasterNet.Application.Prices.PriceCreate;

public class CreatePriceCommand
{
    public record CreatePriceCommandRequest(
        string? Name,
        decimal? CurrentPrice,
        decimal? PromotionPrice
    ) : IRequest<Result<Guid>>, ICommandBase;

    internal class CreatePriceCommandHandler
    : IRequestHandler<CreatePriceCommandRequest, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreatePriceCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(
            CreatePriceCommandRequest request,
            CancellationToken cancellationToken
        )
        {
            var price = new Price
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                CurrentPrice = request.CurrentPrice!.Value,
                PromotionPrice = request.PromotionPrice!.Value
            };

            await _unitOfWork.Repository<Price>().AddAsync(price);
            var result = await _unitOfWork.SaveChangesAsync();

            return result
                ? Result<Guid>.Success(price.Id)
                : Result<Guid>.Failure("Could not insert price");
        }
    }

    public sealed class CreatePriceCommandRequestValidator
    : IValidator<CreatePriceCommandRequest>
    {
        public IEnumerable<ValidationError> Validate(CreatePriceCommandRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                yield return new ValidationError("Name", "Name is required.");

            if (req.CurrentPrice is null || req.CurrentPrice <= 0)
                yield return new ValidationError("CurrentPrice", "CurrentPrice must be greater than 0.");

            if (req.PromotionPrice is null || req.PromotionPrice < 0)
                yield return new ValidationError("PromotionPrice", "PromotionPrice must be 0 or greater.");

            if (req.CurrentPrice is > 0 && req.PromotionPrice is >= 0
                && req.PromotionPrice > req.CurrentPrice)
                yield return new ValidationError("PromotionPrice", "PromotionPrice must not be greater than CurrentPrice.");
        }
    }
}
