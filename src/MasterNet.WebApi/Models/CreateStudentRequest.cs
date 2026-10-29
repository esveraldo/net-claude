namespace MasterNet.WebApi.Models;

public class CreateStudentRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public List<Guid>? CourseIds { get; set; }
}
