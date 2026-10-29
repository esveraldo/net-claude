using Core.MediatOR.Contracts;
using MasterNet.Application.Core;
using MasterNet.Domain.Abstractions;
using MasterNet.Domain.Instructors;


namespace MasterNet.Application.Instructors.InstructorCreate;

public class InstructorCreateCommand
{
    public class InstructorCreateCommandRequest : IRequest<Result<Guid>>, ICommandBase
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
    }

    public class InstructorCreateCommandHandler : IRequestHandler<InstructorCreateCommandRequest, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public InstructorCreateCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(InstructorCreateCommandRequest request, CancellationToken cancellationToken)
        {
            var instructor = new Instructor
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
            };

            var instructorRepository = _unitOfWork.Repository<Instructor>();
            await instructorRepository.AddAsync(instructor);
            var result = await _unitOfWork.SaveChangesAsync();

            return result
                ? Result<Guid>.Success(instructor.Id)
                : Result<Guid>.Failure("Could not insert instructor");
        }
    }
}
