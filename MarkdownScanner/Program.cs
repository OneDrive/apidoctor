using System;

namespace MarkdownScanner
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			var options = new ScannerOptions();
			if (!CommandLine.Parser.Default.ParseArguments(args, options))
			{
				Console.WriteLine(options.UsageText);
				return;
			}

			var scanner = new Scanner(options);
			scanner.Scan();
		}
	}



}
