using System;
using System.Collections.Generic;
using System.Windows.Forms;
using EnvDTE;

namespace VsxFactory.Modeling.VisualStudio.Synchronization
{
 /// <summary>
 /// Class used to choose a type among other types
 /// </summary>
 public partial class TypeChooser : Form
 {
  /// <summary>
  /// Constructor
  /// </summary>
  public TypeChooser()
  {
   InitializeComponent();
  }

  /// <summary>
  /// Choices of types
  /// </summary>
  public List<CodeType> Choices
  {
   set
   {
    choices = value;
    listView.Items.Clear();
    foreach (CodeType t in value)
     listView.Items.Add(t.FullName.Replace("+", "."));
   }
   get
   {
    return choices;
   }
  }
  List<CodeType> choices = new List<CodeType>();

  /// <summary>
  /// Selected choice
  /// </summary>
  public CodeType SelectedChoice
  {
   get
   {
    if (listView.SelectedItems.Count == 1)
     return choices[listView.SelectedItems[0].Index];
    else
     return null;
   }
   set
   {
    listView.SelectedIndices.Clear();
    listView.SelectedIndices.Add(choices.IndexOf(value));
   }
  }
 }
}