using Core.MediatOR.Contracts;
using MasterNet.Application.Core;
using MasterNet.Domain.Abstractions;
using MasterNet.Domain.Courses;
using MasterNet.Domain.Students;
using System.Text.RegularExpressions;

namespace MasterNet.Application.Students.StudentCreate;

public class CreateStudentCommand
{
    public record CreateStudentCommandRequest(
        string? FirstName,
        string? LastName,
        string? Email,
        List<Guid>? CourseIds = null
    ) : IRequest<Result<Guid>>, ICommandBase;

    internal class CreateStudentCommandHandler
    : IRequestHandler<CreateStudentCommandRequest, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateStudentCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(
            CreateStudentCommandRequest request,
            CancellationToken cancellationToken
        )
        {
            // El Id lo asigna el constructor de Student con Guid.CreateVersion7().
            var student = new Student
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email
            };

            if (request.CourseIds is { Count: > 0 })
            {
                student.Courses = new List<Course>();

                foreach (var courseId in request.CourseIds)
                {
                    var course = await _unitOfWork.Repository<Course>().GetByIdAsync(courseId);

                    if (course is null)
                    {
                        return Result<Guid>.NotFound("Course not found");
                    }

                    student.Courses.Add(course);
                }
            }

            await _unitOfWork.Repository<Student>().AddAsync(student);

            var result = await _unitOfWork.SaveChangesAsync();

            return result
                ? Result<Guid>.Success(student.Id)
                : Result<Guid>.Failure("Could not insert student");
        }
    }

    public sealed class CreateStudentCommandRequestValidator
    : IValidator<CreateStudentCommandRequest>
    {
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        public IEnumerable<ValidationError> Validate(CreateStudentCommandRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.FirstName))
                yield return new ValidationError("FirstName", "FirstName is required.");

            if (string.IsNullOrWhiteSpace(req.LastName))
                yield return new ValidationError("LastName", "LastName is required.");

            if (string.IsNullOrWhiteSpace(req.Email))
                yield return new ValidationError("Email", "Email is required.");
            else if (!EmailRegex.IsMatch(req.Email))
                yield return new ValidationError("Email", "Email is not a valid email address.");
        }
    }
}
