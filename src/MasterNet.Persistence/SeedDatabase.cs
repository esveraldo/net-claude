using System.Security.Claims;
using MasterNet.Domain.Courses;
using MasterNet.Domain.Devices;
using MasterNet.Domain.Instructors;
using MasterNet.Domain.Photos;
using MasterNet.Domain.Prices;
using MasterNet.Domain.Ratings;
using MasterNet.Domain.Security;
using MasterNet.Domain.Students;
using MasterNet.Persistence.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MasterNet.Persistence;

public static class SeedDatabase
{
    public static async Task SeedRolesAndUsersAsync(DbContext context, ILogger? logger, CancellationToken cancellationToken)
    {
        try
        {
            var userManager = context.GetService<UserManager<AppUser>>();
            var roleManager = context.GetService<RoleManager<IdentityRole>>();

            if (userManager.Users.Any()) return;

            var adminId = "d3b07384-d9a0-4c9b-8a0d-1b2e2b3c4d5e";
            var clientId = "e4b07384-d9a0-4c9b-8a0d-1b2e2b3c4d5f";

            var roleAdmin = new IdentityRole
            {
                Id = adminId,
                Name = CustomRoles.ADMIN,
                NormalizedName = CustomRoles.ADMIN.ToUpperInvariant()
            };
            var roleClient = new IdentityRole
            {
                Id = clientId,
                Name = CustomRoles.CLIENT,
                NormalizedName = CustomRoles.CLIENT.ToUpperInvariant()
            };

            if (!await roleManager.RoleExistsAsync(CustomRoles.ADMIN))
                await LogIdentityResultAsync(roleManager.CreateAsync(roleAdmin), logger, "Create role ADMIN");

            if (!await roleManager.RoleExistsAsync(CustomRoles.CLIENT))
                await LogIdentityResultAsync(roleManager.CreateAsync(roleClient), logger, "Create role CLIENT");

            var userAdmin = new AppUser
            {
                FullName = "Vaxi Drez",
                UserName = "vaxidrez",
                Email = "vaxi.drez@gmail.com"
            };

            var createAdminRes = await userManager.CreateAsync(userAdmin, "Password123$");
            await LogIdentityResultAsync(Task.FromResult(createAdminRes), logger, "Create user vaxidrez");
            if (createAdminRes.Succeeded)
            {
                await LogIdentityResultAsync(userManager.AddToRoleAsync(userAdmin, CustomRoles.ADMIN), logger, "Add user vaxidrez to ADMIN");
            }

            var userClient = new AppUser
            {
                FullName = "John Doe",
                UserName = "johndoe",
                Email = "john.doe@gmail.com"
            };

            var createClientRes = await userManager.CreateAsync(userClient, "Password123$");
            await LogIdentityResultAsync(Task.FromResult(createClientRes), logger, "Create user johndoe");
            if (createClientRes.Succeeded)
            {
                await LogIdentityResultAsync(userManager.AddToRoleAsync(userClient, CustomRoles.CLIENT), logger, "Add user johndoe to CLIENT");
            }

            async Task AddClaimIfMissingAsync(IdentityRole role, string type, string value, string op)
            {
                var claims = await roleManager.GetClaimsAsync(role);
                if (!claims.Any(c => c.Type == type && c.Value == value))
                {
                    await LogIdentityResultAsync(roleManager.AddClaimAsync(role, new Claim(type, value)), logger, op);
                }
            }

            roleAdmin = await roleManager.FindByNameAsync(CustomRoles.ADMIN) ?? roleAdmin;
            roleClient = await roleManager.FindByNameAsync(CustomRoles.CLIENT) ?? roleClient;

            await AddClaimIfMissingAsync(roleAdmin, CustomClaims.POLICIES, PolicyMaster.COURSE_READ, "AddClaim ADMIN COURSE_READ");
            await AddClaimIfMissingAsync(roleAdmin, CustomClaims.POLICIES, PolicyMaster.COURSE_UPDATE, "AddClaim ADMIN COURSE_UPDATE");
            await AddClaimIfMissingAsync(roleAdmin, CustomClaims.POLICIES, PolicyMaster.COURSE_WRITE, "AddClaim ADMIN COURSE_WRITE");
            await AddClaimIfMissingAsync(roleAdmin, CustomClaims.POLICIES, PolicyMaster.COURSE_DELETE, "AddClaim ADMIN COURSE_DELETE");
            await AddClaimIfMissingAsync(roleAdmin, CustomClaims.POLICIES, PolicyMaster.INSTRUCTOR_READ, "AddClaim ADMIN INSTRUCTOR_READ");
            await AddClaimIfMissingAsync(roleAdmin, CustomClaims.POLICIES, PolicyMaster.INSTRUCTOR_UPDATE, "AddClaim ADMIN INSTRUCTOR_UPDATE");
            await AddClaimIfMissingAsync(roleAdmin, CustomClaims.POLICIES, PolicyMaster.INSTRUCTOR_CREATE, "AddClaim ADMIN INSTRUCTOR_CREATE");
            await AddClaimIfMissingAsync(roleAdmin, CustomClaims.POLICIES, PolicyMaster.RATING_READ, "AddClaim ADMIN RATING_READ");
            await AddClaimIfMissingAsync(roleAdmin, CustomClaims.POLICIES, PolicyMaster.RATING_DELETE, "AddClaim ADMIN RATING_DELETE");
            await AddClaimIfMissingAsync(roleAdmin, CustomClaims.POLICIES, PolicyMaster.RATING_CREATE, "AddClaim ADMIN RATING_CREATE");
            await AddClaimIfMissingAsync(roleAdmin, CustomClaims.POLICIES, PolicyMaster.PRICE_CREATE, "AddClaim ADMIN PRICE_CREATE");

            await AddClaimIfMissingAsync(roleClient, CustomClaims.POLICIES, PolicyMaster.COURSE_READ, "AddClaim CLIENT COURSE_READ");
            await AddClaimIfMissingAsync(roleClient, CustomClaims.POLICIES, PolicyMaster.INSTRUCTOR_READ, "AddClaim CLIENT INSTRUCTOR_READ");
            await AddClaimIfMissingAsync(roleClient, CustomClaims.POLICIES, PolicyMaster.RATING_READ, "AddClaim CLIENT RATING_READ");
            await AddClaimIfMissingAsync(roleClient, CustomClaims.POLICIES, PolicyMaster.RATING_CREATE, "AddClaim CLIENT RATING_CREATE");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to seed roles and users");
        }

        static async Task LogIdentityResultAsync(Task<IdentityResult> task, ILogger? logger, string op)
        {
            try
            {
                var result = await task;
                if (result is null) return;
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    logger?.LogWarning("{Operation} failed: {Errors}", op, errors);
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Identity operation failed: {Op}", op);
            }
        }
    }

