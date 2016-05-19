using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudio.ParsingSolution
{
    public class ReferenceAssembly
    {

        internal ReferenceAssembly(string ass, Microsoft.Build.Construction.ProjectElement child)
        {
            // Microsoft.VisualStudio.ComponentModelHost, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=MSIL"

            this.Fullname = ass;

            var ar = ass.Split(',');
            foreach (string txt in ar)
            {

                if (txt.Contains("="))
                {

                    switch (txt)
                    {

                        case "Version":
                            this.Version = txt.Trim().Split('=')[1].Trim();
                            break;

                        case "Culture":
                            this.Culture = txt.Trim().Split('=')[1].Trim();
                            break;

                        case "PublicKeyToken":
                            this.PublicKeyToken = txt.Trim().Split('=')[1].Trim();
                            break;

                        case "processorArchitecture":
                            this.ProcessorArchitecture = txt.Trim().Split('=')[1].Trim();
                            break;

                    }

                }
                else
                {
                    this.Name = txt.Trim();
                }

                if (child != null)
                {
                    var FileProject = child.ContainingProject.Location.File;
                    FileInfo file1 = new FileInfo(FileProject);
                    if (file1.Exists)
                    {
                        var metadata = child as Microsoft.Build.Construction.ProjectMetadataElement;
                        if (metadata != null)
                        {
                            var file = Path.Combine(file1.Directory.FullName, metadata.Value);
                            if (System.IO.File.Exists(file))
                            {
                                FileInfo f = new FileInfo(file);
                                this.File = f.FullName;
                            }
                        }
                    }
                }




            }

        }

        public string Name { get; private set; }

        public string Version { get; private set; }

        public string Culture { get; private set; }

        public string PublicKeyToken { get; private set; }

        public string ProcessorArchitecture { get; private set; }

        public string File { get; private set; }
        public string Fullname { get; private set; }
    }


}
