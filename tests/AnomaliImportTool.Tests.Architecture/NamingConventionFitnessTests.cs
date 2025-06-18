using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace AnomaliImportTool.Tests.Architecture;

/// <summary>
/// Architecture fitness functions that validate naming conventions
/// </summary>
public class NamingConventionFitnessTests
{
    private static readonly Assembly DomainAssembly = typeof(AnomaliImportTool.Core.Domain.Common.BaseEntity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(AnomaliImportTool.Core.Application.DependencyInjection.ServiceRegistrationAttribute).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(AnomaliImportTool.Infrastructure.Services.SampleInfrastructureService).Assembly;
    private static readonly Assembly SecurityAssembly = typeof(AnomaliImportTool.Security.DependencyInjection.SecurityServiceRegistration).Assembly;
    private static readonly Assembly DocumentProcessingAssembly = typeof(AnomaliImportTool.DocumentProcessing.DependencyInjection.DocumentProcessingServiceRegistration).Assembly;
    private static readonly Assembly ApiAssembly = typeof(AnomaliImportTool.Api.DependencyInjection.ApiServiceRegistration).Assembly;
    private static readonly Assembly GitAssembly = typeof(AnomaliImportTool.Git.DependencyInjection.GitServiceRegistration).Assembly;

    [Fact]
    public void Interfaces_Should_Start_With_I()
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
                .AreInterfaces()
                .Should()
                .HaveNameStartingWith("I")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                "All interfaces should start with 'I' in {0}. " +
                "Violations: {1}",
                assembly.GetName().Name,
                string.Join(", ", result.FailingTypeNames ?? new List<string>()));
        }
    }

    [Fact]
    public void Services_Should_End_With_Service()
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
                .HaveNameEndingWith("Service")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                "All service classes should end with 'Service' in {0}. " +
                "Violations: {1}",
                assembly.GetName().Name,
                string.Join(", ", result.FailingTypeNames ?? new List<string>()));
        }
    }

    [Fact]
    public void Repositories_Should_End_With_Repository()
    {
        // Arrange
        var assemblies = new[] { ApplicationAssembly, InfrastructureAssembly };

        // Act & Assert
        foreach (var assembly in assemblies)
        {
            var result = Types.InAssembly(assembly)
                .That()
                .HaveNameEndingWith("Repository")
                .Should()
                .HaveNameEndingWith("Repository")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                "All repository classes should end with 'Repository' in {0}. " +
                "Violations: {1}",
                assembly.GetName().Name,
                string.Join(", ", result.FailingTypeNames ?? new List<string>()));
        }
    }

    [Fact]
    public void Domain_Entities_Should_Not_Have_Suffix()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("AnomaliImportTool.Core.Domain.Entities")
            .Should()
            .NotHaveNameEndingWith("Entity")
            .And()
            .NotHaveNameEndingWith("Model")
            .And()
            .NotHaveNameEndingWith("Dto")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Domain entities should not have Entity, Model, or Dto suffix. " +
            "Violations: {0}",
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Value_Objects_Should_Be_In_ValueObjects_Namespace()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("AnomaliImportTool.Core.Domain.ValueObjects")
            .Should()
            .BeSealed()
            .Or()
            .BeImmutable()
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Types in ValueObjects namespace should be sealed or immutable. " +
            "Violations: {0}",
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Exceptions_Should_End_With_Exception()
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
                .Inherit(typeof(Exception))
                .Should()
                .HaveNameEndingWith("Exception")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                "All exception classes should end with 'Exception' in {0}. " +
                "Violations: {1}",
                assembly.GetName().Name,
                string.Join(", ", result.FailingTypeNames ?? new List<string>()));
        }
    }

    [Fact]
    public void Commands_Should_End_With_Command()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespaceStartingWith("AnomaliImportTool.Core.Application")
            .And()
            .HaveNameEndingWith("Command")
            .Should()
            .HaveNameEndingWith("Command")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All command classes should end with 'Command'. " +
            "Violations: {0}",
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Queries_Should_End_With_Query()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespaceStartingWith("AnomaliImportTool.Core.Application")
            .And()
            .HaveNameEndingWith("Query")
            .Should()
            .HaveNameEndingWith("Query")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All query classes should end with 'Query'. " +
            "Violations: {0}",
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Handlers_Should_End_With_Handler()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespaceStartingWith("AnomaliImportTool.Core.Application")
            .And()
            .HaveNameEndingWith("Handler")
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All handler classes should end with 'Handler'. " +
            "Violations: {0}",
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Validators_Should_End_With_Validator()
    {
        // Arrange
        var assemblies = new[] { ApplicationAssembly, InfrastructureAssembly };

        // Act & Assert
        foreach (var assembly in assemblies)
        {
            var result = Types.InAssembly(assembly)
                .That()
                .HaveNameEndingWith("Validator")
                .Should()
                .HaveNameEndingWith("Validator")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                "All validator classes should end with 'Validator' in {0}. " +
                "Violations: {1}",
                assembly.GetName().Name,
                string.Join(", ", result.FailingTypeNames ?? new List<string>()));
        }
    }

    [Fact]
    public void Factories_Should_End_With_Factory()
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
            var result = Types.InAssembly(assembly)
                .That()
                .HaveNameEndingWith("Factory")
                .Should()
                .HaveNameEndingWith("Factory")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                "All factory classes should end with 'Factory' in {0}. " +
                "Violations: {1}",
                assembly.GetName().Name,
                string.Join(", ", result.FailingTypeNames ?? new List<string>()));
        }
    }
} 