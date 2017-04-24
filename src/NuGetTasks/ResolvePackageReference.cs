namespace More.Build.Tasks
{
    using Microsoft.Build.Framework;
    using System;
    using System.Diagnostics.Contracts;
    using static System.String;

    /// <summary>
    /// Represents a Microsoft Build <see cref="ITask">task</see> which resolves the NuGet
    /// semantic version of a referenced source project.
    /// </summary>
    [CLSCompliant( false )]
    public class ResolvePackageReference : AssemblyMetadataTask
    {
        /// <summary>
        /// Populates the metadata provided by the task using the specified context.
        /// </summary>
        /// <param name="context">The <see cref="MetadataContext">context</see> used to populate metadata.</param>
        protected override void PopulateMetadata( MetadataContext context )
        {
            Contract.Assume( context != null );

            AssemblyVersion = context.AssemblyVersion;
            SemanticVersion = context.SemanticVersion;
            SemanticVersionPrefix = context.SemanticVersionPrefix;
            SemanticVersionSuffix = context.SemanticVersionSuffix;
            IsValid = !IsNullOrEmpty( SemanticVersion );
        }

        /// <summary>
        /// Gets the version of assembly referenced by the task.
        /// </summary>
        /// <value>An assembly version.</value>
        [Output]
        public string AssemblyVersion { get; private set; }

        /// <summary>
        /// Gets the semantic version of NuGet package referenced by the task.
        /// </summary>
        /// <value>A NuGet semantic version.</value>
        [Output]
        public string SemanticVersion { get; private set; }

        /// <summary>
        /// Gets the semantic version prefix of NuGet package referenced by the task.
        /// </summary>
        /// <value>A NuGet semantic version without any pre-release information.</value>
        [Output]
        public string SemanticVersionPrefix { get; private set; }

        /// <summary>
        /// Gets the semantic version suffix of NuGet package referenced by the task.
        /// </summary>
        /// <value>The NuGet semantic version pre-release and build metadata information.</value>
        [Output]
        public string SemanticVersionSuffix { get; private set; }
    }
}