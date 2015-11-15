# t4-SolutionManager
manages multi file generation with multi folder target. List the files from solution. Parse the code C#, Visual Basic, and all .NET languages integrated into Visual Studio.


## split the generation in many files
```c#

<#@ template debug="false" hostspecific="true" language="C#" #>
<#@ assembly name="System.Core" #>
<#@ output extension=".txt" #>
<#@ include file="..\..\SolutionManagement.t4" /* reference the file include */ #>

<# 

	// initialize the split file manager. 
	using(ManagerScope _manager = StartManager())
	{  
		// create a new file called 'test.generated.txt' located under the script T4.
		using (_manager.NewFile("Test.txt"))
		{
			WriteLine("Test");
		}
  	}
#>

```

## generate the files in a specific folder
```c#

<#@ template debug="false" hostspecific="true" language="C#" #>
<#@ assembly name="System.Core" #>
<#@ import namespace="System.Linq" #>
<#@ import namespace="System.Text" #>
<#@ import namespace="System.Collections.Generic" #>
<#@ output extension=".txt" #>
<#@ include file="..\..\SolutionManagement.t4" #>

<# 

	// initialize the split file manager. 
	using(ManagerScope _manager = StartManager())
	{  

		// get the project that contains the script T4
		var project = _manager.GetCurrentProject();

		// get the folder specified by the path.
		var folder = project.GetFolder(@"targetFolder\SubTargetFolder");

		// create a new file called 'test.generated.txt' located in the specified folder.
		// Note : all files tagged 'generated' in the name are deleted.
		using (_manager.NewFile("Test.txt", folder))
        	{
			WriteLine("Test");
		}

    }
#>

```

## Manipulate the solution
```c#

	// get a reference to the solution
	var sln = Solution();

	// get the project called 'SplitFiles'
	var prj = sln.GetProjects(c => c.Name == "SplitFiles").FirstOrDefault();

	// add a file to the solution
	prj.AddFile("filename.txt");

```

