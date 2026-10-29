using MasterNet.Domain.Abstractions;
using MasterNet.Domain.Courses;

namespace MasterNet.Domain.Students;

public class Student : BaseEntity
{
    // Guid v7: monótonamente creciente en el tiempo, a diferencia del Guid v4
    // (Guid.NewGuid()) usado por el resto de entidades del dominio. Mejora la
    // localidad del índice clusterizado en SQL Server para las inserciones nuevas.
    public Student() => Id = Guid.CreateVersion7();

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }

    public ICollection<Course>? Courses { get; set; }
    public ICollection<CourseStudent>? CourseStudents { get; set; }
}
