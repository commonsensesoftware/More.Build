namespace More.Build.Tasks
{
    using Microsoft.Build.Evaluation;
    using Microsoft.Build.Framework;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Represents the base implemenation for a Microsoft Build <see cref="ITask">task</see> which gets metadata from a source project.
    /// </summary>
    [CLSCompliant( false )]
    public abstract class AssemblyMetadataTask : ITask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyMetadataTask"/> class.
        /// </summary>
        protected AssemblyMetadataTask()
        {
        }

        /// <summary>
        /// Gets or sets the build engine associated with the task.
        /// </summary>
        /// <value>The <see cref="IBuildEngine">build engine</see> associated with the task.</value>
        public IBuildEngine BuildEngine
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets any host object that is associated with the task.
        /// </summary>
        /// <value>The <see cref="ITaskHost">host object</see> associated with the task.</value>
        public ITaskHost HostObject
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the task is valid.
        /// </summary>
        /// <value>True if the task is valid; otherwise, false. The default value is <c>true</c>.</value>
        /// <remarks>This proeprty is used to determine whether the task executed successfully.</remarks>
        public bool IsValid
        {
            get;
            protected set;
        }

        /// <summary>
        /// Populates the metadata provided by the task using the specified attributes.
        /// </summary>
        /// <param name="attributes">The <see cref="IReadOnlyList{T}">read-only list</see> of
        /// <see cref="AttributeData">attributes</see> to populate the metadata from.</param>
        protected abstract void PopulateMetadataFromAttributes( IReadOnlyList<AttributeData> attributes );

        /// <summary>
        /// Creates and returns a regular expression that can be used to match assembly information files.
        /// </summary>
        /// <returns>A new <see cref="Regex">regular expression</see>.</returns>
        protected virtual Regex CreateRegularExpressionForAssemblyInfo()
        {
            Contract.Ensures( Contract.Result<Regex>() != null );

            var pattern = AssemblyInfoFilePattern;

            if ( string.IsNullOrEmpty( pattern ) )
                pattern = ".*AssemblyInfo.cs";

            return new Regex( pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline );
        }

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <returns>True if the task executed successfully; otherwise, false.</returns>
        public bool Execute()
        {
            var fullPath = SourceProjectPath;
            var basePath = Path.GetDirectoryName( fullPath );
            var regex = CreateRegularExpressionForAssemblyInfo();

            using ( var projectCollection = new ProjectCollection() )
            {
                var project = projectCollection.LoadProject( fullPath );

                try
                {
                    // note: the reason we open the project and examine the items rather than just emumerate the
                    // file system is that the process would miss linked files. we should be able to assemble the
                    // assembly attributes from all assembly info files, even they are linked files or imported
                    // from a source that cannot be evaluated by looking at the file system alone.
                    var assemblyInfos = from item in project.GetItems( "Compile" )
                                        let include = item.EvaluatedInclude
                                        where regex.IsMatch( include )
                                        let itemPath = Path.GetFullPath( Path.Combine( basePath, include ) )
                                        let code = File.ReadAllText( itemPath )
                                        select CSharpSyntaxTree.ParseText( code );
                    var references = new[] { MetadataReference.CreateFromFile( typeof( object ).Assembly.Location ) };
                    var compilation = CSharpCompilation.Create( project.GetPropertyValue( "AssemblyName" ), assemblyInfos, references );
                    var attributes = compilation.Assembly.GetAttributes().ToArray();

                    IsValid = true;
                    PopulateMetadataFromAttributes( attributes );
                }
                finally
                {
                    projectCollection.UnloadProject( project );
                }
            }

            return IsValid;
        }

        /// <summary>
        /// Gets or sets the source project to get the NuGet package semantic version from.
        /// </summary>
        /// <value>The path of the source project.</value>
        [Required]
        public string SourceProjectPath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the regular expression pattern used to match assembly information source files.
        /// </summary>
        /// <value>The regular expression pattern used to match assembly information source files. If no value is provided,
        /// then the default pattern will be ".*AssemblyInfo.cs".</value>
        /// <remarks></remarks>
        public string AssemblyInfoFilePattern
        {
            get;
            set;
        }
    }
}
