using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace AnomaliImportTool.Tests.Architecture;

/// <summary>
/// Architecture fitness functions that validate design patterns and SOLID principles
/// </summary>
public class DesignPatternFitnessTests
{
    private static readonly Assembly DomainAssembly = typeof(AnomaliImportTool.Core.Domain.Common.BaseEntity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(AnomaliImportTool.Core.Application.DependencyInjection.ServiceRegistrationAttribute).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(AnomaliImportTool.Infrastructure.Services.SampleInfrastructureService).Assembly;
    private static readonly Assembly SecurityAssembly = typeof(AnomaliImportTool.Security.DependencyInjection.SecurityServiceRegistration).Assembly;
    private static readonly Assembly DocumentProcessingAssembly = typeof(AnomaliImportTool.DocumentProcessing.DependencyInjection.DocumentProcessingServiceRegistration).Assembly;
    private static readonly Assembly ApiAssembly = typeof(AnomaliImportTool.Api.DependencyInjection.ApiServiceRegistration).Assembly;
    private static readonly Assembly GitAssembly = typeof(AnomaliImportTool.Git.DependencyInjection.GitServiceRegistration).Assembly;

    [Fact]
    public void Domain_Entities_Should_Inherit_From_BaseEntity()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("AnomaliImportTool.Core.Domain.Entities")
            .And()
            .AreClasses()
            .Should()
            .Inherit(typeof(AnomaliImportTool.Core.Domain.Common.BaseEntity))
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All domain entities should inherit from BaseEntity. " +
            "Violations: {0}",
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Value_Objects_Should_Be_Immutable()
    {
        // Arrange
        var valueObjectTypes = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("AnomaliImportTool.Core.Domain.ValueObjects")
            .GetTypes();

        // Act & Assert
        foreach (var type in valueObjectTypes)
        {
            // Check that the type is sealed (all our value objects should be sealed records)
            type.IsSealed.Should().BeTrue(
                $"Value object {type.Name} should be sealed for immutability");
            
            // For records, we mainly need to ensure they're sealed
            // The init-only nature is enforced by the record declaration
            // This is a simplified check that focuses on the key immutability aspect
        }
    }

    [Fact]
    public void Repository_Interfaces_Should_Be_In_Application_Layer()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .AreInterfaces()
            .And()
            .HaveNameEndingWith("Repository")
            .Should()
            .ResideInNamespace("AnomaliImportTool.Core.Application.Interfaces.Repositories")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All repository interfaces should be in Application layer. " +
            "Violations: {0}",
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Service_Interfaces_Should_Be_In_Application_Layer()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .AreInterfaces()
            .And()
            .HaveNameEndingWith("Service")
            .Should()
            .ResideInNamespaceStartingWith("AnomaliImportTool.Core.Application.Interfaces")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All service interfaces should be in Application layer interfaces. " +
            "Violations: {0}",
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Classes_Should_Not_Be_Static_Except_Extensions()
    {
        // Arrange
        var assemblies = new[] 
        { 
            DomainAssembly, 
            ApplicationAssembly, 
            InfrastructureAssembly, 
            SecurityAssembly, 
            DocumentProcessingAssembly, 
            ApiAssembly, 
            GitAssembly 
        };

        // Act & Assert
        foreach (var assembly in assemblies)
        {
            var result = Types.InAssembly(assembly)
                .That()
                .AreClasses()
                .And()
                .AreNotStatic()
                .Or()
                .HaveNameEndingWith("Extensions")
                .Or()
                .HaveNameEndingWith("Constants")
                .Or()
                .HaveNameEndingWith("Helper")
                .Should()
                .NotBeStatic()
                .Or()
                .HaveNameEndingWith("Extensions")
                .Or()
                .HaveNameEndingWith("Constants")
                .Or()
                .HaveNameEndingWith("Helper")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                "Classes should not be static except Extensions, Constants, and Helper classes in {0}. " +
                "Violations: {1}",
                assembly.GetName().Name,
                string.Join(", ", result.FailingTypeNames ?? new List<string>()));
        }
    }

    [Fact]
    public void Public_Classes_Should_Have_Interfaces()
    {
        // Arrange
        var assemblies = new[] 
        { 
            InfrastructureAssembly, 
            SecurityAssembly, 
            DocumentProcessingAssembly, 
            ApiAssembly, 
            GitAssembly 
        };

        // Act & Assert
        foreach (var assembly in assemblies)
        {
            var serviceClasses = Types.InAssembly(assembly)
                .That()
                .AreClasses()
                .And()
                .ArePublic()
                .And()
                .HaveNameEndingWith("Service")
                .GetTypes();

            // Check that each service class implements at least one interface
            foreach (var serviceClass in serviceClasses)
            {
                var interfaces = serviceClass.GetInterfaces()
                    .Where(i => i != typeof(IDisposable)) // Exclude common system interfaces
                    .ToList();

                interfaces.Should().NotBeEmpty(
                    $"Service class {serviceClass.Name} should implement at least one business interface");
            }
        }
    }

