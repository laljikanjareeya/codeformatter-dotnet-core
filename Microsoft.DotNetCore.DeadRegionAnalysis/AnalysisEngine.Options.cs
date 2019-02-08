using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.DotNetCore.DeadRegionAnalysis
{
    public partial class AnalysisEngine
    {
        internal class Options
        {
            private ImmutableArray<ImmutableDictionary<string, Tristate>> _symbolConfigurations;
            private Tristate _undefinedSymbolValue;

            public ImmutableArray<Document> Documents { get; private set; }

            public IAnalysisLogger Logger { get; set; }

            internal Options(
                IEnumerable<Project> projects = null,
                IEnumerable<string> projectPaths = null,
                IEnumerable<string> sourcePaths = null,
                IEnumerable<IEnumerable<string>> symbolConfigurations = null,
                IEnumerable<string> alwaysIgnoredSymbols = null,
                IEnumerable<string> alwaysDefinedSymbols = null,
                IEnumerable<string> alwaysDisabledSymbols = null,
                Tristate undefinedSymbolValue = default(Tristate),
                IAnalysisLogger logger = null)
            {
                if (projectPaths != null)
                {
                    List<Project> projectsToAdd = new List<Project>();
                    projectPaths.ToList().ForEach(filePath =>
                    {
                        AnalyzerManager analyzerManager = new AnalyzerManager();
                        ProjectAnalyzer analyzer = analyzerManager.GetProject(filePath);
                        projectsToAdd.AddRange(analyzerManager.GetWorkspace().CurrentSolution.Projects);
                    });
                    projects = projectsToAdd.AsEnumerable();
                }
                if (projects != null)
                {
                    Documents = GetSharedDocuments(projects);
                }

                if (projects == null && sourcePaths != null)
                {
                    var projectId = ProjectId.CreateNewId("AnalysisProject");
                    var solution = new AdhocWorkspace()
                        .CurrentSolution
                        .AddProject(projectId, "AnalysisProject", "AnalysisProject", LanguageNames.CSharp);

                    foreach (var path in sourcePaths)
                    {
                        var documentId = DocumentId.CreateNewId(projectId);
                        solution = solution.AddDocument(
                            documentId,
                            Path.GetFileName(path),
                            new FileTextLoader(path, defaultEncoding: Encoding.UTF8));
                    }

                    Documents = solution.Projects.Single().Documents.ToImmutableArray();
                }

                _symbolConfigurations = CalculateSymbolConfigurations(
                    alwaysDisabledSymbols,
                    alwaysDefinedSymbols,
                    alwaysIgnoredSymbols,
                    symbolConfigurations);

                _undefinedSymbolValue = undefinedSymbolValue;

                Logger = logger ?? new ConsoleAnalysisLogger();
            }

            internal CompositePreprocessorExpressionEvaluator GetPreprocessorExpressionEvaluator()
            {
                var evaluators = _symbolConfigurations.Select(config => new PreprocessorExpressionEvaluator(config, _undefinedSymbolValue));
                return new CompositePreprocessorExpressionEvaluator(evaluators);
            }

            internal PreprocessorSymbolTracker GetPreprocessorSymbolTracker()
            {
                var specifiedSymbols = new HashSet<string>(StringComparer.Ordinal);

                foreach (var config in _symbolConfigurations)
                {
                    foreach (string symbol in config.Keys)
                    {
                        specifiedSymbols.Add(symbol);
                    }
                }

                return new PreprocessorSymbolTracker(specifiedSymbols);
            }

            private static ImmutableArray<Document> GetSharedDocuments(IEnumerable<Project> projects)
            {
                var documents = new List<Document>();
                projects.ToList().ForEach(project =>
                {
                    if (project.Documents.Any())
                        documents.AddRange(project.Documents);
                });
                return documents.ToImmutableArray();
            }

            private static ImmutableArray<ImmutableDictionary<string, Tristate>> CalculateSymbolConfigurations(
                IEnumerable<string> alwaysDisabledSymbols,
                IEnumerable<string> alwaysDefinedSymbols,
                IEnumerable<string> alwaysIgnoredSymbols,
                IEnumerable<IEnumerable<string>> symbolConfigurations)
            {
                var explicitStates = ImmutableDictionary.CreateBuilder<string, Tristate>();

                AddExplicitSymbolStates(explicitStates, alwaysDisabledSymbols, Tristate.False);
                AddExplicitSymbolStates(explicitStates, alwaysDefinedSymbols, Tristate.True);
                AddExplicitSymbolStates(explicitStates, alwaysIgnoredSymbols, Tristate.Varying);

                if (symbolConfigurations == null || !symbolConfigurations.Any())
                {
                    return ImmutableArray.Create(explicitStates.ToImmutable());
                }

                var configurationStateMaps = ImmutableArray.CreateBuilder<ImmutableDictionary<string, Tristate>>();
                foreach (var configuration in symbolConfigurations)
                {
                    var stateMap = ImmutableDictionary.CreateBuilder<string, Tristate>();

                    foreach (var item in explicitStates)
                    {
                        stateMap.Add(item);
                    }

                    foreach (var symbol in configuration)
                    {
                        if (!stateMap.ContainsKey(symbol))
                        {
                            stateMap.Add(symbol, Tristate.True);
                        }
                    }

                    configurationStateMaps.Add(stateMap.ToImmutable());
                }

                return configurationStateMaps.ToImmutable();
            }

            private static void AddExplicitSymbolStates(ImmutableDictionary<string, Tristate>.Builder symbolStates, IEnumerable<string> symbols, Tristate explicitState)
            {
                if (symbols == null)
                {
                    return;
                }

                foreach (var symbol in symbols)
                {
                    Tristate state;
                    if (symbolStates.TryGetValue(symbol, out state))
                    {
                        if (state == explicitState)
                        {
                            throw new ArgumentException(
                                string.Format("Symbol '{0}' appears in the {1} list multiple times",
                                    symbol, GetStateString(explicitState)));
                        }
                        else
                        {
                            throw new ArgumentException(
                                string.Format("Symbol '{0}' cannot be both {1} and {2}",
                                    symbol, GetStateString(state), GetStateString(explicitState)));
                        }
                    }
                    else
                    {
                        symbolStates[symbol] = explicitState;
                    }
                }
            }

            private static string GetStateString(Tristate state)
            {
                if (state == Tristate.False)
                {
                    return "always disabled";
                }
                else if (state == Tristate.True)
                {
                    return "always enabled";
                }
                else
                {
                    return "ignore";
                }
            }
        }
    }
}
