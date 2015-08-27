/*
 * Markdown Scanner
 * Copyright (c) Microsoft Corporation
 * All rights reserved. 
 * 
 * MIT License
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of 
 * this software and associated documentation files (the ""Software""), to deal in 
 * the Software without restriction, including without limitation the rights to use, 
 * copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
 * Software, and to permit persons to whom the Software is furnished to do so, 
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all 
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace ApiDocs.ConsoleApp
{
    using System;
    using System.IO;

    public class FancyConsole
    {
        public const ConsoleColor ConsoleDefaultColor = ConsoleColor.White;
        public const ConsoleColor ConsoleHeaderColor = ConsoleColor.Cyan;
        public const ConsoleColor ConsoleSubheaderColor = ConsoleColor.DarkCyan;
        public const ConsoleColor ConsoleCodeColor = ConsoleColor.Gray;
        public const ConsoleColor ConsoleErrorColor = ConsoleColor.Red;
        public const ConsoleColor ConsoleWarningColor = ConsoleColor.Yellow;
        public const ConsoleColor ConsoleSuccessColor = ConsoleColor.Green;

        
        private static string logFileName;
        private static StreamWriter logWriter;

        public static bool WriteVerboseOutput { get; set; }

        public static string LogFileName 
        {
            get { return logFileName; }
            set
            {
                logFileName = value;
                logWriter = !string.IsNullOrEmpty(logFileName) ? new StreamWriter(logFileName, false) { AutoFlush = true } : null;
            }
        }

        public static void Write(string output)
        {
            if (null != logWriter) logWriter.Write(output);
            Console.Write(output);
        }

        public static void Write(ConsoleColor color, string output)
        {
            if (null != logWriter) logWriter.Write(output);

            Console.ForegroundColor = color;
            Console.Write(output);
            Console.ResetColor();
        }

        public static void Write(string format, params object[] values)
        {
            if (null != logWriter) logWriter.Write(format, values);
            Console.Write(format, values);
        }

        public static void Write(ConsoleColor color, string format, params object[] values)
        {
            if (null != logWriter) logWriter.Write(format, values);

            Console.ForegroundColor = color;
            Console.Write(format, values);
            Console.ResetColor();
        }

        public static void WriteLine()
        {
            if (null != logWriter) logWriter.WriteLine();
            Console.WriteLine();
        }

        public static void WriteLine(string format, params object[] values)
        {
            if (null != logWriter) logWriter.WriteLine(format, values);
            Console.WriteLine(format, values);
        }

        public static void WriteLine(string output)
        {
            if (null != logWriter) logWriter.WriteLine(output);
            Console.WriteLine(output);
        }

        public static void WriteLine(ConsoleColor color, string format, params object[] values)
        {
            if (null != logWriter) logWriter.WriteLine(format, values);

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
            using (StringReader reader = new StringReader(string.Format(format, values)))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    WriteLine(string.Concat(indentString, line));
                }
            }
        }

        public static void WriteLineIndented(string indentString, string output)
        {
            using (StringReader reader = new StringReader(output))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    WriteLine(string.Concat(indentString, line));
                }
            }
        }

        public static void VerboseWriteLine()
        {
            if (WriteVerboseOutput)
            {
                WriteLine();
            }
        }

        public static void VerboseWriteLine(string output)
        {
            if (WriteVerboseOutput)
            {
                WriteLine(output);
            }
        }

        public static void VerboseWriteLine(ConsoleColor color, string output)
        {
            if (WriteVerboseOutput)
            {
                WriteLine(color, output);
            }
        }

        public static void VerboseWriteLine(string format, params object[] parameters)
        {
            if (WriteVerboseOutput)
            {
                WriteLine(format, parameters);
            }
        }

        public static void VerboseWriteLineIndented(string indent, string output)
        {
            if (WriteVerboseOutput)
            {
                WriteLineIndented(indent, output);
            }
        }

        public static void VerboseWriteLineIndented(string indent, string format, params object[] parameters)
        {
            if (WriteVerboseOutput)
            {
                WriteLineIndented(indent, format, parameters);
            }
        }

    }
}
