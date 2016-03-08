using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudio.ParsingSolution
{
    public class ReferenceAssembly
    {

        public ReferenceAssembly(string ass)
        {
            // Microsoft.VisualStudio.ComponentModelHost, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=MSIL"

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

            }

        }

        public string Name { get; private set; }
        public string Version { get; private set; }
        public string Culture { get; private set; }
        public string PublicKeyToken { get; private set; }
        public string ProcessorArchitecture { get; private set; }

    }


}
