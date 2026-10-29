using Core.MediatOR.Contracts;
using MasterNet.Application.Core;
using MasterNet.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace MasterNet.Application.Students.StudentUpdate;

public class StudentUpdateCommand
{

    public record StudentUpdateCommandRequest(
        StudentUpdateRequest StudentUpdateRequest,
        Guid? StudentId
    ) : IRequest<Result<Guid>>, ICommandBase;

    internal class StudentUpdateCommandHandler
    : IRequestHandler<StudentUpdateCommandRequest, Result<Guid>>
    {
        private readonly MasterNetDbContext _context;

        public StudentUpdateCommandHandler(MasterNetDbContext context)
        {
            _context = context;
        }

        public async Task<Result<Guid>> Handle(
            StudentUpdateCommandRequest request,
            CancellationToken cancellationToken
        )
        {
            var studentId = request.StudentId;

            var student = await _context.Students!
            .FirstOrDefaultAsync(x => x.Id == studentId, cancellationToken);

            if (student is null)
            {
                return Result<Guid>.NotFound("The student does not exist");
            }

            student.FirstName = request.StudentUpdateRequest.FirstName;
            student.LastName = request.StudentUpdateRequest.LastName;
            student.Email = request.StudentUpdateRequest.Email;

            _context.Entry(student).State = EntityState.Modified;
            var result = await _context.SaveChangesAsync(cancellationToken) > 0;

            return result
                        ? Result<Guid>.Success(student.Id)
                        : Result<Guid>.Failure("Errors updating the student");

        }
    }

    public sealed class StudentUpdateCommandRequestValidator
    : IValidator<StudentUpdateCommandRequest>
    {
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        public IEnumerable<ValidationError> Validate(StudentUpdateCommandRequest req)
        {
            if (req.StudentId is null || req.StudentId == Guid.Empty)
                yield return new ValidationError("StudentId", "StudentId is required.");

            if (string.IsNullOrWhiteSpace(req.StudentUpdateRequest.FirstName))
                yield return new ValidationError("FirstName", "FirstName is required.");

            if (string.IsNullOrWhiteSpace(req.StudentUpdateRequest.LastName))
                yield return new ValidationError("LastName", "LastName is required.");

            if (string.IsNullOrWhiteSpace(req.StudentUpdateRequest.Email))
                yield return new ValidationError("Email", "Email is required.");
            else if (!EmailRegex.IsMatch(req.StudentUpdateRequest.Email))
                yield return new ValidationError("Email", "Email is not a valid email address.");
        }
    }

}
