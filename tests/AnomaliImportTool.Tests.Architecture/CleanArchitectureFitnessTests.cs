using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace AnomaliImportTool.Tests.Architecture;

/// <summary>
/// Architecture fitness functions that validate Clean Architecture dependency rules
/// </summary>
public class CleanArchitectureFitnessTests
{
    private static readonly Assembly DomainAssembly = typeof(AnomaliImportTool.Core.Domain.Common.BaseEntity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(AnomaliImportTool.Core.Application.DependencyInjection.ServiceRegistrationAttribute).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(AnomaliImportTool.Infrastructure.Services.SampleInfrastructureService).Assembly;
    private static readonly Assembly SecurityAssembly = typeof(AnomaliImportTool.Security.DependencyInjection.SecurityServiceRegistration).Assembly;
    private static readonly Assembly DocumentProcessingAssembly = typeof(AnomaliImportTool.DocumentProcessing.DependencyInjection.DocumentProcessingServiceRegistration).Assembly;
    private static readonly Assembly ApiAssembly = typeof(AnomaliImportTool.Api.DependencyInjection.ApiServiceRegistration).Assembly;
    private static readonly Assembly GitAssembly = typeof(AnomaliImportTool.Git.DependencyInjection.GitServiceRegistration).Assembly;

    [Fact]
    public void Domain_Should_Not_HaveDependencyOnOtherProjects()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "AnomaliImportTool.Core.Application",
                "AnomaliImportTool.Infrastructure",
                "AnomaliImportTool.WPF",
                "AnomaliImportTool.Security",
                "AnomaliImportTool.DocumentProcessing",
                "AnomaliImportTool.Api",
                "AnomaliImportTool.Git")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Domain layer should not depend on any other application layers. " +
            "Violations: {0}", 
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Application_Should_Only_DependOn_Domain()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "AnomaliImportTool.Infrastructure",
                "AnomaliImportTool.WPF",
                "AnomaliImportTool.Security",
                "AnomaliImportTool.DocumentProcessing",
                "AnomaliImportTool.Api",
                "AnomaliImportTool.Git")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Application layer should only depend on Domain layer. " +
            "Violations: {0}", 
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Infrastructure_Should_DependOn_Application_And_Domain_Only()
    {
        // Arrange & Act
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "AnomaliImportTool.WPF",
                "AnomaliImportTool.Security",
                "AnomaliImportTool.DocumentProcessing",
                "AnomaliImportTool.Api",
                "AnomaliImportTool.Git")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Infrastructure layer should only depend on Application and Domain layers. " +
            "Violations: {0}", 
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Security_Should_DependOn_Application_And_Domain_Only()
    {
        // Arrange & Act
        var result = Types.InAssembly(SecurityAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "AnomaliImportTool.Infrastructure",
                "AnomaliImportTool.WPF",
                "AnomaliImportTool.DocumentProcessing",
                "AnomaliImportTool.Api",
                "AnomaliImportTool.Git")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Security layer should only depend on Application and Domain layers. " +
            "Violations: {0}", 
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void DocumentProcessing_Should_DependOn_Application_And_Domain_Only()
    {
        // Arrange & Act
        var result = Types.InAssembly(DocumentProcessingAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "AnomaliImportTool.Infrastructure",
                "AnomaliImportTool.WPF",
                "AnomaliImportTool.Security",
                "AnomaliImportTool.Api",
                "AnomaliImportTool.Git")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "DocumentProcessing layer should only depend on Application and Domain layers. " +
            "Violations: {0}", 
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Api_Should_DependOn_Application_And_Domain_Only()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApiAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "AnomaliImportTool.Infrastructure",
                "AnomaliImportTool.WPF",
                "AnomaliImportTool.Security",
                "AnomaliImportTool.DocumentProcessing",
                "AnomaliImportTool.Git")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Api layer should only depend on Application and Domain layers. " +
            "Violations: {0}", 
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Git_Should_DependOn_Application_And_Domain_Only()
    {
        // Arrange & Act
        var result = Types.InAssembly(GitAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "AnomaliImportTool.Infrastructure",
                "AnomaliImportTool.WPF",
                "AnomaliImportTool.Security",
                "AnomaliImportTool.DocumentProcessing",
                "AnomaliImportTool.Api")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Git layer should only depend on Application and Domain layers. " +
            "Violations: {0}", 
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Domain_Should_Not_Reference_System_Data()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("System.Data")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Domain layer should not have direct database dependencies. " +
            "Violations: {0}", 
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Domain_Should_Not_Reference_Entity_Framework()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "EntityFramework")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Domain layer should not reference Entity Framework. " +
            "Violations: {0}", 
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Domain_Should_Not_Reference_Infrastructure_Concerns()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "System.Net.Http",
                "System.IO",
                "Microsoft.Extensions.Logging",
                "Microsoft.Extensions.DependencyInjection")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Domain layer should not reference infrastructure concerns. " +
            "Violations: {0}", 
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Application_Should_Not_Reference_Infrastructure_Implementations()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "System.Data.SqlClient",
                "Microsoft.EntityFrameworkCore.SqlServer",
                "System.Net.Http")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Application layer should not reference specific infrastructure implementations. " +
            "Violations: {0}", 
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }
} 