// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Buildalyzer;
using Buildalyzer.Workspaces;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XUnitConverter
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("xunitconverter <project>");
                return;
            }

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += delegate { cts.Cancel(); };
            RunAsync(args[0], cts.Token).Wait();
        }

        private static async Task RunAsync(string projectPath, CancellationToken cancellationToken)
        {
            var converters = new ConverterBase[]
                {
                    new MSTestToXUnitConverter(),
                    new TestAssertTrueOrFalseConverter(),
                    new AssertArgumentOrderConverter(),
                };

            AnalyzerManager manager = new AnalyzerManager();
            ProjectAnalyzer analyzer = manager.GetProject(projectPath);
            Encoding s_utf8WithoutBom = new UTF8Encoding(false);
            using (var workspace = analyzer.GetWorkspace())
            {
                var originalSolution = workspace.CurrentSolution;
                var project = originalSolution.Projects.FirstOrDefault();
                foreach (var converter in converters)
                {
                    var newSolution = await converter.ProcessAsync(project, cancellationToken);
                    newSolution.GetChanges(originalSolution).GetProjectChanges().ToList().ForEach(proj =>
                    {
                        var changedDocumentIds = proj.GetChangedDocuments(true).ToList();
                        changedDocumentIds.ForEach(documentId =>
                        {
                            var document = newSolution.GetDocument(documentId);
                            var text = document.GetTextAsync();
                            using (var writer = new StreamWriter(document.FilePath, append: false, encoding: text.Result.Encoding ?? s_utf8WithoutBom))
                            {
                                text.Result.Write(writer);
                            }
                        });

                    });
                    //workspace.TryApplyChanges(project.Solution);
                }
            }
        }
    }
}
