using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.ConsoleApp
{
    public class FancyConsole
    {

        public static bool WriteVerboseOutput { get; set; }

        public static void Write(string output)
        {
            Console.Write(output);
        }

        public static void Write(ConsoleColor color, string output)
        {
            Console.ForegroundColor = color;
            Console.Write(output);
            Console.ResetColor();
        }

        public static void Write(ConsoleColor color, string format, params object[] values)
        {
            Console.ForegroundColor = color;
            Console.Write(format, values);
            Console.ResetColor();
        }

        public static void WriteLine()
        {
            Console.WriteLine();
        }

        public static void WriteLine(string format, params object[] values)
        {
            Console.WriteLine(format, values);
        }

        public static void WriteLine(ConsoleColor color, string format, params object[] values)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(format, values);
            Console.ResetColor();
        }

        public static void WriteLineIndented(string indentString, ConsoleColor color, string output)
        {
            Console.ForegroundColor = color;
            WriteLineIndented(indentString, output);
            Console.ResetColor();
        }

        public static void WriteLineIndented(string indentString, ConsoleColor color, string format, params object[] values)
        {
            Console.ForegroundColor = color;
            WriteLineIndented(indentString, format, values);
            Console.ResetColor();
        }

        public static void WriteLineIndented(string indentString, string format, params object[] values)
        {
            using (System.IO.StringReader reader = new System.IO.StringReader(string.Format(format, values)))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Console.WriteLine(string.Concat(indentString, line));
                }
            }
        }

        public static void WriteLineIndented(string indentString, string output)
        {
            using (System.IO.StringReader reader = new System.IO.StringReader(output))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Console.WriteLine(string.Concat(indentString, line));
                }
            }
        }


        public static void VerboseWriteLine(string output)
        {
            if (WriteVerboseOutput)
            {
                FancyConsole.WriteLine(output);
            }
        }

        public static void VerboseWriteLine(string format, params object[] parameters)
        {
            if (WriteVerboseOutput)
            {
                FancyConsole.WriteLine(format, parameters);
            }
        }

        public static void VerboseWriteLineIndented(string indent, string format, params object[] parameters)
        {
            if (WriteVerboseOutput)
            {
                FancyConsole.WriteLineIndented(indent, format, parameters);
            }
        }

    }
}
