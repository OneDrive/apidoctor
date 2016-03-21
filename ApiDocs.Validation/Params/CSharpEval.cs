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

namespace ApiDocs.Validation.Params
{
    using System;
    using System.Text;
    using System.Reflection;
    using Microsoft.CSharp;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    internal static class CSharpEval
    {
        public static string Evaluate(string code, IReadOnlyDictionary<string, string> values)
        {
            if (!code.EndsWith(";"))
            {
                code += ";";
            }

            string codeWrapper = 
            "namespace MarkdownScanner.Eval { " +
                "using System; " +
                "using System.Collections.Generic; " +
                "public class MethodEval {" +
                    "public static object RunCode(IReadOnlyDictionary<string, string> values) {" +
                        "return " + code +
                    "}" +
                "}" +
            "}";

            Assembly asm = GenerateAssembly(codeWrapper);
            
            Module mod = asm.GetModules()[0];
            if (null == mod)
            {
                throw new Exception("Unable to locate dynamic code module.");
            }

            Type t = mod.GetType("MarkdownScanner.Eval.MethodEval");
            if (null == t)
            {
                throw new Exception("Unable to locate MethodEval type");
            }
                
            var methodInfo = t.GetMethod("RunCode");
            object result = methodInfo.Invoke(null, new object[] { values });

            if (result is DateTimeOffset)
            {
                return ((DateTimeOffset)result).ToUniversalTime().ToString(@"yyyy-MM-ddTHH\:mm\:ssZ");
            }
            else
            {
                return result.ToString();
            }
        }

        private static Assembly GenerateAssembly(string code)
        {
            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerParameters compilerparams = new CompilerParameters();
            compilerparams.GenerateExecutable = false;
            compilerparams.GenerateInMemory = true;

            var results =
               provider.CompileAssemblyFromSource(compilerparams, code);

            if (results.Errors.HasErrors)
            {
                StringBuilder errors = new StringBuilder("Compiler Errors :\r\n");
                foreach (CompilerError error in results.Errors)
                {
                    errors.AppendFormat("Line {0},{1}\t: {2}\n",
                           error.Line, error.Column, error.ErrorText);
                }
                throw new Exception(errors.ToString());
            }
            else
            {
                return results.CompiledAssembly;
            }
        }
    }
}
