using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.IO;
using System.Text;

namespace Microsoft.DotNetCore.CodeFormatting
{
    public sealed class ResponseFileWorkspace : Workspace
    {
        private static Encoding s_utf8WithoutBom = new UTF8Encoding(false);

        private ResponseFileWorkspace()
                  : base(Microsoft.CodeAnalysis.Host.Mef.MefHostServices.DefaultHost, "Custom")
        {
        }

        public static ResponseFileWorkspace Create()
        {
            return new ResponseFileWorkspace();
        }

        public Project OpenCommandLineProject(string responseFile, string language, ProjectInfo projectInfo)
        {
            this.OnProjectAdded(projectInfo);
            return this.CurrentSolution.GetProject(projectInfo.Id);
        }

        public override bool CanApplyChange(ApplyChangesKind feature)
        {
            return feature == ApplyChangesKind.ChangeDocument;
        }

        protected override void ApplyDocumentTextChanged(DocumentId documentId, SourceText text)
        {
            var document = this.CurrentSolution.GetDocument(documentId);
            if (document != null)
            {
                try
                {
                    using (var writer = new StreamWriter(document.FilePath, append: false, encoding: text.Encoding ?? s_utf8WithoutBom))
                    {
                        text.Write(writer);
                    }
                }
                catch (IOException e)
                {
                    this.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, e.Message, documentId));
                }
                catch (UnauthorizedAccessException e)
                {
                    this.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, e.Message, documentId));
                }

                this.OnDocumentTextChanged(documentId, text, PreservationMode.PreserveValue);
            }
        }
    }
}
