namespace More.Build.Tasks
{
    using Microsoft.Build.Evaluation;
    using Microsoft.Build.Framework;
    using System;
    using System.Collections.Generic;
    using static System.String;

    /// <summary>
    /// Represents the base implemenation for a Microsoft Build <see cref="ITask">task</see> which gets metadata from a source project.
    /// </summary>
    [CLSCompliant( false )]
    public abstract class AssemblyMetadataTask : ITask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyMetadataTask"/> class.
        /// </summary>
        protected AssemblyMetadataTask() { }

        /// <summary>
        /// Gets or sets the build engine associated with the task.
        /// </summary>
        /// <value>The <see cref="IBuildEngine">build engine</see> associated with the task.</value>
        public IBuildEngine BuildEngine { get; set; }

        /// <summary>
        /// Gets or sets any host object that is associated with the task.
        /// </summary>
        /// <value>The <see cref="ITaskHost">host object</see> associated with the task.</value>
        public ITaskHost HostObject { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the task is valid.
        /// </summary>
        /// <value>True if the task is valid; otherwise, false. The default value is <c>true</c>.</value>
        /// <remarks>This proeprty is used to determine whether the task executed successfully.</remarks>
        public bool IsValid { get; protected set; }

        /// <summary>
        /// Populates the metadata provided by the task using the specified context.
        /// </summary>
        /// <param name="context">The <see cref="MetadataContext">context</see> used to populate metadata.</param>
        protected abstract void PopulateMetadata( MetadataContext context );

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <returns>True if the task executed successfully; otherwise, false.</returns>
        public bool Execute()
        {
            var fullPath = SourceProjectPath;
            var pattern = AssemblyInfoFilePattern;
            var globalProperties = new Dictionary<string, string>();

            if ( IsNullOrEmpty( pattern ) )
            {
                pattern = ".*AssemblyInfo.cs";
            }

            if ( !IsNullOrEmpty( VersionSuffixOverride ) )
            {
                globalProperties.Add( "VersionSuffix", VersionSuffixOverride );
            }

            IsValid = true;

            using ( var projectCollection = new ProjectCollection() )
            {
                var project = projectCollection.LoadProject( fullPath, globalProperties, toolsVersion: null );

                if ( IsNullOrEmpty( project.FullPath ) )
                {
                    project.FullPath = fullPath;
                }

                var attributes = new AttributeDataCollection( project, pattern );
                var context = new MetadataContext( project, attributes );

                try
                {
                    PopulateMetadata( context );
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
        public string SourceProjectPath { get; set; }

        /// <summary>
        /// Gets or sets the regular expression pattern used to match assembly information source files.
        /// </summary>
        /// <value>The regular expression pattern used to match assembly information source files. If no value is provided,
        /// then the default pattern will be ".*AssemblyInfo.cs".</value>
        /// <remarks></remarks>
        public string AssemblyInfoFilePattern { get; set; }

        /// <summary>
        /// Gets or sets the overriden semantic version suffix of the referenced NuGet package.
        /// </summary>
        /// <value>The NuGet semantic version pre-release and build metadata information.</value>
        public string VersionSuffixOverride { get; set; }
    }
}