The following are some points of interest:

* By convention, the specified *.props file must be in the form of [project name].nuget.props
* If you'd rather not use a *.props file, you can define the required ItemGroup and items in the project itself
  using ProjectReference items.
* The "NuGet.CommandLine" package may not have the developmentDependency="true" define in its *.nuspec file
  and as a result it does not automatically set this attribute in the packages.config file. You may need to
  set this manually to avoid having it appear as a dependency in your package. This is a one time process.
* Make sure that T4 files (*.tt) have their build action set to "None"
* T4 files (*.tt) must have the @hostspecific="true". Also note that the T4-to-MSBuild integration only
  executes during a build. This means the transform only occurs during build-time.
* The T4 output for a *.nupec file should:
	* Have it's build action set to "None"
	* Have the *.nuspec added to the *.gitignore since the file will change after every build
* The T4 output for a *.vstemplate file should:
	* Have it's build action set to "VSTemplate"
	* Have the *.vstemplate added to the *.gitignore since the file will change after every build
* A project or item template project should set the PackAfterBuild property to "false" since templates do
  not directly out a *.nupkg
* When building a VSIX, the build dependencies may need to be configured manually so that the last dependency
  transforms and builds before the VSIX picks it up. This is required because the VSIX does not directly
  reference (and shouldn't) the source libraries. This example adds a build dependency to "Library F" to
  ensure the entire chain builds first. This also can be achieve with additional dependencies or setting the
  build order.
* The NuGet packages built for each project is output to a centralized location (..\NuGet). These packages
  are linked to the VSIX and packaged in the output *.vsix file. This allows rebuilds to pick up the
  most recent package output. The *.symbols.nupkg files are explicitly excluded.