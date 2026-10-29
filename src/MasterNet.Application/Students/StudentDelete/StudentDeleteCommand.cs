using Core.MediatOR.Contracts;
using MasterNet.Application.Core;
using MasterNet.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MasterNet.Application.Students.StudentDelete;

public class StudentDeleteCommand
{

    public record StudentDeleteCommandRequest(Guid? StudentId)
    : IRequest<Result<bool>>, ICommandBase;

    internal class StudentDeleteCommandHandler
    : IRequestHandler<StudentDeleteCommandRequest, Result<bool>>
    {
        private readonly MasterNetDbContext _context;

        public StudentDeleteCommandHandler(MasterNetDbContext context)
        {
            _context = context;
        }

        public async Task<Result<bool>> Handle(
            StudentDeleteCommandRequest request,
            CancellationToken cancellationToken
        )
        {
            var student = await _context.Students!
            .Include(x => x.Courses)
            .FirstOrDefaultAsync(x => x.Id == request.StudentId, cancellationToken);

            if (student is null)
            {
                return Result<bool>.NotFound("The student does not exist");
            }

            _context.Students!.Remove(student);

            var result = await _context.SaveChangesAsync(cancellationToken) > 0;

            return result
                            ? Result<bool>.Success(true)
                            : Result<bool>.Failure("Transaction error");

        }
    }
}
