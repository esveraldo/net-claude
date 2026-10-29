using System.Text.Json;

namespace MasterNet.WebApi.IntegrationTests.Students;

// DTOs propios del proyecto de tests: deliberadamente no referencian tipos de
// MasterNet.Application. Las pruebas ejercitan la API como caja negra vía HTTP.
public record StudentCourseDto(Guid? Id, string? Title, string? Description);

public record StudentDto(
    Guid? Id,
    string? FirstName,
    string? LastName,
    string? Email,
    List<StudentCourseDto>? Courses
);

public record PagedStudentsDto(
    int CurrentPage,
    int TotalPages,
    int PageSize,
    int TotalCount,
    List<StudentDto> Items
);

public static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
