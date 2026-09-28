using System.Text;
using TicketManager.Cli;

// The Windows console does not use UTF-8 by default, which breaks Vietnamese output.
Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

return CliApp.Run(args, Console.Out, Console.Error);
