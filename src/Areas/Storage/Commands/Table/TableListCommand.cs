// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Areas.Storage.Options.Table;
using AzureMcp.Areas.Storage.Services;
using AzureMcp.Commands.Storage;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.Storage.Commands.Table;

public sealed class TableListCommand(ILogger<TableListCommand> logger) : BaseStorageCommand<TableListOptions>()
{
    private const string CommandTitle = "List Storage Tables";
    private readonly ILogger<TableListCommand> _logger = logger;

    public override string Name => "list";

    public override string Description =>
        """
        List all tables in a Storage account. This command retrieves and displays all tables available in the specified Storage account.
        Results include table names and are returned as a JSON array. You must specify an account name and subscription ID.
        Use this command to explore your Storage resources or to verify table existence before performing operations on specific tables.
        """;

    public override string Title => CommandTitle;

    [McpServerTool(Destructive = false, ReadOnly = true, Title = CommandTitle)]
    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        var options = BindOptions(parseResult);

        try
        {
            if (!Validate(parseResult.CommandResult, context.Response).IsValid)
            {
                return context.Response;
            }

            var storageService = context.GetService<IStorageService>();
            var tables = await storageService.ListTables(
                options.Account!,
                options.Subscription!,
                options.AuthMethod ?? AuthMethod.Credential,
                null,
                options.Tenant,
                options.RetryPolicy);

            context.Response.Results = tables?.Count > 0
                ? ResponseResult.Create(new TableListCommandResult(tables), StorageJsonContext.Default.TableListCommandResult)
                : null;

            // Only show warning if we actually had to fall back to a different auth method
            if (context.Response.Results is not null && !string.IsNullOrEmpty(context.Response.Message))
            {
                var authMethod = options.AuthMethod ?? AuthMethod.Credential;
                context.Response.Message = authMethod switch
                {
                    AuthMethod.Credential when context.Response.Message.Contains("connection string") =>
                        "Warning: Credential and key auth failed, succeeded using connection string. " +
                        "Consider using --auth-method connectionString for future calls.",
                    AuthMethod.Key when context.Response.Message.Contains("connection string") =>
                        "Warning: Key auth failed, succeeded using connection string. " +
                        "Consider using --auth-method connectionString for future calls.",
                    _ => string.Empty // Clear any warning message if auth succeeded directly
                };
            }
            else
            {
                context.Response.Message = string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing tables. Account: {Account}.", options.Account);
            HandleException(context.Response, ex);
        }

        return context.Response;
    }

    internal record TableListCommandResult(List<string> Tables);
}
