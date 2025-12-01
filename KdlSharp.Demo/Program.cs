using KdlSharp.Demo.Examples;

Console.WriteLine("╔═══════════════════════════════════════╗");
Console.WriteLine("║       KdlSharp Demo Application       ║");
Console.WriteLine("╚═══════════════════════════════════════╝");
Console.WriteLine();

// Run all examples
BasicParsing.Run();
Console.WriteLine(new string('─', 50));
Console.WriteLine();

Serialization.Run();
Console.WriteLine(new string('─', 50));
Console.WriteLine();

ErrorHandling.Run();
Console.WriteLine(new string('─', 50));
Console.WriteLine();

AdvancedUsage.Run();
Console.WriteLine(new string('─', 50));
Console.WriteLine();

SchemaValidation.Run();
Console.WriteLine(new string('─', 50));
Console.WriteLine();

Queries.Run();
Console.WriteLine(new string('─', 50));
Console.WriteLine();

StreamingReaderDemo.Run();
Console.WriteLine(new string('─', 50));
Console.WriteLine();

await AsyncOperations.RunAsync();
Console.WriteLine(new string('─', 50));
Console.WriteLine();

FileOperations.Run();
Console.WriteLine(new string('─', 50));
Console.WriteLine();

ExtensionMethods.Run();
Console.WriteLine(new string('─', 50));
Console.WriteLine();

Console.WriteLine("Demo completed!");