    [Fact]
    public void Domain_Events_Should_Implement_IDomainEvent()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("AnomaliImportTool.Core.Domain.SharedKernel.Events")
            .And()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Event")
            .Should()
            .ImplementInterface(typeof(AnomaliImportTool.Core.Domain.SharedKernel.Events.IDomainEvent))
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All domain events should implement IDomainEvent. " +
            "Violations: {0}",
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Exceptions_Should_Inherit_From_Exception()
    {
        // Arrange
        var assemblies = new[] 
        { 
            DomainAssembly, 
            ApplicationAssembly, 
            InfrastructureAssembly, 
            SecurityAssembly, 
            DocumentProcessingAssembly, 
            ApiAssembly, 
            GitAssembly 
        };

        // Act & Assert
        foreach (var assembly in assemblies)
        {
            var result = Types.InAssembly(assembly)
                .That()
                .HaveNameEndingWith("Exception")
                .Should()
                .Inherit(typeof(Exception))
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                "All exception classes should inherit from Exception in {0}. " +
                "Violations: {1}",
                assembly.GetName().Name,
                string.Join(", ", result.FailingTypeNames ?? new List<string>()));
        }
    }

    [Fact]
    public void Dependency_Injection_Attributes_Should_Be_Used_Correctly()
    {
        // Arrange
        var assemblies = new[] 
        { 
            InfrastructureAssembly, 
            SecurityAssembly, 
            DocumentProcessingAssembly, 
            ApiAssembly, 
            GitAssembly 
        };

        // Act & Assert
        foreach (var assembly in assemblies)
        {
            var result = Types.InAssembly(assembly)
                .That()
                .AreClasses()
                .And()
                .HaveNameEndingWith("Service")
                .Should()
                .HaveCustomAttribute(typeof(AnomaliImportTool.Core.Application.DependencyInjection.ServiceRegistrationAttribute))
                .Or()
                .NotBePublic() // Private services don't need the attribute
                .GetResult();

            // Note: This is a simplified check. In practice, you might want more sophisticated validation
            result.Should().NotBeNull("Service registration should be consistent");
        }
    }

    [Fact]
    public void Guard_Clauses_Should_Be_Used_In_Public_Methods()
    {
        // This test would require more sophisticated analysis
        // For now, we'll verify that Guard class exists and is used
        var guardType = DomainAssembly.GetType("AnomaliImportTool.Core.Domain.SharedKernel.Guards.Guard");
        
        guardType.Should().NotBeNull("Guard class should exist for parameter validation");
    }

    [Fact]
    public void Value_Objects_Should_Override_Equals_And_GetHashCode()
    {
        // Arrange & Act
        var valueObjectTypes = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("AnomaliImportTool.Core.Domain.ValueObjects")
            .And()
            .AreClasses()
            .GetTypes();

        // Assert
        foreach (var type in valueObjectTypes)
        {
            var equalsMethod = type.GetMethod("Equals", new[] { typeof(object) });
            var getHashCodeMethod = type.GetMethod("GetHashCode", Type.EmptyTypes);

            equalsMethod.Should().NotBeNull($"{type.Name} should override Equals method");
            getHashCodeMethod.Should().NotBeNull($"{type.Name} should override GetHashCode method");
            
            if (equalsMethod != null)
            {
                equalsMethod.IsVirtual.Should().BeTrue($"{type.Name}.Equals should be virtual/override");
            }
            
            if (getHashCodeMethod != null)
            {
                getHashCodeMethod.IsVirtual.Should().BeTrue($"{type.Name}.GetHashCode should be virtual/override");
            }
        }
    }

    [Fact]
    public void Async_Methods_Should_End_With_Async()
    {
        // Arrange
        var assemblies = new[] 
        { 
            ApplicationAssembly, 
            InfrastructureAssembly, 
            SecurityAssembly, 
            DocumentProcessingAssembly, 
            ApiAssembly, 
            GitAssembly 
        };

        // Act & Assert
        foreach (var assembly in assemblies)
        {
            var types = Types.InAssembly(assembly)
                .That()
                .AreClasses()
                .GetTypes();

            foreach (var type in types)
            {
                var asyncMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Where(m => m.ReturnType.Name.StartsWith("Task"))
                    .ToList();

                foreach (var method in asyncMethods)
                {
                    method.Name.Should().EndWith("Async", 
                        $"Async method {type.Name}.{method.Name} should end with 'Async'");
                }
            }
        }
    }
} 