using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Win32;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

var app = new RegistryClientApp();
return await app.RunAsync(args);

internal sealed class RegistryClientApp
{
	public async Task<int> RunAsync(string[] args)
	{
		var options = ClientOptions.Parse(args);
		using var cts = new CancellationTokenSource(options.Timeout);

		Console.CancelKeyPress += (_, eventArgs) =>
		{
			eventArgs.Cancel = true;
			if (!cts.IsCancellationRequested)
			{
				Console.WriteLine("Cancellation requested. Stopping...");
				cts.Cancel();
			}
		};

		try
		{
			// read odr.exe name from registry:
			string? odrPath = Registry.GetValue(
				@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Mcp",
				"Command", null) as string;

			string? Args = Registry.GetValue(
				@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Mcp",
				"Args", null) as string;

			if (string.IsNullOrWhiteSpace(odrPath) || string.IsNullOrWhiteSpace(Args))
			{
				throw new ArgumentException("This version of Windows doesn't support On-Device Registry");
			}
            
			var transportOptions = new StdioClientTransportOptions
			{
				Command = odrPath,
				Arguments = Args.Split(),
			};

			var transport = new StdioClientTransport(transportOptions);
			await using var client = await McpClient.CreateAsync(transport, cancellationToken: cts.Token);

			Console.WriteLine($"Connected to {client.ServerInfo.Name} v{client.ServerInfo.Version} via {options.CommandSummary}.");

			var callResult = await client.CallToolAsync(
				toolName: "list_mcp_servers",
				cancellationToken: cts.Token);

			if (callResult.IsError == true)
			{
				var details = ContentHelpers.ExtractText(callResult) ?? "Tool call returned an error with no details.";
				Console.Error.WriteLine($"list_mcp_servers returned an error: {details}");
				return 2;
			}

			var registryNode = ContentHelpers.ExtractRegistryJson(callResult);
			if (registryNode is null)
			{
				Console.Error.WriteLine("list_mcp_servers did not include structured JSON output.");
				return 3;
			}

			var servers = RegistryParser.ParseServers(registryNode);
			if (servers.Count == 0)
			{
				Console.WriteLine("No MCP servers were returned by odr.exe.");
				return 0;
			}

			Console.WriteLine();
			TableRenderer.Render(servers);
			Console.WriteLine();
			Console.WriteLine($"Found {servers.Count} MCP server(s).");
			return 0;
		}
		catch (OperationCanceledException)
		{
			Console.Error.WriteLine("Operation cancelled.");
			return -1;
		}
		catch (Win32Exception ex)
		{
			Console.Error.WriteLine($"Unable to start '{options.CommandSummary}': {ex.Message}");
			return 4;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Unexpected error: {ex.Message}");
			Console.Error.WriteLine(ex);
			return 5;
		}
	}
}

internal sealed record ClientOptions(string Command, IReadOnlyList<string> CommandArguments, string? WorkingDirectory, TimeSpan Timeout)
{
	private const string DefaultCommand = "odr.exe";
	private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

	public string CommandSummary => CommandArguments.Count > 0
		? $"{Command} {string.Join(' ', CommandArguments)}"
		: Command;

	public static ClientOptions Parse(string[] args)
	{
		var command = Environment.GetEnvironmentVariable("ODR_PATH") ?? DefaultCommand;
		var workingDirectory = Environment.GetEnvironmentVariable("ODR_WORKING_DIR");
		var timeout = DefaultTimeout;
		var forwardedArgs = new List<string>();

		var shouldForwardRemaining = false;
		for (var i = 0; i < args.Length; i++)
		{
			var token = args[i];
			if (shouldForwardRemaining)
			{
				forwardedArgs.Add(token);
				continue;
			}

			switch (token)
			{
				case "--odr" or "-c":
					i++;
					EnsureArgument(args, i, token);
					command = args[i];
					break;
				case "--working-dir" or "-w":
					i++;
					EnsureArgument(args, i, token);
					workingDirectory = args[i];
					break;
				case "--timeout" or "-t":
					i++;
					EnsureArgument(args, i, token);
					if (!int.TryParse(args[i], out var seconds) || seconds <= 0)
					{
						throw new ArgumentException("--timeout must be a positive integer number of seconds.");
					}
					timeout = TimeSpan.FromSeconds(seconds);
					break;
				case "--":
					shouldForwardRemaining = true;
					break;
				default:
					// Unrecognized token before the separator: treat it as part of the odr command.
					forwardedArgs.Add(token);
					break;
			}
		}

		if (!string.IsNullOrWhiteSpace(workingDirectory) && !Directory.Exists(workingDirectory))
		{
			throw new DirectoryNotFoundException($"Working directory '{workingDirectory}' does not exist.");
		}

		return new ClientOptions(command, forwardedArgs, workingDirectory, timeout);
	}

