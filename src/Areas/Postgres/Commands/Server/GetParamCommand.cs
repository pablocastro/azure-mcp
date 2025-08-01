// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Areas.Postgres.Options;
using AzureMcp.Areas.Postgres.Options.Server;
using AzureMcp.Areas.Postgres.Services;
using AzureMcp.Commands.Postgres;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.Postgres.Commands.Server;

public sealed class GetParamCommand(ILogger<GetParamCommand> logger) : BaseServerCommand<GetParamOptions>(logger)
{
    private const string CommandTitle = "Get PostgreSQL Server Parameter";
    private readonly Option<string> _paramOption = PostgresOptionDefinitions.Param;
    public override string Name => "param";

    public override string Description =>
        "Retrieves a specific parameter of a PostgreSQL server.";

    public override string Title => CommandTitle;

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.AddOption(_paramOption);
    }

    protected override GetParamOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.Param = parseResult.GetValueForOption(_paramOption);
        return options;
    }

    [McpServerTool(Destructive = false, ReadOnly = true, Title = CommandTitle)]
    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        try
        {
            var options = BindOptions(parseResult);

            if (!Validate(parseResult.CommandResult, context.Response).IsValid)
            {
                return context.Response;
            }

            IPostgresService pgService = context.GetService<IPostgresService>() ?? throw new InvalidOperationException("PostgreSQL service is not available.");
            var parameterValue = await pgService.GetServerParameterAsync(options.Subscription!, options.ResourceGroup!, options.User!, options.Server!, options.Param!);
            context.Response.Results = parameterValue?.Length > 0 ?
                ResponseResult.Create(
                    new GetParamCommandResult(parameterValue),
                    PostgresJsonContext.Default.GetParamCommandResult) :
                null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occurred retrieving the parameter.");
            HandleException(context.Response, ex);
        }
        return context.Response;
    }

    internal record GetParamCommandResult(string ParameterValue);
}
