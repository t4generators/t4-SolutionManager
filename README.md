
# t4-SolutionManager

[![Gitter](https://badges.gitter.im/Join Chat.svg)](https://gitter.im/gaelgael5/t4generators?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) ![Build status](https://ci.appveyor.com/api/projects/status/miuedx7p06tbhdk1?svg=true)

* Manages multi file generation with multi folder target.
* List the files from solution. 
* Parse the code C#, Visual Basic, and all .NET languages integrated into Visual Studio.


# Getting started

### Parsing solution

#### Split the generation in many files
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

#### Generate files in a specific folder
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

#### Manipulate the solution
```c#
...
	// get a reference to the solution
	var sln = Solution();

	// get all object of type NodeFolderSolution (solution folder)
	List<NodeFolderSolution> listSlnFolders = sln.GetItem<NodeFolderSolution>().ToList();

	// get all object of type NodeProject (Project)
	List<NodeProject> listProjects = sln.GetItem<NodeProject>().ToList();

	// get all object of type NodeItem (folder in project)
	List<NodeItemFolder> listFolders = sln.GetItem<NodeItemFolder>().ToList();

	// get all object of type NodeItem (working file *.cs, *.vb, ...)
	List<NodeItem> listFiles = sln.GetItem<NodeItem>().ToList();

	// get the project called 'SplitFiles'
	var prj = sln.GetProjects(c => c.Name == "SplitFiles").FirstOrDefault();

	// add a file in the solution
	prj.AddFile("filename.txt");
...

```

### Code parser

#### Parsing the classes of the solution

```c#

<#@ template debug="true" hostspecific="true" language="C#" #>
<#@ assembly name="System.Core" #>
<#@ import namespace="System.Linq" #>
<#@ import namespace="System.Collections.Generic" #>
<#@ output extension=".txt" #>
<#@ include file="..\..\SolutionManagement.t4" #>
<# 

	// get a reference to solution
	NodeSolution sln = Solution();

	// get all object of type NodeItem (working file *.cs, *.vb, ...)
	List<NodeItem> list = sln.GetItem<NodeItem>().ToList();

	foreach(NodeItem item in list)
	{

		// parse the list of code objects from 
		foreach(BaseInfo obj in item.GetClassItems())
		{
			
			ClassInfo cls = obj as ClassInfo;

			if (cls != null)
			{
				WriteLine("class : " + cls.Name);
				WriteLine("" + cls.DocComment);
				WriteLine("Methods : ");


				// Parse methods
				foreach(CodeFunctionInfo m in cls.GetMethods())
				{

					var t = m.Type.AsFullName.Trim();
					if (string.IsNullOrEmpty(t))
						t = "void";
					Write("\t" + t + " " + m.Name);
					Write("(");
					bool f = false;
                    foreach (MethodParamInfo p1 in m.Parameters)
                    {
						if (f)
							Write(", ");
						Write(p1.Type.AsFullName + " " + p1.Name);
						f = true;
					}
					WriteLine(");");
					
					WriteLine("");
				}


				// Parse proprerties
				WriteLine("");
				WriteLine("Properties : ");
				foreach(CodePropertyInfo p in cls.GetProperties())
				{
					TypeInfo t = p.Type;
					Write("\t" + t.AsFullName + " " + p.Name);
					WriteLine("");
				}


				// Parse events
				WriteLine("");
				WriteLine("Events : ");
				foreach(CodeEventInfo e in cls.GetEvents())
				{
					WriteLine("\t event " + e.Type.AsFullName  + " " + e.Name);
				}

			}

		}

	}


 #>
```

### Parse attributes


### Parse attributes

```c#
...
	// get a reference to solution
	NodeSolution sln = Solution();

	// get all object of type NodeItem (working file *.cs, *.vb, ...)
	List<NodeItem> list = sln.GetItem<NodeItem>().ToList();

	foreach(NodeItem item in list)
	{

		// parse the list of code objects from 
		foreach(BaseInfo obj in item.GetClassItems())
		{
			
			ClassInfo cls = obj as ClassInfo;

			if (cls != null)
			{
				WriteLine("class : " + cls.Name);
                foreach (AttributeInfo attr in cls.Attributes)
                {
					WriteLine("\tattribute : " + attr.FullName);
                    foreach (AttributeArgumentInfo arg in attr.Arguments)
						WriteLine("\t\t" + (arg.Name  + " "  + arg.Value).Trim());
                }
			}
		}
	}
...
 ```


 ### Parse interfaces

```c#

 	// get a reference to solution
	NodeSolution sln = Solution();

	// get all object of type NodeItem (working file *.cs, *.vb, ...)
	List<NodeItem> list = sln.GetItem<NodeItem>().ToList();

	foreach(NodeItem item in list)
	{

		// parse the list of code objects from 
		foreach(InterfaceInfo i in item.GetClassItems<InterfaceInfo>())
		{

				WriteLine("interfaces : " + i.Name);

                ...

		}

	}

 ```

 ### Parse generic arguments of the class

 ```c#

 	NodeSolution sln = Solution();

	// get all object of type NodeItem (working file *.cs, *.vb, ...)
	List<NodeItem> list = sln.GetItem<NodeItem>().ToList();

	foreach(NodeItem item in list)
	{

		// parse the list of code objects from 
		foreach(ClassInfo cls in item.GetClassItems<ClassInfo>(c => c.IsGeneric))
		{
		
			WriteLine("class : " + cls.FullName);
            foreach (GenericArgument gen in cls.GenericArguments)
            {

				Write("\twhere " + gen.Name + ":" );
		 
				if (gen.IsClass)
					WriteLine(" class");

				bool t = false;

                foreach (var cons in gen.Constraints)
                {
					if (t)
						Write(",");

					Write(" " + cons);

					t = true;
                }


				if (gen.HasEmptyConstructor)
                {
					if (t)
						Write(",");

					WriteLine(" new()");
                }

				WriteLine("");
            }
		}

	}

```



#### Author
  **Gaël, Beard** 
  (gaelgael5@gmail.com)<br /> 
  Architect by pickup<br /> 

  Copyright 2015 <br /> 
