# Open Source
The API Documentation Tool uses the following open source components:

* [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) - Json parser for .NET apps. MIT license, Copyright (c) 2007 James Newton-King.
* [CommandLineParser](https://commandline.codeplex.com/) - Command line parser library. MIT license, Copyright (c) 2005 - 2012 Giacomo Stelluti Scala.
* [mustache-sharp](https://github.com/jehugaleahsa/mustache-sharp) - An extension of the mustache text template engine for .NET. Public domain.
* [MarkdownDeep](https://github.com/toptensoftware/MarkdownDeep) - Markdown for C# parser. Apache 2.0 license, Copyright (C) 2010-2011 Topten Software.

## Markdown Deep

Markdown Deep has been modified from the original version. The modifications provide access to more of the internals
of the library, such as the Block class, to enable the tool to parse the documentation block by block.

The HTML conversion code has also been modified to enable richer HTML output from the Markdown source.