	private static void EnsureArgument(string[] args, int index, string option)
	{
		if (index >= args.Length)
		{
			throw new ArgumentException($"Missing value for {option} option.");
		}
	}
}

internal static class ContentHelpers
{
	public static string? ExtractText(CallToolResult result)
	{
		if (result.Content is null)
		{
			return null;
		}

		foreach (var block in result.Content)
		{
			if (block is TextContentBlock textBlock && !string.IsNullOrWhiteSpace(textBlock.Text))
			{
				return textBlock.Text.Trim();
			}
		}

		return null;
	}

	public static JsonNode? ExtractRegistryJson(CallToolResult result)
	{
		if (result.StructuredContent is not null)
		{
			return result.StructuredContent;
		}

		var text = ExtractText(result);
		if (string.IsNullOrWhiteSpace(text) || !LooksLikeJson(text))
		{
			return null;
		}

		try
		{
			return JsonNode.Parse(text);
		}
		catch (JsonException)
		{
			return null;
		}
	}

	private static bool LooksLikeJson(string value)
	{
		value = value.TrimStart();
		return value.StartsWith('{') || value.StartsWith('[');
	}
}

internal static class RegistryParser
{
	public static IReadOnlyList<RegisteredServer> ParseServers(JsonNode? root)
	{
		if (root is null)
		{
			return Array.Empty<RegisteredServer>();
		}

		var nodes = EnumerateServerNodes(root).ToList();
		var servers = new List<RegisteredServer>(nodes.Count);
		foreach (var node in nodes)
		{
			if (node is null)
			{
				continue;
			}

			var record = CreateServer(node);
			if (record is not null)
			{
				servers.Add(record);
			}
		}

		return servers;
	}

	private static RegisteredServer? CreateServer(JsonNode node)
	{
		var name = GetString(node, "name") ?? GetString(node, "title") ?? GetString(node, "id");
		if (string.IsNullOrWhiteSpace(name))
		{
			return null;
		}

		var description = GetString(node, "description") ?? GetString(node, "summary");

		return new RegisteredServer(
			Name: name,
			Description: description);
	}

	private static string DescribeTransport(JsonNode node)
	{
		if (node is JsonObject obj)
		{
			if (obj.TryGetPropertyValue("transport", out var transportNode))
			{
				return DescribeTransportNode(transportNode);
			}

			if (obj.TryGetPropertyValue("transports", out var transportsNode))
			{
				return DescribeTransportNode(transportsNode);
			}
		}

		return "(not specified)";
	}

	private static string DescribeTransportNode(JsonNode? node)
	{
		switch (node)
		{
			case null:
				return "(not specified)";
			case JsonValue value when value.TryGetValue<string>(out var single):
				return single;
			case JsonArray array:
				return string.Join(", ", array.Select(DescribeTransportNode));
			case JsonObject obj:
				var type = GetString(obj, "type") ?? GetString(obj, "protocol") ?? "unknown";
				var transportName = type;
				var command = GetString(obj, "command") ?? GetString(obj, "exe");
				var args = ExtractStringArray(obj, "args", "arguments");
				var url = GetString(obj, "url") ?? GetString(obj, "endpoint");

				if (!string.IsNullOrWhiteSpace(command))
				{
					transportName += $" ({command}{FormatArgs(args)})";
				}
				else if (!string.IsNullOrWhiteSpace(url))
				{
					transportName += $" ({url})";
				}

				return transportName;
			default:
				return node.ToJsonString();
		}

		static string FormatArgs(IReadOnlyList<string> args) => args.Count == 0
			? string.Empty
			: " " + string.Join(' ', args);
	}

