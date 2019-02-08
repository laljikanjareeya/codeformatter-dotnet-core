using System.Collections.Generic;
using System.Composition.Hosting;
using System.Reflection;

namespace Microsoft.DotNetCore.CodeFormatting
{
    public static class FormattingEngine
    {
        public static IFormattingEngine Create()
        {
            var container = CreateCompositionContainer();
            var engine = container.CreateContainer().GetExport<IFormattingEngine>();
            var consoleFormatLogger = new ConsoleFormatLogger();
            return engine;
        }

        public static List<IRuleMetadata> GetFormattingRules()
        {
            //TODO:need to debug
            var container = CreateCompositionContainer();
            var list = new List<IRuleMetadata>();
            AppendRules<ISyntaxFormattingRule>(list, container);
            AppendRules<ILocalSemanticFormattingRule>(list, container);
            AppendRules<IGlobalSemanticFormattingRule>(list, container);
            return list;
        }

        private static void AppendRules<T>(List<IRuleMetadata> list, ContainerConfiguration container)
            where T : IFormattingRule
        {
            //TODO:need to debug
            var contains = container.CreateContainer().GetExports<IRuleMetadata>();
            list.AddRange(contains);
        }

        private static ContainerConfiguration CreateCompositionContainer()
        {
            var assemblies = new[] { typeof(FormattingEngine).GetTypeInfo().Assembly };
            var configuration = new ContainerConfiguration()
                .WithAssembly(typeof(FormattingEngine).GetTypeInfo().Assembly);
            return configuration;
        }
    }
}
