# t4-SolutionManager
manages multi file generation with multi folder target. List the files from solution. Parse the code C#, Visual Basic, and all .NET languages integrated into Visual Studio.


## split the generation in many files
```c#

<#@ template debug="false" hostspecific="true" language="C#" #>
<#@ assembly name="System.Core" #>
<#@ output extension=".txt" #>
<#@ include file="..\..\SolutionManagement.t4" #>

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
