using MasterNet.Domain.Security;
using Microsoft.AspNetCore.Authorization;

namespace MasterNet.WebApi.Extensions;

public static class PoliciesConfiguration
{
    private static readonly string[] AllPolicies =
    [
        PolicyMaster.COURSE_READ,
        PolicyMaster.COURSE_WRITE,
        PolicyMaster.COURSE_UPDATE,
        PolicyMaster.COURSE_DELETE,
        PolicyMaster.INSTRUCTOR_CREATE,
        PolicyMaster.INSTRUCTOR_UPDATE,
        PolicyMaster.INSTRUCTOR_READ,
        PolicyMaster.RATING_READ,
        PolicyMaster.RATING_CREATE,
        PolicyMaster.RATING_DELETE,
        PolicyMaster.PRICE_CREATE
    ];

    public static IServiceCollection AddPoliciesServices(
        this IServiceCollection services
    )
    {
        services.AddAuthorization(opt =>
        {
            foreach (var policyName in AllPolicies)
            {
                AddClaimPolicy(opt, policyName);
            }
        });

        return services;
    }

    // Cada política es el mismo criterio: exigir el claim POLICIES con el valor de la política.
    private static void AddClaimPolicy(AuthorizationOptions opt, string policyName) =>
        opt.AddPolicy(policyName, policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => c.Type == CustomClaims.POLICIES && c.Value == policyName)));
}
