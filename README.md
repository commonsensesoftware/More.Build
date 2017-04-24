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

_Read Source Code → Generate Properties for Referenced Projects → NuGet Pack_

_Read Source Code → Generate T4 Template Parameters → Transform T4 Template → Compile_

### How It Works

#### Step 1
The first step uses a custom MSBuild task that reads the current NuGet semantic
version from a referenced project. It achieves this by:

 1. Loading the source project and enumerating all **&lt;Compile /&gt;** items. This
    approach is used to capture scenarios such as linked files.
 2. Matching items whose **Include** attribute matches the configured regular
    expression. The default pattern is ".\*AssemblyInfo.cs".
 3. Using only the matched files, create a semantic (not compiled) build of
    the source assembly using the NET Compiler Platform (Roslyn).
 4. Enumerating attributes for the semantic version. Honor the **AssemblyInformationalVersionAttribute** 
    first and then fall back to the **AssemblyVersionAttribute** value.
 5. The following MSBuild 15.0 NuGet properties are honored, when present:
    * PackageVersion
    * VersionPrefix
    * VersionSuffix
 6. Set the task output to the resolved semantic version.

MSBuild properties have prescendence. This allows you to build packages without changing code or \*.nuspec files. This is particularly useful for pre-release packages. For example:

`msbuild /p:VersionSuffix=beta1`

In addition, this methodology has close parity with NuGet packages create natively for projects using MSBuild 15.0 and will ease the transition to the new project format.

#### Step 2
Starting with Visual Studio .NET 2013, MSBuild has improved T4 support that
allows using a build property as an input parameter to a T4 template. Using
the output of the build task in *Step 1*, we can create items that the build
can forward to a T4 template as follows:
```xml
<ItemGroup>
 <T4ParameterValue Include="Project1">
  <Value>$(NuGetSemanticVersion)</Value>
 </T4ParameterValue>
</ItemGroup>
```

#### Step 3
Now we can simply reference any generated project reference tokens in our \*.nuspec file:

```xml
<?xml version="1.0"?>
<package xmlns="http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd">
 <metadata>
  <!-- omitted -->
  <dependencies>
   <dependency id="LibraryA" version="$librarya$" />
  </dependencies>
 </metadata>
</package>
```

When we need to reference a NuGet package outside of a \*.nuspec file, we can change our source file into a T4 template (\*.tt) that outputs an up-to-date file during each build. An abridged T4 template for a Visual Studio template (\*.vstempalte) would look like:

```t4
<#@ template language="c#" hostspecific="true" #>
<#@ output extension=".vstemplate" #>
<#@ parameter type="System.String" name="LibraryA" #><?xml version="1.0" encoding="utf-8"?>
<VSTemplate Version="3.0.0" Type="Item"
            xmlns="http://schemas.microsoft.com/developer/vstemplate/2005"
            xmlns:sdk="http://schemas.microsoft.com/developer/vstemplate-sdkextension/2010">
  <TemplateData><!-- ommitted --></TemplateData>
  <TemplateContent><!-- ommitted --></TemplateContent>
  <WizardExtension>
    <Assembly>NuGet.VisualStudio.Interop, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a</Assembly>
    <FullClassName>NuGet.VisualStudio.TemplateWizard</FullClassName>
  </WizardExtension>
  <WizardData>
    <packages repository="extension" repositoryId="My.Qualified.Vsix.Id">
      <package id="LibraryA" version="<#= LibraryA #>" />
    </packages>
  </WizardData>
</VSTemplate>
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

#### Leveraging Project References
When you do have dependent projects, the sources that will provide the input semantic
versions will be automatically inferred from project references. For example:
```xml
<ItemGroup>
 <ProjectReference Include="..\LibraryA.csproj">
  <Name>LibA</Name>
 </ProjectReference>
</ItemGroup>
```

#### NuGet Pack Properties
The following rules apply to NuGet pack properties generated from project references:

* If a **&lt;ProjectReference&gt;** has a **&lt;Name&gt;**, that value will be used
* For **&lt;ProjectReference&gt;** that does not have **&lt;Name&gt;** metadata, the name of the project file is used (without an extension)
* The resultant token will always be lowercase to have parity with other properties
* A token name cannot have a `.` character and will be replaced with the `_` character

**Examples**
* Project1.csproj -> \$project1\$
* Other.Project.csproj -> \$other_project\$

#### T4 Text Templating Parameters
The following rules apply to T4 text templating parameters generated from project references:

* If a **&lt;ProjectReference&gt;** has a **&lt;Name&gt;**, that value will be used
* For a **&lt;ProjectReference&gt;** that does not have **&lt;Name&gt;** metadata, the name of the project file is used (without an extension)
* A token name cannot have a `.` character and will be replaced with the `_` character

**Examples**
* Project1.csproj
  * Declaration: `<#@ parameter type="System.String" name="Project1" #>`
  * Reference: `<#= Project1 #>`
* Other.Project.csproj
  * Declaration: `<#@ parameter type="System.String" name="Other_Project" #>`
  * Reference: `<#= Other_Project #>`

#### Customizing the Build
There a several ways the build can be customized. The following outlines a few of
the most common customizations:

* **Package Output** - The package output can be changed by setting the
                       **&lt;PackageOutDir /&gt;** or **&lt;PackageOutputPath /&gt;** (MSBuild 15.0)
                       build property. The default location is a subdirectory called "NuGet" in
                       solution directory.
* **NuGet.exe** - The NuGet executable is referenced from the dependency on the
                  **NuGet.CommandLine** package. If you prefer some other location, you
                  can override it using the **&lt;NuGetExe /&gt;** build property.
* **NuGet Properties** - The **&lt;NuGetPackProperties /&gt;** property can be specified to
                         pass additional properties that can be used as replacement tokens
                         in the \*.nuspec file. By default, only the build properties
                         **&lt;Configuration /&gt;** and **&lt;Platform /&gt;** are supplied,
                         which map to the **$configuration$** and **$platform$** tokens respectively.
                         You should append to this property's value rather than replace it.
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
* **Exclude Referenced Projects** - The **-IncludeReferencedProjects** option is not
                                    specified. This avoids scenarios where unnecessary,
                                    transitive dependencies are included in your packages.
                                    To override this behavior and let NuGet determine the
                                    dependencies for you, specify
                                    **&lt;ExcludeReferencedProjects&gt;true&lt;/ExcludeReferencedProjects&gt;**.

Any of these properties can be specified directly in the target project, but that would require unloading, editing, and reloading the project each time there is a change. As an alterative, the provided \*.targets file will automatically import your custom properties if you add an MSBuild file that follows the naming convention: **[project name].nuget.props**. This is the recommended approach to manage the NuGet build settings specific to your project.

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