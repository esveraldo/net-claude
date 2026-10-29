using System.Net;
using System.Net.Http.Json;

namespace MasterNet.WebApi.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public class StudentsEndpointsTests
{
    private readonly HttpClient _client;

    // Ids fijos sembrados por SeedDatabase.SeedStudentsAsync desde SeedData/student.json.
    private static readonly Guid SeededStudentWithCoursesId = Guid.Parse("01a07bbb-3b60-7835-a51c-a970362aecea"); // Ana Martinez
    private static readonly Guid SeededCourseId1 = Guid.Parse("90314795-b797-42fe-82ae-2ac67bfff821");
    private static readonly Guid SeededCourseId2 = Guid.Parse("2ffc7d10-cb1e-4229-aa1c-58caa712c294");

    public StudentsEndpointsTests(MasterNetApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string UniqueSuffix() => Guid.NewGuid().ToString("N")[..8];

    private async Task<Guid> CreateStudentAsync(string firstName, string lastName, string email, List<Guid>? courseIds = null)
    {
        var response = await _client.PostAsJsonAsync("/api/students", new
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            CourseIds = courseIds
        });

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(TestJson.Options);
    }

    [Fact]
    public async Task Post_Should_Create_Student_And_Return_Version7_Id()
    {
        var suffix = UniqueSuffix();

        var response = await _client.PostAsJsonAsync("/api/students", new
        {
            FirstName = "Test",
            LastName = $"Version7-{suffix}",
            Email = $"version7-{suffix}@example.com"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var id = await response.Content.ReadFromJsonAsync<Guid>(TestJson.Options);
        Assert.NotEqual(Guid.Empty, id);

        // Formato "xxxxxxxx-xxxx-Vxxx-...": el primer carácter del tercer grupo es el
        // dígito de versión del GUID (RFC 9562). Debe ser '7'.
        var versionNibble = id.ToString().Split('-')[2][0];
        Assert.Equal('7', versionNibble);
    }

    [Fact]
    public async Task Post_Should_Return_400_When_Fields_Are_Missing()
    {
        var response = await _client.PostAsJsonAsync("/api/students", new
        {
            FirstName = "",
            LastName = "",
            Email = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("FirstName", body);
        Assert.Contains("LastName", body);
        Assert.Contains("Email", body);
    }

    [Fact]
    public async Task Post_Should_Return_404_When_CourseId_Does_Not_Exist()
    {
        var suffix = UniqueSuffix();

        var response = await _client.PostAsJsonAsync("/api/students", new
        {
            FirstName = "Test",
            LastName = $"UnknownCourse-{suffix}",
            Email = $"unknown-course-{suffix}@example.com",
            CourseIds = new List<Guid> { Guid.NewGuid() }
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Should_Associate_Existing_Courses()
    {
        var suffix = UniqueSuffix();

        var studentId = await CreateStudentAsync(
            "Test",
            $"Associate-{suffix}",
            $"associate-{suffix}@example.com",
            new List<Guid> { SeededCourseId1, SeededCourseId2 });

        var getResponse = await _client.GetAsync($"/api/students/{studentId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var student = await getResponse.Content.ReadFromJsonAsync<StudentDto>(TestJson.Options);

        Assert.NotNull(student);
        Assert.NotNull(student!.Courses);
        Assert.Equal(2, student.Courses!.Count);
        Assert.Contains(student.Courses, c => c.Id == SeededCourseId1);
        Assert.Contains(student.Courses, c => c.Id == SeededCourseId2);
    }

    [Fact]
    public async Task Put_Should_Update_Existing_Student()
    {
        var suffix = UniqueSuffix();
        var studentId = await CreateStudentAsync("Original", $"Name-{suffix}", $"original-{suffix}@example.com");

        var updatedEmail = $"updated-{suffix}@example.com";
        var putResponse = await _client.PutAsJsonAsync($"/api/students/{studentId}", new
        {
            FirstName = "Updated",
            LastName = $"Name-{suffix}",
            Email = updatedEmail
        });

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/students/{studentId}");
        var student = await getResponse.Content.ReadFromJsonAsync<StudentDto>(TestJson.Options);

        Assert.NotNull(student);
        Assert.Equal("Updated", student!.FirstName);
        Assert.Equal(updatedEmail, student.Email);
    }

    [Fact]
    public async Task Put_Should_Return_404_For_Unknown_Id()
    {
        var response = await _client.PutAsJsonAsync($"/api/students/{Guid.NewGuid()}", new
        {
            FirstName = "Ghost",
            LastName = "Student",
            Email = "ghost@example.com"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Should_Remove_Student()
    {
        var suffix = UniqueSuffix();
        var studentId = await CreateStudentAsync("ToDelete", $"Name-{suffix}", $"todelete-{suffix}@example.com");

        var deleteResponse = await _client.DeleteAsync($"/api/students/{studentId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var deleted = await deleteResponse.Content.ReadFromJsonAsync<bool>(TestJson.Options);
        Assert.True(deleted);

        var getResponse = await _client.GetAsync($"/api/students/{studentId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_Should_Return_404_For_Unknown_Id()
    {
        var response = await _client.DeleteAsync($"/api/students/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_All_Should_Return_Paged_Students()
    {
        var suffix = UniqueSuffix();
        var uniqueLastName = $"Paged-{suffix}";
        await CreateStudentAsync("Test", uniqueLastName, $"paged-{suffix}@example.com");

        var response = await _client.GetAsync($"/api/students?LastName={uniqueLastName}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<PagedStudentsDto>(TestJson.Options);

        Assert.NotNull(page);
        Assert.Equal(1, page!.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal(uniqueLastName, page.Items[0].LastName);
    }

    [Fact]
    public async Task Get_By_Id_Should_Return_Student_With_Courses()
    {
        var suffix = UniqueSuffix();
        var studentId = await CreateStudentAsync(
            "Test",
            $"WithCourses-{suffix}",
            $"withcourses-{suffix}@example.com",
            new List<Guid> { SeededCourseId1 });

        var response = await _client.GetAsync($"/api/students/{studentId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var student = await response.Content.ReadFromJsonAsync<StudentDto>(TestJson.Options);

        Assert.NotNull(student);
        Assert.Equal(studentId, student!.Id);
        Assert.NotNull(student.Courses);
        Assert.Single(student.Courses!);
        Assert.Equal(SeededCourseId1, student.Courses![0].Id);
    }

    [Fact]
    public async Task Get_By_Id_Should_Return_404_For_Unknown_Id()
    {
        var response = await _client.GetAsync($"/api/students/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Seed_Should_Provide_Students_Linked_To_Courses()
    {
        var response = await _client.GetAsync($"/api/students/{SeededStudentWithCoursesId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var student = await response.Content.ReadFromJsonAsync<StudentDto>(TestJson.Options);

        Assert.NotNull(student);
        Assert.NotNull(student!.Courses);
        Assert.NotEmpty(student.Courses!);
    }
}
