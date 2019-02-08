using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNetCore.CodeFormatting
{
    [Export(typeof(IFormattingEngine))]
    internal sealed class FormattingEngineImplementation : IFormattingEngine
    {
        /// <summary>
        /// Developers who want to opt out of the code formatter for items like unicode
        /// tables can surround them with #if !DOTNET_FORMATTER.  
        /// </summary>
        internal const string TablePreprocessorSymbolName = "DOTNET_FORMATTER";

        private readonly IEnumerable<IFormattingFilter> _filters;

        private readonly IEnumerable<Lazy<ISyntaxFormattingRule, RuleMetadata>> _syntaxRules;
        private readonly IEnumerable<Lazy<ILocalSemanticFormattingRule, RuleMetadata>> _localSemanticRules;
        private readonly IEnumerable<Lazy<IGlobalSemanticFormattingRule, RuleMetadata>> _globalSemanticRules;
        private readonly Stopwatch _watch = new Stopwatch();
        private readonly Dictionary<string, bool> _ruleMap = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private bool _allowTables;
        private bool _verbose;

        public ImmutableArray<string> CopyrightHeader
        {
            get { return Options.CopyrightHeader; }
            set { Options.CopyrightHeader = value; }
        }

        public ImmutableArray<string[]> PreprocessorConfigurations
        {
            get { return Options.PreprocessorConfigurations; }
            set { Options.PreprocessorConfigurations = value; }
        }

        public ImmutableArray<string> FileNames
        {
            get { return Options.FileNames; }
            set { Options.FileNames = value; }
        }

        public IFormatLogger FormatLogger
        {
            get { return Options.FormatLogger; }
            set { Options.FormatLogger = value; }
        }

        public bool AllowTables
        {
            get { return _allowTables; }
            set { _allowTables = value; }
        }

        public bool Verbose
        {
            get { return _verbose; }
            set { _verbose = value; }
        }

        public ImmutableArray<IRuleMetadata> AllRules
        {
            get
            {
                var list = new List<IRuleMetadata>();
                list.AddRange(_syntaxRules.Select(x => x.Metadata));
                list.AddRange(_localSemanticRules.Select(x => x.Metadata));
                list.AddRange(_globalSemanticRules.Select(x => x.Metadata));
                return list.ToImmutableArray();
            }
        }

        [ImportingConstructor]
        public FormattingEngineImplementation(
            [ImportMany] IEnumerable<IFormattingFilter> filters,
            [ImportMany] IEnumerable<Lazy<ISyntaxFormattingRule, RuleMetadata>> syntaxRules,
            [ImportMany] IEnumerable<Lazy<ILocalSemanticFormattingRule, RuleMetadata>> localSemanticRules,
            [ImportMany] IEnumerable<Lazy<IGlobalSemanticFormattingRule, RuleMetadata>> globalSemanticRules)
        {
            _filters = filters;
            _syntaxRules = syntaxRules;
            _localSemanticRules = localSemanticRules;
            _globalSemanticRules = globalSemanticRules;
            foreach (var rule in AllRules)
            {
                _ruleMap[rule.Name] = rule.DefaultRule;
            }
        }

        private IEnumerable<TRule> GetOrderedRules<TRule>(IEnumerable<Lazy<TRule, RuleMetadata>> rules)
             where TRule : IFormattingRule
        {
            return rules
                .OrderBy(r => r.Metadata.Order)
                .Where(r => _ruleMap[r.Metadata.Name])
                .Select(r => r.Value)
                .ToList();
        }

        public Task FormatSolutionAsync(Solution solution, CancellationToken cancellationToken)
        {
            var documentIds = solution.Projects.SelectMany(x => x.DocumentIds).ToList();
            return FormatAsync(solution.Workspace, documentIds, cancellationToken);
        }

        public Task FormatProjectAsync(Project project, CancellationToken cancellationToken)
        {
            return FormatAsync(project.Solution.Workspace, project.DocumentIds, cancellationToken);
        }

        public void ToggleRuleEnabled(IRuleMetadata ruleMetaData, bool enabled)
        {
            _ruleMap[ruleMetaData.Name] = enabled;
        }

        private async Task FormatAsync(Workspace workspace, IReadOnlyList<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            try
            {
                var watch = new Stopwatch();
                watch.Start();
                var originalSolution = workspace.CurrentSolution;
                var newSolution = await FormatCoreAsync(originalSolution, documentIds, cancellationToken);
                newSolution.GetChanges(originalSolution).GetProjectChanges().ToList().ForEach(projectChanges =>
                {
                    //NOTE: As TryApplyChanges not working save document to disk from new solution
                    SaveUpdatedDocumentsToDisk(projectChanges, newSolution);
                });
                watch.Stop();
                FormatLogger.WriteLine("Total time {0}", watch.Elapsed);
            }
            catch (Exception)
            {
                FormatLogger.WriteErrorLine("Unable to save changes to disk");
            }

        }

        /// <summary>
        /// Saves the updated documents to disk.
        /// </summary>
        /// <param name="projectChanges">The project changes.</param>
        /// <param name="newSolution">The new solution.</param>
        private static void SaveUpdatedDocumentsToDisk(ProjectChanges projectChanges, Solution newSolution)
        {
            var s_utf8WithoutBom = new UTF8Encoding(false);
            var changedDocumentIds = projectChanges.GetChangedDocuments(true).ToList();
            changedDocumentIds.ForEach(documentId =>
            {
                var document = newSolution.GetDocument(documentId);
                var text = document.GetTextAsync();
                using (var writer = new StreamWriter(document.FilePath, append: false, encoding: text.Result.Encoding ?? s_utf8WithoutBom))
                {
                    text.Result.Write(writer);
                }
            });
        }

        private Solution AddTablePreprocessorSymbol(Solution solution)
        {
            var projectIds = solution.ProjectIds;
            foreach (var projectId in projectIds)
            {
                var project = solution.GetProject(projectId);
                var parseOptions = project.ParseOptions as CSharpParseOptions;
                if (parseOptions != null)
                {
                    var list = new List<string>();
                    list.AddRange(parseOptions.PreprocessorSymbolNames);
                    list.Add(TablePreprocessorSymbolName);
                    parseOptions = parseOptions.WithPreprocessorSymbols(list);
                    solution = project.WithParseOptions(parseOptions).Solution;
                }
            }

            return solution;
        }

        /// <summary>
        /// Remove the added table preprocessor symbol.  Don't want that saved into the project
        /// file as a change. 
        /// </summary>
        private Solution RemoveTablePreprocessorSymbol(Solution newSolution, Solution oldSolution)
        {
            var projectIds = newSolution.ProjectIds;
            foreach (var projectId in projectIds)
            {
                var oldProject = oldSolution.GetProject(projectId);
                var newProject = newSolution.GetProject(projectId);
                newSolution = newProject.WithParseOptions(oldProject.ParseOptions).Solution;
            }

            return newSolution;
        }

        internal async Task<Solution> FormatCoreAsync(Solution originalSolution, IReadOnlyList<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            var solution = originalSolution;

            if (_allowTables)
            {
                solution = AddTablePreprocessorSymbol(originalSolution);
            }

            solution = await RunSyntaxPass(solution, documentIds, cancellationToken);
            solution = await RunLocalSemanticPass(solution, documentIds, cancellationToken);
            solution = await RunGlobalSemanticPass(solution, documentIds, cancellationToken);

            if (_allowTables)
            {
                solution = RemoveTablePreprocessorSymbol(solution, originalSolution);
            }

            return solution;
        }

        private bool ShouldBeProcessed(Document document)
        {
            foreach (var filter in _filters)
            {
                var shouldBeProcessed = filter.ShouldBeProcessed(document);
                if (!shouldBeProcessed)
                    return false;
            }

            return true;
        }

        private Task<SyntaxNode> GetSyntaxRootAndFilter(Document document, CancellationToken cancellationToken)
        {
            if (!ShouldBeProcessed(document))
            {
                return Task.FromResult<SyntaxNode>(null);
            }

            return document.GetSyntaxRootAsync(cancellationToken);
        }

        private Task<SyntaxNode> GetSyntaxRootAndFilter(IFormattingRule formattingRule, Document document, CancellationToken cancellationToken)
        {
            if (!formattingRule.SupportsLanguage(document.Project.Language))
            {
                return Task.FromResult<SyntaxNode>(null);
            }

            return GetSyntaxRootAndFilter(document, cancellationToken);
        }

        private void StartDocument()
        {
            _watch.Restart();
        }

        private void EndDocument(Document document)
        {
            _watch.Stop();
            if (_verbose)
            {
                FormatLogger.WriteLine("    {0} {1} seconds", document.Name, _watch.Elapsed.TotalSeconds);
            }
        }

        /// <summary>
        /// Semantics is not involved in this pass at all.  It is just a straight modification of the 
        /// parse tree so there are no issues about ensuring the version of <see cref="SemanticModel"/> and
        /// the <see cref="SyntaxNode"/> line up.  Hence we do this by iteraning every <see cref="Document"/> 
        /// and processing all rules against them at once 
        /// </summary>
        private async Task<Solution> RunSyntaxPass(Solution originalSolution, IReadOnlyList<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            FormatLogger.WriteLine("\tSyntax Pass");

            var currentSolution = originalSolution;
            foreach (var documentId in documentIds)
            {
                var document = originalSolution.GetDocument(documentId);
                var syntaxRoot = await GetSyntaxRootAndFilter(document, cancellationToken);
                if (syntaxRoot == null)
                {
                    continue;
                }

                StartDocument();
                var newRoot = RunSyntaxPass(syntaxRoot, document.Project.Language);
                EndDocument(document);

                if (newRoot != syntaxRoot)
                {
                    currentSolution = currentSolution.WithDocumentSyntaxRoot(document.Id, newRoot);
                }
            }

            return currentSolution;
        }

        private SyntaxNode RunSyntaxPass(SyntaxNode root, string languageName)
        {
            foreach (var rule in GetOrderedRules(_syntaxRules))
            {
                if (rule.SupportsLanguage(languageName))
                {
                    root = rule.Process(root, languageName);
                }
            }

            return root;
        }

        private async Task<Solution> RunLocalSemanticPass(Solution solution, IReadOnlyList<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            FormatLogger.WriteLine("\tLocal Semantic Pass");
            foreach (var localSemanticRule in GetOrderedRules(_localSemanticRules))
            {
                solution = await RunLocalSemanticPass(solution, documentIds, localSemanticRule, cancellationToken);
            }

            return solution;
        }

        private async Task<Solution> RunLocalSemanticPass(Solution originalSolution, IReadOnlyList<DocumentId> documentIds, ILocalSemanticFormattingRule localSemanticRule, CancellationToken cancellationToken)
        {
            if (_verbose)
            {
                FormatLogger.WriteLine("  {0}", localSemanticRule.GetType().Name);
            }

            var currentSolution = originalSolution;
            foreach (var documentId in documentIds)
            {
                var document = originalSolution.GetDocument(documentId);
                var syntaxRoot = await GetSyntaxRootAndFilter(localSemanticRule, document, cancellationToken);
                if (syntaxRoot == null)
                {
                    continue;
                }

                StartDocument();
                var newRoot = await localSemanticRule.ProcessAsync(document, syntaxRoot, cancellationToken);
                EndDocument(document);

                if (syntaxRoot != newRoot)
                {
                    currentSolution = currentSolution.WithDocumentSyntaxRoot(documentId, newRoot);
                }
            }

            return currentSolution;
        }

        private async Task<Solution> RunGlobalSemanticPass(Solution solution, IReadOnlyList<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            FormatLogger.WriteLine("\tGlobal Semantic Pass");
            foreach (var globalSemanticRule in GetOrderedRules(_globalSemanticRules))
            {
                solution = await RunGlobalSemanticPass(solution, documentIds, globalSemanticRule, cancellationToken);
            }

            return solution;
        }

        private async Task<Solution> RunGlobalSemanticPass(Solution solution, IReadOnlyList<DocumentId> documentIds, IGlobalSemanticFormattingRule globalSemanticRule, CancellationToken cancellationToken)
        {
            if (_verbose)
            {
                FormatLogger.WriteLine("  {0}", globalSemanticRule.GetType().Name);
            }

            foreach (var documentId in documentIds)
            {
                var document = solution.GetDocument(documentId);
                var syntaxRoot = await GetSyntaxRootAndFilter(globalSemanticRule, document, cancellationToken);
                if (syntaxRoot == null)
                {
                    continue;
                }

                StartDocument();
                solution = await globalSemanticRule.ProcessAsync(document, syntaxRoot, cancellationToken);
                EndDocument(document);
            }

            return solution;
        }
    }
}