    public static async Task SeedPricesAsync(MasterNetDbContext dbContext, ILogger? logger, CancellationToken cancellationToken)
    {
        try
        {
            if (dbContext.Prices is null || dbContext.Prices.Any()) return;

            var json = TryLoadJsonFile("price.json");
            if (json is null) return;

            var prices = JsonConvert.DeserializeObject<List<Price>>(json);
            if (prices?.Count > 0)
            {
                foreach (var p in prices)
                {
                    p.Courses ??= new List<Course>();
                    p.CoursePrices ??= new List<CoursePrice>();
                }

                dbContext.Prices!.AddRange(prices);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to seed prices");
        }
    }

    public static async Task SeedInstructorsAsync(MasterNetDbContext dbContext, ILogger? logger, CancellationToken cancellationToken)
    {
        try
        {
            if (dbContext.Instructors is null || dbContext.Instructors.Any()) return;

            var json = TryLoadJsonFile("instructor.json");
            if (json is null) return;

            var instr = JsonConvert.DeserializeObject<List<Instructor>>(json);
            if (instr?.Count > 0)
            {
                foreach (var i in instr)
                {
                    i.Courses ??= new List<Course>();
                    i.CourseInstructors ??= new List<CourseInstructor>();
                }

                dbContext.Instructors!.AddRange(instr);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to seed instructors");
        }
    }

    public static async Task SeedCoursesAsync(MasterNetDbContext dbContext, ILogger? logger, CancellationToken cancellationToken)
    {
        try
        {
            if (dbContext.Courses is null || dbContext.Courses.Any()) return;

            var json = TryLoadJsonFile("course.json");
            if (json is null) return;

            var jArr = JArray.Parse(json);
            var coursesToAdd = new List<Course>();

            foreach (var token in jArr)
            {
                try
                {
                    var idStr = token["Id"]?.ToString();
                    if (!Guid.TryParse(idStr, out var id)) id = Guid.NewGuid();

                    var title = token["Title"]?.ToString();
                    var desc = token["Description"]?.ToString();

                    DateTime? published = null;
                    var publishedStr = token["PublishedAt"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(publishedStr) && DateTime.TryParse(publishedStr, out var pd))
                    {
                        published = pd;
                    }

                    var course = new Course
                    {
                        Id = id,
                        Title = title,
                        Description = desc,
                        PublishedAt = published,
                        Ratings = new List<Rating>(),
                        Prices = new List<Price>(),
                        CoursePrices = new List<CoursePrice>(),
                        Instructors = new List<Instructor>(),
                        CourseInstructors = new List<CourseInstructor>(),
                        Photos = new List<Photo>()
                    };

                    coursesToAdd.Add(course);
                }
                catch (Exception ex)
                {
                    logger?.LogDebug(ex, "Skipping invalid course element");
                }
            }

            if (coursesToAdd.Count > 0)
            {
                dbContext.Courses.AddRange(coursesToAdd);
                await dbContext.SaveChangesAsync(cancellationToken);

                foreach (var token in jArr)
                {
                    try
                    {
                        var idStr = token["Id"]?.ToString();
                        if (!Guid.TryParse(idStr, out var courseId)) continue;

                        var course = dbContext.Courses!.FirstOrDefault(c => c.Id == courseId);
                        if (course is null) continue;

                        if (token["Prices"] is JArray pricesEl)
                        {
                            foreach (var pidToken in pricesEl)
                            {
                                var pidStr = pidToken?.ToString();
                                if (Guid.TryParse(pidStr, out var pid))
                                {
                                    var price = dbContext.Prices!.FirstOrDefault(p => p.Id == pid);
                                    if (price is not null)
                                    {
                                        course.Prices = course.Prices ?? new List<Price>();
                                        if (!course.Prices.Any(p => p.Id == price.Id)) course.Prices.Add(price);
                                    }
                                }
                            }
                        }

                        if (token["Instructors"] is JArray instrEl)
                        {
                            foreach (var iidToken in instrEl)
                            {
                                var iidStr = iidToken?.ToString();
                                if (Guid.TryParse(iidStr, out var iid))
                                {
                                    var instr = dbContext.Instructors!.FirstOrDefault(i => i.Id == iid);
                                    if (instr is not null)
                                    {
                                        course.Instructors = course.Instructors ?? new List<Instructor>();
                                        if (!course.Instructors.Any(i => i.Id == instr.Id)) course.Instructors.Add(instr);
                                    }
                                }
                            }
                        }

                        await dbContext.SaveChangesAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogDebug(ex, "Failed wiring course relations");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to seed courses");
        }
    }

    public static async Task SeedRatingsAsync(MasterNetDbContext dbContext, ILogger? logger, CancellationToken cancellationToken)
    {
        try
        {
            if (dbContext.Ratings is null || dbContext.Ratings.Any()) return;

            var json = TryLoadJsonFile("rating.json");
            if (json is null) return;

            var ratings = JsonConvert.DeserializeObject<List<Rating>>(json);
            if (ratings?.Count > 0)
            {
                foreach (var r in ratings)
                {
                    r.Course = null;
                }
                dbContext.Ratings!.AddRange(ratings);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to seed ratings");
        }
    }

    public static async Task SeedStudentsAsync(MasterNetDbContext dbContext, ILogger? logger, CancellationToken cancellationToken)
    {
        try
        {
            if (dbContext.Students is null || dbContext.Students.Any()) return;

            var json = TryLoadJsonFile("student.json");
            if (json is null) return;

            var jArr = JArray.Parse(json);
            var studentsToAdd = new List<Student>();

            foreach (var token in jArr)
            {
                try
                {
                    var idStr = token["Id"]?.ToString();
                    if (!Guid.TryParse(idStr, out var id)) id = Guid.CreateVersion7();

                    var student = new Student
                    {
                        Id = id,
                        FirstName = token["FirstName"]?.ToString(),
                        LastName = token["LastName"]?.ToString(),
                        Email = token["Email"]?.ToString(),
                        Courses = new List<Course>(),
                        CourseStudents = new List<CourseStudent>()
                    };

                    studentsToAdd.Add(student);
                }
                catch (Exception ex)
                {
                    logger?.LogDebug(ex, "Skipping invalid student element");
                }
            }

            if (studentsToAdd.Count > 0)
            {
                dbContext.Students.AddRange(studentsToAdd);
                await dbContext.SaveChangesAsync(cancellationToken);

                foreach (var token in jArr)
                {
                    try
                    {
                        var idStr = token["Id"]?.ToString();
                        if (!Guid.TryParse(idStr, out var studentId)) continue;

                        var student = dbContext.Students!.FirstOrDefault(s => s.Id == studentId);
                        if (student is null) continue;

                        if (token["Courses"] is JArray coursesEl)
                        {
                            foreach (var cidToken in coursesEl)
                            {
                                var cidStr = cidToken?.ToString();
                                if (Guid.TryParse(cidStr, out var cid))
                                {
                                    var course = dbContext.Courses!.FirstOrDefault(c => c.Id == cid);
                                    if (course is not null)
                                    {
                                        student.Courses = student.Courses ?? new List<Course>();
                                        if (!student.Courses.Any(c => c.Id == course.Id)) student.Courses.Add(course);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogDebug(ex, "Failed wiring student relations");
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to seed students");
        }
    }

    public static async Task SeedDevicesAsync(MasterNetDbContext dbContext, ILogger? logger, CancellationToken cancellationToken)
    {
        try
        {
            if (dbContext.Devices is null || dbContext.Devices.Any()) return;

            var json = TryLoadJsonFile("device.json");
            if (json is null) return;

            var jArr = JArray.Parse(json);
            var devicesToAdd = new List<Device>();

            foreach (var token in jArr)
            {
                try
                {
                    var idStr = token["id"]?.ToString() ?? token["Id"]?.ToString();
                    Guid id = Guid.NewGuid();
                    if (!string.IsNullOrWhiteSpace(idStr) && Guid.TryParse(idStr, out var parsedId))
                        id = parsedId;

                    var name = token["deviceName"]?.ToString() ?? token["DeviceName"]?.ToString();
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        logger?.LogDebug("Skipping device with empty name (id={IdStr})", idStr);
                        continue;
                    }

                    var device = new Device(new DeviceName(name));
                    device.Id = id;

                    devicesToAdd.Add(device);
                }
                catch (Exception ex)
                {
                    logger?.LogDebug(ex, "Skipping invalid device element");
                }
            }

            if (devicesToAdd.Count > 0)
            {
                dbContext.Devices!.AddRange(devicesToAdd);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to seed devices");
        }
    }

    private static string? TryLoadJsonFile(string fileName)
    {
        var candidate1 = Path.Combine(Directory.GetCurrentDirectory(), "src", "MasterNet.Persistence", "SeedData", fileName);
        var candidate2 = Path.Combine(Directory.GetCurrentDirectory(), "SeedData", fileName);
        var candidate3 = Path.Combine(AppContext.BaseDirectory, "SeedData", fileName);

        if (File.Exists(candidate1)) return File.ReadAllText(candidate1);
        if (File.Exists(candidate2)) return File.ReadAllText(candidate2);
        if (File.Exists(candidate3)) return File.ReadAllText(candidate3);
        return null;
    }
}