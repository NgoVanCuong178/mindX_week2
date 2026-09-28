using System.CommandLine;
using TicketManager.Cli.Commands;
using TicketManager.Cli.Models;
using TicketManager.Cli.Services;
using TicketManager.Cli.Storage;

namespace TicketManager.Cli;

public static class CliApp
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var dataFileOption = new Option<string?>("--data-file") { Recursive = true };

        TicketService CreateService(ParseResult result)
        {
            var path = DataFilePath.Resolve(
                result.GetValue(dataFileOption),
                Environment.GetEnvironmentVariable(DataFilePath.EnvironmentVariable),
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            return new TicketService(new JsonTicketRepository(path), TimeProvider.System);
        }

        // tickets create
        var createTitle = new Option<string>("--title") { Required = true };
        var createDescription = new Option<string?>("--description");
        var createStatus = new Option<string?>("--status");
        var createPriority = new Option<string?>("--priority");
        var createTags = new Option<string[]>("--tag");
        var create = new Command("create", "Create a ticket")
        {
            createTitle, createDescription, createStatus, createPriority, createTags,
        };
        create.SetAction((Func<ParseResult, int>)(result =>
        {
            var statusText = result.GetValue(createStatus);
            var priorityText = result.GetValue(createPriority);
            var ticket = CreateService(result).Create(
                result.GetValue(createTitle),
                result.GetValue(createDescription),
                statusText is null ? TicketStatus.Open : EnumText.Parse<TicketStatus>(statusText),
                priorityText is null ? TicketPriority.Medium : EnumText.Parse<TicketPriority>(priorityText),
                result.GetValue(createTags));
            output.WriteLine($"Created ticket #{ticket.Id}");
            return 0;
        }));

        // tickets list
        var listStatus = new Option<string?>("--status");
        var listPriority = new Option<string?>("--priority");
        var listTags = new Option<string[]>("--tag");
        var list = new Command("list", "List tickets") { listStatus, listPriority, listTags };
        list.SetAction((Func<ParseResult, int>)(result =>
        {
            var statusText = result.GetValue(listStatus);
            var priorityText = result.GetValue(listPriority);
            var tickets = CreateService(result).List(
                statusText is null ? null : EnumText.Parse<TicketStatus>(statusText),
                priorityText is null ? null : EnumText.Parse<TicketPriority>(priorityText),
                result.GetValue(listTags));

            if (tickets.Count == 0)
            {
                output.WriteLine("No tickets found.");
                return 0;
            }

            output.WriteLine($"{"ID",-5}{"STATUS",-13}{"PRIORITY",-10}{"TITLE",-40}TAGS");
            foreach (var ticket in tickets)
            {
                output.WriteLine($"{ticket.Id,-5}{EnumText.Format(ticket.Status),-13}" +
                                 $"{EnumText.Format(ticket.Priority),-10}{ticket.Title,-40}" +
                                 string.Join(", ", ticket.Tags));
            }
            return 0;
        }));

        // tickets show <id>
        var showId = new Argument<int>("id");
        var show = new Command("show", "Show ticket details") { showId };
        show.SetAction((Func<ParseResult, int>)(result =>
        {
            var ticket = CreateService(result).Get(result.GetValue(showId));
            output.WriteLine($"ID:          {ticket.Id}");
            output.WriteLine($"Title:       {ticket.Title}");
            output.WriteLine($"Description: {ticket.Description}");
            output.WriteLine($"Status:      {EnumText.Format(ticket.Status)}");
            output.WriteLine($"Priority:    {EnumText.Format(ticket.Priority)}");
            output.WriteLine($"Tags:        {string.Join(", ", ticket.Tags)}");
            output.WriteLine($"Created:     {ticket.CreatedAt:u}");
            output.WriteLine($"Updated:     {ticket.UpdatedAt:u}");
            return 0;
        }));

        // tickets update <id> --status
        var updateId = new Argument<int>("id");
        var updateStatus = new Option<string>("--status") { Required = true };
        var update = new Command("update", "Update ticket status") { updateId, updateStatus };
        update.SetAction((Func<ParseResult, int>)(result =>
        {
            var status = EnumText.Parse<TicketStatus>(result.GetValue(updateStatus)!);
            var ticket = CreateService(result).UpdateStatus(result.GetValue(updateId), status);
            output.WriteLine($"Updated ticket #{ticket.Id}: status = {EnumText.Format(ticket.Status)}");
            return 0;
        }));

        var root = new RootCommand("Ticket Manager CLI") { dataFileOption, create, list, show, update };

        // Parse errors are reported here instead of through Invoke(), which would also print
        // the help text to stdout.
        var parseResult = root.Parse(args);
        if (parseResult.Errors.Count > 0)
        {
            foreach (var parseError in parseResult.Errors)
            {
                error.WriteLine(parseError.Message);
            }
            return 1;
        }

        // The only place where exceptions are turned into exit codes.
        try
        {
            return parseResult.Invoke(new InvocationConfiguration
            {
                Output = output,
                Error = error,
                EnableDefaultExceptionHandler = false,
            });
        }
        catch (ValidationException ex)
        {
            error.WriteLine(ex.Message);
            return 1;
        }
        catch (TicketNotFoundException ex)
        {
            error.WriteLine(ex.Message);
            return 1;
        }
        catch (StorageException ex)
        {
            error.WriteLine(ex.Message);
            return 2;
        }
    }
}
