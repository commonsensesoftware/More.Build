# More.Build
Provides more extensions for MSBuild.

## More NuGet Build Tasks
This project contains custom MSBuild tasks for creating NuGet packages (\*.nupkg). This project differs from other variants that
build NuGet packages in that package sources can be chained together within a solution.

This project started and was later refactored out of the build process used in the [More](https://github.com/commonsensesoftware/More)
framework. Building a single NuGet package is one thing, but creating a build chain of packages where the referenced package version needs
to be automatically updated is far more challenging. This problem is further complicated for tool builders that need to include the
generated packages in other resources such as templates.

### Design
The design and build process is as follows:

_Read Source Code → Transform T4 Template → Compile_

### How It Works

#### Step 1
The first step uses a custom MSBuild task that reads the current NuGet semantic
version from the source project. It achieves this by:

 1. Loading the source project and enumerating all **&lt;Compile /&gt;** items. This
    approach is used to capture scenarios such as linked files.
 2. Matching items whose **Include** attribute matches the configured regular
    expression. The default pattern is ".\*AssemblyInfo.cs".
 3. Using only the matched files, create a semantic (not compiled) build of
    the source assembly using the NET Compiler Platform (Roslyn).
 4. Enumerating attributes for the semantic version. Honor the **AssemblyInformationalVersionAttribute** 
    first and then fall back to the **AssemblyVersionAttribute** value.
 5. Set the task output to the resolved semantic version.

#### Step 2
Starting with Visual Studio .NET 2013, MSBuild has improved T4 support that
allows using a build property as an input parameter to a T4 template. Using
the output of the build task in *Step 1*, we can create items that the build
can forward to a T4 template as follows:
```xml
<ItemGroup>
 <T4ParameterValue Include="MyVersion">
  <Value>$(NuGetSemanticVersion)</Value>
 </T4ParameterValue>
</ItemGroup>
```

#### Step 3
Now we can change our \*.nuspec file into a T4 template (\*.tt) that outputs an
up-to-date \*.nuspec files during each build. An abridged T4 template would look like:
```t4
<#@ template language="c#" hostspecific="true" #>
<#@ output extension=".nuspec" #>
<#@ parameter type="System.String" name="MyVersion" #><?xml version="1.0"?>
<package xmlns="http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd">
 <metadata>
  <!-- omitted for brevity -->
  <dependencies>
   <dependency id="MyPackage" version="<#= MyVersion #>" />
  </dependencies>
 </metadata>
</package>
```

#### Step 4
The final step extends the **AfterBuild** target so that the NuGet **pack**
command is run against the transformed \*.nuspec file. There are also a handful
of scenarios where you might want to opt out of this process, such as when
creating a T4 template for a \*.vstemplate in a Visual Studio project or item
template. These types of projects have the same build requirements, but do not
output a \*.nupkg file. To opt out of the process, you can specify
**&lt;PackAfterBuild&gt;false&lt;/PackAfterBuild&gt;** build property.

### Simplifying the Process
To simply the process and make it easily repeatable, a \*.targets file is included
in this package that stitches all the pieces together for you. If your project
doesn't have any other dependent packages in the solution, then you're done. The
corresponding \*.nupkg will be output on each build.

#### Add Your Own Specific Build Properties
When you do have dependent projects, the sources that will provide the input semantic
versions cannot be automatically inferred. In order to specify the source projects
and the corresponding T4 parameter name, you must specify an item group as follows:
```xml
<ItemGroup>
 <NuPkgSource Include="..\LibraryA.csproj">
  <T4ParameterName>Lib_A_Version</T4ParameterName>
 </NuPkgSource>
</ItemGroup>
```
This could be specified directly in the target project, but that would require
unloading, editing, and reloading the project each time there is a change. As
an alterative, the provided \*.targets file will automatically import your
custom properties if you add an MSBuild file that follows the naming convention:
**[project name].nuget.props**. This is the recommended approach to manage
the NuGet build settings specific to your project.

#### Customizing the Build
There a several ways the build can be customized. The following outlines a few of
the most common customizations:

* **Package Output** - The package output can be changed by setting the
                       **&lt;PackageOutDir /&gt;** build property. The default location
                       is a subdirectory called "NuGet" in solution directory.
* **NuGet.exe** - The NuGet executable is referenced from the dependency on the
                  **NuGet.CommandLine** package. If you prefer some other location, you
                  can override it using the **&lt;NuGetExe /&gt;** build property.
* **NuGet Properties** - The **&lt;NuGetPackProperties /&gt;** property can be specified to
                         pass additional properties that can be used as replacement tokens
                         in the \*.nuspec file. By default, only the build property
                         **&lt;Configuration /&gt;** is passed as an additional property,
                         which maps to the **$configuration$** token. You should append to
                         this property's value rather than replace it.
* **NuGet Pack Target** - By default, the target of the _pack_ command is the current
                          project. When you pack a source project, there are some
                          automatic behaviors (ex: references)  that cannot otherwise
                          be changed. This might occur, for example, if you're building
                          a package that only uses the **build** folder. In this scenario,
                          you can change the pack target to the \*.nuspec directly, by
                          specifying the build property **&lt;NuGetPackTarget&gt;**. When
                          this property is overriden, **&lt;NuGetPackProperties /&gt;**
                          will also automatically be updated with the standard tokens
                          **$id$**, **$version$**, **$author$**, and **$description$**,
                          which are normally only provided when building against a project.
* **Exclude Referenced Projects** - By default, if the target of the _pack_ command is the current
                                    project, then the **-IncludeReferencedProjects** option is
                                    automatically specified. The behavior of this switch includes
                                    dependencies for all referenced projects; however, some of those
                                    dependencies may be transitive. To override this behavior and
                                    specify the minimum dependencies yourself, you can specify
                                    **&lt;ExcludeReferencedProjects&gt;true&lt;/ExcludeReferencedProjects&gt;**.

#### Source Control Notes
No matter which version control system (VSC) you're using, you'll want to exclude the \*.nupkg
output folder. This is a subdirectory of the solution called "NuGet" by default. You'll also
want to exclude the output of T4 templates, such as \*.nuspec or \*.vstemplate, since they
will change during each build.

### Examples
No solution would be complete without examples. The [Examples](https://github.com/commonsensesoftware/More.Build/tree/master/examples)
folder contains the following examples:

* **Basic** - Provides a basic example where one library has a dependency on another.
* **Complex** - Provides a complex example where several different libraries have
                dependencies on one another, item templates are generated, a VSIX
                is compiled that has the current package and item template output.

### Limitations
* This solution currently only supports Visual C#. If there is interest in supporting
  other languages, it will be considered for implementation.
* T4 templates hosted by MSBuild can only be transformed when you perform a build. This
  may be counterintuitive to your previous experience using T4 templates.