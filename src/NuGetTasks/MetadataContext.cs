namespace More.Build.Tasks
{
    using Microsoft.CodeAnalysis;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using static System.String;
    using Project = Microsoft.Build.Evaluation.Project;

    /// <summary>
    /// Represents a context used to collect metadata.
    /// </summary>
    [CLSCompliant( false )]
    public class MetadataContext
    {
        readonly Lazy<string> assemblyVersion;
        readonly Lazy<string> author;
        readonly Lazy<string> description;
        readonly Lazy<Tuple<string, string, string>> semanticVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataContext"/> class.
        /// </summary>
        /// <param name="project">The source <see cref="Project">project</see>.</param>
        /// <param name="attributes">The <see cref="IReadOnlyList{T}">read-only list</see> of assembly attributes.</param>
        public MetadataContext( Project project, IReadOnlyList<AttributeData> attributes )
        {
            Contract.Requires( project != null );
            Contract.Requires( attributes != null );

            Project = project;
            Attributes = attributes;
            assemblyVersion = new Lazy<string>( ResolveAssemblyVersion );
            author = new Lazy<string>( ResolveAuthor );
            description = new Lazy<string>( ResolveDescription );
            semanticVersion = new Lazy<Tuple<string, string, string>>( ResolveSemanticVersion );
        }

        /// <summary>
        /// Gets the current Microsoft Build project.
        /// </summary>
        /// <value>The current <see cref="Project">project</see> being evaluated.</value>
        public Project Project { get; }

        /// <summary>
        /// Gets a collection of evaluated assembly attributes.
        /// </summary>
        /// <value>A <see cref="IReadOnlyList{T}">read-only list</see> of <see cref="AttributeData">attributes</see>.</value>
        public IReadOnlyList<AttributeData> Attributes { get; }

        /// <summary>
        /// Gets the version of the referenced assembly.
        /// </summary>
        /// <value>An assembly version.</value>
        public string AssemblyVersion => assemblyVersion.Value;

        /// <summary>
        /// Gets the semantic version of the referenced NuGet package.
        /// </summary>
        /// <value>A NuGet semantic version.</value>
        public string SemanticVersion => semanticVersion.Value.Item1;

        /// <summary>
        /// Gets the semantic version prefix of the referenced NuGet package.
        /// </summary>
        /// <value>A NuGet semantic version without any pre-release information.</value>
        public string SemanticVersionPrefix => semanticVersion.Value.Item2;

        /// <summary>
        /// Gets the semantic version suffix of the referenced NuGet package.
        /// </summary>
        /// <value>The NuGet semantic version pre-release and build metadata information.</value>
        public string SemanticVersionSuffix => semanticVersion.Value.Item3;

        /// <summary>
        /// Gets the author of the referenced assembly.
        /// </summary>
        /// <value>The assembly author.</value>
        public string Author => author.Value;

        /// <summary>
        /// Gets the description of the referenced assembly.
        /// </summary>
        /// <value>The assembly description.</value>
        public string Description => description.Value;

        string ResolveAssemblyVersion()
        {
            var value = Project.GetPropertyValue( nameof( AssemblyVersion ) );

            if ( IsNullOrEmpty( value ) )
            {
                return Attributes.GetAssemblyVersion();
            }

            return value;
        }

        string ResolveAuthor()
        {
            var value = Project.GetPropertyValue( "Authors" );

            if ( IsNullOrEmpty( value ) )
            {
                return Attributes.GetCompany();
            }

            return value;
        }

        string ResolveDescription()
        {
            var value = Project.GetPropertyValue( nameof( Description ) );

            if ( IsNullOrEmpty( value ) )
            {
                return Attributes.GetDescription();
            }

            return value;
        }

        Tuple<string, string, string> ResolveSemanticVersion()
        {
            var full = Project.GetPropertyValue( "PackageVersion" );
            var suffixOverride = Project.GetPropertyValue( "VersionSuffix" );
            var prefix = default( string );
            var suffix = default( string );

            if ( IsNullOrEmpty( full ) )
            {
                prefix = Project.GetPropertyValue( "VersionPrefix" );

                if ( IsNullOrEmpty( prefix ) )
                {
                    full = Project.GetPropertyValue( nameof( AssemblyVersion ) );

                    if ( IsNullOrEmpty( full ) )
                    {
                        full = Attributes.GetSemanticVersion();
                        full = SplitSemanticVersion( full, out prefix, out suffix, suffixOverride );
                    }
                    else
                    {
                        full = full.Substring( 0, full.LastIndexOf( '.' ) );
                        prefix = full;
                        suffix = suffixOverride;
                    }
                }
                else
                {
                    suffix = suffixOverride;

                    if ( IsNullOrEmpty( suffix ) )
                    {
                        full = prefix;
                    }
                    else
                    {
                        full = $"{prefix}-{suffix}";
                    }
                }
            }
            else
            {
                full = SplitSemanticVersion( full, out prefix, out suffix, suffixOverride );
            }

            return Tuple.Create( full, prefix, suffix );
        }

        static string SplitSemanticVersion( string version, out string prefixComponent, out string suffixComponent, string suffixOverrideComponent )
        {
            suffixComponent = null;

            if ( IsNullOrEmpty( version ) )
            {
                prefixComponent = null;
                return version;
            }

            var parts = version.Split( '-' );

            prefixComponent = parts[0];

            if ( IsNullOrEmpty( suffixOverrideComponent ) )
            {
                if ( parts.Length == 2 )
                {
                    suffixComponent = parts[1];
                }

                return version;
            }

            suffixComponent = suffixOverrideComponent;
            return $"{prefixComponent}-{suffixComponent}";
        }
    }
}