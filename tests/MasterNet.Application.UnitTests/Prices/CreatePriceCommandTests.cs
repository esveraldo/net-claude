using MasterNet.Application.Prices.PriceCreate;
using MasterNet.Domain.Abstractions;
using MasterNet.Domain.Prices;
using Moq;
using static MasterNet.Application.Prices.PriceCreate.CreatePriceCommand;

namespace MasterNet.Application.UnitTests.Prices;

public class CreatePriceCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IGenericRepository<Price>> _priceRepositoryMock = new();

    public CreatePriceCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(u => u.Repository<Price>())
            .Returns(_priceRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessWithPriceId()
    {
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(true);

        var handler = new CreatePriceCommandHandler(_unitOfWorkMock.Object);
        var request = new CreatePriceCommandRequest("Standard Plan", 49.99m, 29.99m);

        var result = await handler.Handle(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        _priceRepositoryMock.Verify(
            r => r.AddAsync(It.Is<Price>(p =>
                p.Name == "Standard Plan" &&
                p.CurrentPrice == 49.99m &&
                p.PromotionPrice == 29.99m)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SaveChangesFails_ReturnsFailureResult()
    {
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(false);

        var handler = new CreatePriceCommandHandler(_unitOfWorkMock.Object);
        var request = new CreatePriceCommandRequest("Standard Plan", 49.99m, 29.99m);

        var result = await handler.Handle(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }
}

public class CreatePriceCommandRequestValidatorTests
{
    private readonly CreatePriceCommandRequestValidator _validator = new();

    [Fact]
    public void Validate_EmptyName_ReturnsValidationError()
    {
        var request = new CreatePriceCommandRequest("", 49.99m, 29.99m);

        var errors = _validator.Validate(request);

        Assert.Contains(errors, e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Validate_CurrentPriceZeroOrNegative_ReturnsValidationError(decimal currentPrice)
    {
        var request = new CreatePriceCommandRequest("Standard Plan", currentPrice, 0m);

        var errors = _validator.Validate(request);

        Assert.Contains(errors, e => e.PropertyName == "CurrentPrice");
    }

    [Fact]
    public void Validate_PromotionPriceNegative_ReturnsValidationError()
    {
        var request = new CreatePriceCommandRequest("Standard Plan", 49.99m, -1m);

        var errors = _validator.Validate(request);

        Assert.Contains(errors, e => e.PropertyName == "PromotionPrice");
    }

    [Fact]
    public void Validate_PromotionPriceGreaterThanCurrentPrice_ReturnsValidationError()
    {
        var request = new CreatePriceCommandRequest("Standard Plan", 49.99m, 59.99m);

        var errors = _validator.Validate(request);

        Assert.Contains(errors, e => e.PropertyName == "PromotionPrice");
    }

    [Fact]
    public void Validate_ValidRequest_ReturnsNoErrors()
    {
        var request = new CreatePriceCommandRequest("Standard Plan", 49.99m, 29.99m);

        var errors = _validator.Validate(request);

        Assert.Empty(errors);
    }
}
