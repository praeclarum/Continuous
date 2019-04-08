using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.CodeAnalysis.Text;

namespace Continuous.Client.MD.Extensions
{
    public static class VSExtensions
    {
        public static Document GetAnalysisDocument(this MonoDevelop.Ide.Gui.Document fromDocument)
        {
            var textBuffer = fromDocument.GetContent<ITextBuffer>();
            if (textBuffer != null && textBuffer.AsTextContainer() is SourceTextContainer container)
            {
                var document = container.GetOpenDocumentInCurrentContext();
                if (document != null)
                {
                    return document;
                }
            }

            return null;
        }
        
        /// <summary>
        /// Gets the document from the corresponding workspace's current solution that is associated with the text container 
        /// in its current project context.
        /// </summary>
        public static Document GetOpenDocumentInCurrentContext(this SourceTextContainer container)
        {
            if (Workspace.TryGetWorkspace(container, out var workspace))
            {
                var id = workspace.GetDocumentIdInCurrentContext(container);
                return workspace.CurrentSolution.GetDocument(id);
            }

            return null;
        }
    }
}
