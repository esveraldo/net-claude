using Core.Mappy.Extensions;
using Core.Mappy.Interfaces;
using Core.MediatOR.Contracts;
using MasterNet.Application.Core;
using MasterNet.Application.Students.GetStudent;
using MasterNet.Domain.Students;
using MasterNet.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace MasterNet.Application.Students.GetStudents;

public class GetStudentsQuery
{

    public record GetStudentsQueryRequest : IRequest<Result<PagedList<StudentResponse>>>
    {
        public GetStudentsRequest? StudentsRequest { get; set; }
    }

    internal class GetStudentsQueryHandler
    : IRequestHandler<GetStudentsQueryRequest, Result<PagedList<StudentResponse>>>
    {
        private readonly MasterNetDbContext _context;
        private readonly IMapper _mapper;

        public GetStudentsQueryHandler(MasterNetDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<PagedList<StudentResponse>>> Handle(
            GetStudentsQueryRequest request,
            CancellationToken cancellationToken
        )
        {

            IQueryable<Student> queryable = _context.Students!
                                            .Include(x => x.Courses);

            var predicate = ExpressionBuilder.New<Student>();
            if (!string.IsNullOrEmpty(request.StudentsRequest!.FirstName))
            {
                predicate = predicate
                .And(y => y.FirstName!.ToLower()
                .Contains(request.StudentsRequest.FirstName.ToLower()));
            }

            if (!string.IsNullOrEmpty(request.StudentsRequest!.LastName))
            {
                predicate = predicate
                .And(y => y.LastName!.ToLower()
                .Contains(request.StudentsRequest.LastName.ToLower()));
            }

            if (!string.IsNullOrEmpty(request.StudentsRequest!.Email))
            {
                predicate = predicate
                .And(y => y.Email!.ToLower()
                .Contains(request.StudentsRequest.Email.ToLower()));
            }

            queryable = queryable.Where(predicate);

            if (!string.IsNullOrEmpty(request.StudentsRequest!.OrderBy))
            {
                Expression<Func<Student, object>>? orderBySelector =
                                request.StudentsRequest.OrderBy!.ToLower() switch
                                {
                                    "firstname" => student => student.FirstName!,
                                    "email" => student => student.Email!,
                                    _ => student => student.LastName!
                                };

                bool orderBy = request.StudentsRequest.OrderAsc.HasValue
                            ? request.StudentsRequest.OrderAsc.Value
                            : true;

                queryable = orderBy
                            ? queryable.OrderBy(orderBySelector)
                            : queryable.OrderByDescending(orderBySelector);
            }
            else
            {
                queryable = queryable.OrderBy(x => x.LastName);
            }

            var studentsQuery = queryable
            .ProjectTo<StudentResponse>(_mapper.ConfigurationProvider)
            .AsQueryable();

            var pagination = await PagedList<StudentResponse>.CreateAsync(
                studentsQuery,
                request.StudentsRequest.PageNumber,
                request.StudentsRequest.PageSize
            );

            return Result<PagedList<StudentResponse>>.Success(pagination);

        }
    }


}
