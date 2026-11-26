using System.Reflection;
using NetArchTest.Rules;

namespace ShopSphere.Architecture.Tests;

/// <summary>Clean Architecture dependency rules, enforced for every service:
/// Domain depends on nothing; Application never depends on Infrastructure or Api.</summary>
public class LayerRulesTests
{
    public static IEnumerable<object[]> Services => new[] { "Ordering", "Catalog", "Inventory", "Payment", "Notification", "Identity", "Cart" }
        .Select(s => new object[] { s });

    private static Assembly Load(string service, string layer) => Assembly.Load($"ShopSphere.{service}.{layer}");

    [Theory, MemberData(nameof(Services))]
    public void Domain_does_not_depend_on_outer_layers(string service)
    {
        var result = Types.InAssembly(Load(service, "Domain")).Should()
            .NotHaveDependencyOnAny($"ShopSphere.{service}.Application", $"ShopSphere.{service}.Infrastructure", $"ShopSphere.{service}.Api",
                "Microsoft.EntityFrameworkCore", "RabbitMQ.Client", "MediatR")
            .GetResult();
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Theory, MemberData(nameof(Services))]
    public void Application_does_not_depend_on_infrastructure(string service)
    {
        var result = Types.InAssembly(Load(service, "Application")).Should()
            .NotHaveDependencyOnAny($"ShopSphere.{service}.Infrastructure", $"ShopSphere.{service}.Api", "Microsoft.EntityFrameworkCore", "RabbitMQ.Client")
            .GetResult();
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }
}
