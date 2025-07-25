// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine.Parsing;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzureMcp.Areas.Storage.Commands.Blob.Container;
using AzureMcp.Areas.Storage.Services;
using AzureMcp.Models.Command;
using AzureMcp.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AzureMcp.Tests.Areas.Storage.UnitTests.Blob.Container;

[Trait("Area", "Storage")]
public class ContainerListCommandTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IStorageService _storageService;
    private readonly ILogger<ContainerListCommand> _logger;
    private readonly ContainerListCommand _command;
    private readonly CommandContext _context;
    private readonly Parser _parser;
    private readonly string _knownAccountName = "account123";
    private readonly string _knownSubscriptionId = "sub123";

    public ContainerListCommandTests()
    {
        _storageService = Substitute.For<IStorageService>();
        _logger = Substitute.For<ILogger<ContainerListCommand>>();

        var collection = new ServiceCollection().AddSingleton(_storageService);

        _serviceProvider = collection.BuildServiceProvider();
        _command = new(_logger);
        _context = new(_serviceProvider);
        _parser = new(_command.GetCommand());
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsContainers()
    {
        // Arrange
        var expectedContainers = new List<string> { "container1", "container2" };

        _storageService.ListContainers(Arg.Is(_knownAccountName), Arg.Is(_knownSubscriptionId), Arg.Any<string>(),
            Arg.Any<RetryPolicyOptions>()).Returns(expectedContainers);

        var args = _parser.Parse([
            "--account-name", _knownAccountName,
            "--subscription", _knownSubscriptionId
        ]);

        // Act
        var response = await _command.ExecuteAsync(_context, args);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Results);

        var json = JsonSerializer.Serialize(response.Results);
        var result = JsonSerializer.Deserialize<ContainerListResult>(json);

        Assert.NotNull(result);
        Assert.Equal(expectedContainers, result.Containers);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsNull_WhenNoContainers()
    {
        // Arrange
        _storageService.ListContainers(Arg.Is(_knownAccountName), Arg.Is(_knownSubscriptionId), Arg.Any<string>(),
            Arg.Any<RetryPolicyOptions>()).Returns([]);

        var args = _parser.Parse([
            "--account-name", _knownAccountName,
            "--subscription", _knownSubscriptionId
        ]);

        // Act
        var response = await _command.ExecuteAsync(_context, args);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Results);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesException()
    {
        // Arrange
        var expectedError = "Test error";

        _storageService.ListContainers(Arg.Is(_knownAccountName), Arg.Is(_knownSubscriptionId), Arg.Any<string>(),
            Arg.Any<RetryPolicyOptions>()).ThrowsAsync(new Exception(expectedError));

        var args = _parser.Parse([
            "--account-name", _knownAccountName,
            "--subscription", _knownSubscriptionId
        ]);

        // Act
        var response = await _command.ExecuteAsync(_context, args);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(500, response.Status);
        Assert.StartsWith(expectedError, response.Message);
    }

    private class ContainerListResult
    {
        [JsonPropertyName("containers")]
        public List<string> Containers { get; set; } = [];
    }
}