	private static IEnumerable<JsonNode?> EnumerateServerNodes(JsonNode node)
	{
		switch (node)
		{
			case JsonArray array:
				foreach (var child in array)
				{
					if (child is not null)
					{
						yield return child;
					}
				}
				yield break;
			case JsonObject obj:
				if (obj.TryGetPropertyValue("servers", out var value) && value is not null)
				{
					foreach (var child in EnumerateServerNodes(value))
					{
						yield return child;
					}
				}
				

				break;
		}
	}

	private static IReadOnlyList<string> ExtractStringArray(JsonNode node, params string[] propertyNames)
	{
		foreach (var property in propertyNames)
		{
			var value = GetNode(node, property);
			if (value is JsonArray array)
			{
				var strings = array
					.Select(item => item?.GetValue<string?>())
					.Where(s => !string.IsNullOrWhiteSpace(s))
					.Select(s => s!.Trim())
					.ToList();

				if (strings.Count > 0)
				{
					return strings;
				}
			}
		}

		return Array.Empty<string>();
	}

	private static JsonNode? GetNode(JsonNode node, string property)
	{
		return node is JsonObject obj && obj.TryGetPropertyValue(property, out var value)
			? value
			: null;
	}

	private static string? GetString(JsonNode node, string property)
	{
		var value = GetNode(node, property);
		return value?.GetValue<string?>()?.Trim();
	}
}

internal sealed record RegisteredServer(string Name, string? Description);

internal static class TableRenderer
{
	private const int MaxSummaryLength = 80;

	public static void Render(IReadOnlyList<RegisteredServer> servers)
	{
		var rows = servers
			.Select(server => new[]
			{
				server.Name,
				Trim(server.Description, MaxSummaryLength),
			})
			.ToList();

		var headers = new[] { "Name", "Description" };
		var widths = CalculateWidths(headers, rows);

		PrintSeparator(widths);
		PrintRow(headers, widths);
		PrintSeparator(widths);
		foreach (var row in rows)
		{
			PrintRow(row, widths);
		}
		PrintSeparator(widths);
	}

	private static int[] CalculateWidths(string[] headers, List<string[]> rows)
	{
		var widths = headers.Select(h => h.Length).ToArray();
		for (var i = 0; i < widths.Length; i++)
		{
			foreach (var row in rows)
			{
				foreach (var line in row[i].Split('\n'))
				{
					widths[i] = Math.Max(widths[i], line.Length);
				}
			}
		}

		return widths;
	}

	private static void PrintRow(IReadOnlyList<string> values, IReadOnlyList<int> widths)
	{
		var wrappedColumns = values
			.Select((value, index) => WrapLines(value, widths[index]))
			.ToList();

		var maxLines = wrappedColumns.Max(column => column.Count);
		for (var lineIndex = 0; lineIndex < maxLines; lineIndex++)
		{
			Console.Write("| ");
			for (var columnIndex = 0; columnIndex < wrappedColumns.Count; columnIndex++)
			{
				var column = wrappedColumns[columnIndex];
				var text = lineIndex < column.Count ? column[lineIndex] : string.Empty;
				Console.Write(text.PadRight(widths[columnIndex]));
				Console.Write(" | ");
			}
			Console.WriteLine();
		}
	}

	private static List<string> WrapLines(string text, int width)
	{
		var lines = new List<string>();
		foreach (var rawLine in text.Split('\n'))
		{
			var line = rawLine;
			while (line.Length > width)
			{
				lines.Add(line[..width]);
				line = line[width..];
			}
			lines.Add(line);
		}

		return lines;
	}

	private static void PrintSeparator(IReadOnlyList<int> widths)
	{
		Console.Write("+-");
		foreach (var width in widths)
		{
			Console.Write(new string('-', width));
			Console.Write("-+-");
		}
		Console.WriteLine();
	}

	private static string Trim(string? value, int max)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "-";
		}

		var trimmed = value.Trim();
		return trimmed.Length <= max ? trimmed : trimmed[..max] + "…";
	}
}
