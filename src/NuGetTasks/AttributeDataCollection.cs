namespace More.Build.Tasks
{
    using Microsoft.CodeAnalysis;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Text.RegularExpressions;
    using static Microsoft.CodeAnalysis.CSharp.CSharpCompilation;
    using static Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree;
    using static Microsoft.CodeAnalysis.MetadataReference;
    using static System.IO.File;
    using static System.IO.Path;
    using static System.String;
    using static System.Text.RegularExpressions.RegexOptions;
    using Project = Microsoft.Build.Evaluation.Project;

    /// <summary>
    /// Represents a collection of assembly attributes.
    /// </summary>
    [CLSCompliant( false )]
    public class AttributeDataCollection : IReadOnlyList<AttributeData>
    {
        readonly string assemblyInfoFilePattern;
        readonly Project project;
        readonly Lazy<IReadOnlyList<AttributeData>> attributes;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeDataCollection"/> class.
        /// </summary>
        /// <param name="project">The source <see cref="Project">project</see> containing the assembly attributes.</param>
        /// <param name="assemblyInfoFilePattern">The regular expression pattern used to match assembly information files.</param>
        public AttributeDataCollection( Project project, string assemblyInfoFilePattern )
        {
            Contract.Requires( project != null );

            this.project = project;
            this.assemblyInfoFilePattern = IsNullOrEmpty( assemblyInfoFilePattern ) ? ".*AssemblyInfo.cs" : assemblyInfoFilePattern;
            attributes = new Lazy<IReadOnlyList<AttributeData>>( EvaluateAssemblyAttributes );
        }

        /// <summary>
        /// Gets the item in the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to retrieve.</param>
        /// <returns>The item at the specified <paramref name="index"/>.</returns>
        public AttributeData this[int index] => attributes.Value[index];

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        /// <value>The total number of items in the collection.</value>
        public int Count => attributes.Value.Count;

        /// <summary>
        /// Returns an iterator than can enumerate the collection.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator{T}">enumerator</see>.</returns>
        public IEnumerator<AttributeData> GetEnumerator() => attributes.Value.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IReadOnlyList<AttributeData> EvaluateAssemblyAttributes()
        {
            // note: the reason we open the project and examine the items rather than just emumerate the
            // file system is that the process would miss linked files. we should be able to assemble the
            // assembly attributes from all assembly info files, even they are linked files or imported
            // from a source that cannot be evaluated by looking at the file system alone.
            var regex = new Regex( assemblyInfoFilePattern, IgnoreCase | Singleline );
            var assemblyInfos = from item in project.GetItems( "Compile" )
                                let include = item.EvaluatedInclude
                                where regex.IsMatch( include )
                                let itemPath = GetFullPath( Combine( project.DirectoryPath, include ) )
                                let code = ReadAllText( itemPath )
                                select ParseText( code );
            var references = new[] { CreateFromFile( typeof( object ).Assembly.Location ) };
            var compilation = Create( project.GetPropertyValue( "AssemblyName" ), assemblyInfos, references );

            return compilation.Assembly.GetAttributes().ToArray();
        }
    }
}