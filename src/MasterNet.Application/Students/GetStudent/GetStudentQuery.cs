using Core.Mappy.Extensions;
using Core.Mappy.Interfaces;
using Core.MediatOR.Contracts;
using MasterNet.Application.Core;
using MasterNet.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MasterNet.Application.Students.GetStudent;

public class GetStudentQuery
{

    public record GetStudentQueryRequest
    : IRequest<Result<StudentResponse>>
    {
        public Guid Id { get; set; }
    }

    internal class GetStudentQueryHandler
    : IRequestHandler<GetStudentQueryRequest, Result<StudentResponse>>
    {
        private readonly MasterNetDbContext _context;
        private readonly IMapper _mapper;

        public GetStudentQueryHandler(MasterNetDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<StudentResponse>> Handle(
            GetStudentQueryRequest request,
            CancellationToken cancellationToken
        )
        {
            var student = await _context.Students!.Where(x => x.Id == request.Id)
                            .Include(x => x.Courses)
                            .ProjectTo<StudentResponse>(_mapper.ConfigurationProvider)
                            .FirstOrDefaultAsync(cancellationToken);

            if (student is null)
            {
                return Result<StudentResponse>.NotFound("The student does not exist");
            }

            return Result<StudentResponse>.Success(student);
        }
    }
}

public record StudentResponse(
    Guid? Id,
    string? FirstName,
    string? LastName,
    string? Email,
    List<StudentCourseResponse> Courses
)
{
    public StudentResponse() : this(null, null, null, null, new())
    {
    }
}

public record StudentCourseResponse(
    Guid? Id,
    string? Title,
    string? Description
)
{
    public StudentCourseResponse() : this(null, null, null)
    {
    }
}
