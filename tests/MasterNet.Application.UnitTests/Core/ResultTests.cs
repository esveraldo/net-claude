using MasterNet.Application.Core;

namespace MasterNet.Application.UnitTests.Core;

public class ResultTests
{
    [Fact]
    public void Success_SetsIsSuccessAndValue()
    {
        var result = Result<Guid>.Success(Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        Assert.Equal(ErrorType.Failure, result.Type);
    }

    [Fact]
    public void Failure_SetsErrorTypeFailure()
    {
        var result = Result<Guid>.Failure("Could not insert entity");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Failure, result.Type);
        Assert.Equal("Could not insert entity", result.Error);
    }

    [Fact]
    public void NotFound_SetsErrorTypeNotFound()
    {
        var result = Result<Guid>.NotFound("The course does not exist");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Type);
        Assert.Equal("The course does not exist", result.Error);
    }

    [Fact]
    public void Conflict_SetsErrorTypeConflict()
    {
        var result = Result<Guid>.Conflict("Email is already registered");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.Type);
        Assert.Equal("Email is already registered", result.Error);
    }

    [Fact]
    public void Unauthorized_SetsErrorTypeUnauthorized()
    {
        var result = Result<Guid>.Unauthorized("Invalid credentials");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.Type);
        Assert.Equal("Invalid credentials", result.Error);
    }

    [Fact]
    public void Failure_WithValidationErrors_SetsErrorTypeValidation()
    {
        var errors = new[] { new ValidationError("Name", "Name is required.") };

        var result = Result<Guid>.Failure(errors);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Type);
        Assert.NotNull(result.ValidationErrors);
        Assert.Single(result.ValidationErrors!);
    }
}
