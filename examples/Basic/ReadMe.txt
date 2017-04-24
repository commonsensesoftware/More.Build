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
* The T4 output for a file should:
	* Have it's build action set to "None"
	* Have the generated file type added to the *.gitignore since the file will change after every build