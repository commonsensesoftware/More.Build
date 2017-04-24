namespace More.Build.Tasks
{
    using Microsoft.CodeAnalysis;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Reflection;
    using static System.String;

    /// <summary>
    /// Provides extension methods for retrieving attribute information.
    /// </summary>
    [CLSCompliant( false )]
    public static class AttributeExtensions
    {
        /// <summary>
        /// Returns a single value from an attribute with the specified name.
        /// </summary>
        /// <param name="attributes">The <see cref="IEnumerable{T}">sequence</see> of <see cref="AttributeData">attributes</see> to search.</param>
        /// <param name="attributeName">The name of the attribute to find.</param>
        /// <returns>The single value from the attribute's constructor or <c>null</c> if no match is found.</returns>
        public static string GetSingleAttributeValue( this IEnumerable<AttributeData> attributes, string attributeName )
        {
            Contract.Requires( attributes != null );
            Contract.Requires( !IsNullOrEmpty( attributeName ) );
            return (string) attributes.FirstOrDefault( a => a.AttributeClass.Name == attributeName )?.ConstructorArguments[0].Value;
        }

        /// <summary>
        /// Returns the assembly version from the set of attributes.
        /// </summary>
        /// <param name="attributes">The <see cref="IEnumerable{T}">sequence</see> of <see cref="AttributeData">attributes</see> to search.</param>
        /// <returns>The resolved assembly version or <c>null</c>.</returns>
        public static string GetDescription( this IEnumerable<AttributeData> attributes )
        {
            Contract.Requires( attributes != null );
            return attributes.GetSingleAttributeValue( nameof( AssemblyDescriptionAttribute ) );
        }

        /// <summary>
        /// Returns the assembly informational version from the set of attributes.
        /// </summary>
        /// <param name="attributes">The <see cref="IEnumerable{T}">sequence</see> of <see cref="AttributeData">attributes</see> to search.</param>
        /// <returns>The resolved assembly informational version or <c>null</c>.</returns>
        [CLSCompliant( false )]
        public static string GetCompany( this IEnumerable<AttributeData> attributes )
        {
            Contract.Requires( attributes != null );
            return attributes.GetSingleAttributeValue( nameof( AssemblyCompanyAttribute ) );
        }

        /// <summary>
        /// Returns the semantic version from the set of attributes.
        /// </summary>
        /// <param name="attributes">The <see cref="IEnumerable{T}">sequence</see> of <see cref="AttributeData">attributes</see> to search.</param>
        /// <returns>The resolved semantic version or <c>null</c>.</returns>
        public static string GetSemanticVersion( this IEnumerable<AttributeData> attributes )
        {
            Contract.Requires( attributes != null );

            var version = attributes.GetSingleAttributeValue( nameof( AssemblyInformationalVersionAttribute ) );

            if ( IsNullOrEmpty( version ) )
            {
                version = attributes.GetAssemblyVersion();
            }

            return version;
        }

        /// <summary>
        /// Returns the assembly version from the set of attributes.
        /// </summary>
        /// <param name="attributes">The <see cref="IEnumerable{T}">sequence</see> of <see cref="AttributeData">attributes</see> to search.</param>
        /// <returns>The resolved assembly version or "0.0.0.0".</returns>
        public static string GetAssemblyVersion( this IEnumerable<AttributeData> attributes )
        {
            Contract.Requires( attributes != null );

            var version = attributes.GetSingleAttributeValue( nameof( AssemblyVersionAttribute ) );

            if ( IsNullOrEmpty( version ) )
            {
                version = "0.0.0.0";
            }

            return version;
        }
    }
}