using MasterNet.Application.Core;

namespace MasterNet.Application.Students.GetStudents;

public class GetStudentsRequest : PagingParams
{

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }

}
