using Core.MediatOR.Contracts;
using MasterNet.Application.Core;
using MasterNet.Domain.Abstractions;
using MasterNet.Domain.Instructors;

namespace MasterNet.Application.Instructors.InstructorUpdate;

public class InstructorUpdateCommand
{
    public record InstructorUpdateCommandRequest(
        Guid InstructorId,
        InstructorUpdateRequest InstructorUpdateRequest
    ) : IRequest<Result<Guid>>, ICommandBase;

    internal class InstructorUpdateCommandHandler
    : IRequestHandler<InstructorUpdateCommandRequest, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public InstructorUpdateCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(
            InstructorUpdateCommandRequest request,
            CancellationToken cancellationToken
        )
        {
            var instructorRepository = _unitOfWork.Repository<Instructor>();
            var instructor = await instructorRepository.GetByIdAsync(request.InstructorId);

            if (instructor is null)
            {
                return Result<Guid>.NotFound("The instructor does not exist");
            }

            instructor.FirstName = request.InstructorUpdateRequest.FirstName;
            instructor.LastName = request.InstructorUpdateRequest.LastName;
            instructor.Degree = request.InstructorUpdateRequest.Degree;

            var result = await _unitOfWork.SaveChangesAsync();

            return result
                ? Result<Guid>.Success(instructor.Id)
                : Result<Guid>.Failure("Errors updating the instructor");
        }
    }

    public sealed class InstructorUpdateCommandRequestValidator
    : IValidator<InstructorUpdateCommandRequest>
    {
        public IEnumerable<ValidationError> Validate(InstructorUpdateCommandRequest req)
        {
            if (req.InstructorId == Guid.Empty)
                yield return new ValidationError("InstructorId", "InstructorId is required.");

            if (string.IsNullOrWhiteSpace(req.InstructorUpdateRequest.FirstName))
                yield return new ValidationError("FirstName", "FirstName is required.");

            if (string.IsNullOrWhiteSpace(req.InstructorUpdateRequest.LastName))
                yield return new ValidationError("LastName", "LastName is required.");
        }
    }
}
