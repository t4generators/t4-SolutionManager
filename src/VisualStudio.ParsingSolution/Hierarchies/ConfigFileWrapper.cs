using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;
using System.Diagnostics;
using VsxFactory.Modeling.VisualStudio;
using System.IO;

namespace VsxFactory.Modeling
{
    public class XmlFileWrapper
    {
        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        /// <value>The name of the file.</value>
        public string FileName { get; private set; }
        /// <summary>
        /// Gets or sets the document.
        /// </summary>
        /// <value>The document.</value>
        public XmlDocument Document { get; private set; }
        /// <summary>
        /// Gets or sets the owner project.
        /// </summary>
        /// <value>The owner project.</value>
        public IVsProject OwnerProject { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigFileWrapper"/> class.
        /// </summary>
        /// <param name="ownerProject">The owner project.</param>
        /// <param name="doc">The doc.</param>
        /// <param name="fileName">Name of the file.</param>
        public XmlFileWrapper(XmlDocument doc, string fileName=null, IVsProject ownerProject=null)
        {
            Guard.ArgumentNotNull(doc, "doc");

            Document = doc;
            FileName = fileName;
            OwnerProject = ownerProject;
        }

        /// <summary>
        /// Saves this instance.
        /// </summary>
        public void Save()
        {
            if (!String.IsNullOrEmpty(FileName))
            {
                if (OwnerProject != null)
                {
                    var node = OwnerProject.FindByPath<IVsProjectItem>(FileName);
                    if (node != null)
                    {
                        StringBuilder sb = new StringBuilder();
                        using (var writer = new StringWriter(sb))
                        {
                            Document.Save(writer);
                            OwnerProject.AddItem(FileName, GetEncoding( Document ).GetBytes( sb.ToString()));
                        }
                        //node.EnsureCheckout();
                        return;
                    }
                }
                Document.Save(FileName);
            }
            else
                throw new Exception("No filename");
        }

        private Encoding GetEncoding(XmlDocument xdoc)
        {
            if (xdoc.HasChildNodes)
            {
                var dcl = xdoc.FirstChild as XmlDeclaration;
                if (dcl != null)
                {
                    var encoding = dcl.Encoding;
                    if (!String.IsNullOrEmpty(encoding))
                        return Encoding.GetEncoding(encoding);
                }
            }
            return Encoding.UTF8;
        }

        /// <summary>
        /// Ensures the XML element.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="attributes">The attributes.</param>
        /// <returns></returns>
        public XmlNode EnsureXmlElement(string path, Dictionary<string, string> attributes)
        {
            return EnsureXmlElement(path, attributes, false, null, null);
        }

        /// <summary>
        /// Ensures the XML element.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="prepend">if set to <c>true</c> [prepend].</param>
        public XmlNode EnsureXmlElement(string path, Dictionary<string, string> attributes, bool prepend)
        {
            return EnsureXmlElement(path, attributes, prepend, null, null);
        }

        /// <summary>
        /// Ensures the XML element.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="prepend">if set to <c>true</c> [prepend].</param>
        /// <returns></returns>
        public XmlNode EnsureXmlElement(string path, Dictionary<string, string> attributes, bool prepend, string xmlns, string nodeReferencePath)
        {
            XmlNode newChild = null;
            XmlNode parentNode = Document;

            string prefix = String.Empty;
            XmlNamespaceManager ns = new XmlNamespaceManager(Document.NameTable);
            if (!String.IsNullOrEmpty(xmlns))
            {
                ns.AddNamespace("NS", xmlns);
                prefix = "NS:";
            }
            else
                xmlns = string.Empty;

            XmlNode nodeReference = null;
            if (!String.IsNullOrEmpty(nodeReferencePath))
            {
                nodeReference = Document.SelectSingleNode(nodeReferencePath, ns);
            }

            List<string> parts = SplitPath(path);
            foreach (string str in parts)
            {
                newChild = parentNode.SelectSingleNode(prefix + str, ns);

                if (newChild == null)
                {
                    string tagName;
                    string attributName;
                    string attributValue;
                    ParseExpression(str, out tagName, out attributName, out attributValue);
                    newChild = parentNode.OwnerDocument.CreateElement(tagName, xmlns);
                    if (nodeReference != null)
                    {
                        if (prepend)
                            parentNode.InsertBefore(newChild, nodeReference);
                        else
                            parentNode.InsertAfter(newChild, nodeReference);
                    }
                    else if (prepend)
                    {
                        parentNode.PrependChild(newChild);
                    }
                    else
                    {
                        parentNode.AppendChild(newChild);
                    }

                    if (attributName != null)
                    {
                        var att = Document.CreateAttribute(attributName);
                        System.Diagnostics.Debug.Assert(attributValue!=null, "incorrect path syntax");
                        att.Value = attributValue;
                        newChild.Attributes.Append(att);
                    }
                }
                parentNode = newChild;
            }

            if (attributes != null)
            {
                foreach (var attribute in attributes)
                {
                    var a = newChild.Attributes[attribute.Key];
                    if (a == null)
                    {
                        a = Document.CreateAttribute(attribute.Key);
                        newChild.Attributes.Append(a);
                    }
                    if (a.Value != attribute.Value)
                    {
                        a.Value = attribute.Value;
                    }
                }
            }

            return newChild;
        }

        /// <summary>
        /// Parses the expression.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <param name="tagName">Name of the tag.</param>
        /// <param name="attributName">Name of the attribut.</param>
        /// <param name="attributValue">The attribut value.</param>
        private void ParseExpression(string str, out string tagName, out string attributName, out string attributValue)
        {
            StringBuilder sb = new StringBuilder();
            attributValue = attributName = tagName = null;
            for (int i = 0; i < str.Length; i++)
            {
                char ch = str[i];
                if (ch == '[')
                {
                    tagName = sb.ToString();
                    sb.Length = 0;
                }
                else if (ch == '=')
                {
                    Debug.Assert(tagName != null);
                    attributName = sb.ToString();
                    sb.Length = 0;
                }
                else if (ch == ']')
                {
                    Debug.Assert(attributName != null);
                    attributValue = sb.ToString();
                    sb.Length = 0;
                }
                else if (ch != '@' && ch != '"' && ch != '\'')
                    sb.Append(ch);
            }
            if (sb.Length > 0 && tagName == null)
                tagName = sb.ToString();
        }

        /// <summary>
        /// Splits the path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        private List<string> SplitPath(string path)
        {
            List<string> parts = new List<string>();
            StringBuilder part = new StringBuilder();
            bool inComment = false;
            path += '/'; // Pour simplifier l'algorithme
            for (int i = 0; i < path.Length; i++)
            {
                char ch = path[i];
                if (ch == '"')
                {
                    inComment = !inComment;
                }
                if (ch == '/' && !inComment)
                {
                    if (part.Length > 0)
                    {
                        parts.Add(part.ToString());
                        part.Length = 0;
                    }
                }
                else
                    part.Append(ch);
            }
            return parts;
        }


        public void EnsureDeleteXmlElement(string path)
        {
            EnsureDeleteXmlElement(path, null);
        }

        /// <summary>
        /// Ensures the XML element is deleted.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public bool EnsureDeleteXmlElement(string path, string xmlns)
        {
            XmlNamespaceManager ns = new XmlNamespaceManager(Document.NameTable);
            if (!String.IsNullOrEmpty(xmlns))
            {
                ns.AddNamespace("NS", xmlns);
            }

            bool modified = false;
            XmlNode parentNode = Document;
            var newChild = parentNode.SelectSingleNode(path, ns);

            if (newChild != null)
            {
                modified = true;
                parentNode = newChild.ParentNode;
                parentNode.RemoveChild(newChild);
                while (parentNode != null && parentNode.ChildNodes.Count == 0 && parentNode.Name != "configuration")
                {
                    newChild = parentNode;
                    parentNode = newChild.ParentNode;
                    parentNode.RemoveChild(newChild);
                }
            }     

            return modified;
        }

        /// <summary>
        /// Creates the specified file name.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns></returns>
        public static XmlFileWrapper Create(IVsProject ownerProject, string fileName)
        {
            Guard.ArgumentNotNullOrEmptyString(fileName, "fileName");
            Guard.ArgumentNotNull(ownerProject, "ownerProject");

            try
            {
                XmlDocument xdoc = new XmlDocument();
                xdoc.Load(fileName);
                return new XmlFileWrapper(xdoc, fileName, ownerProject);
            }
            catch
            {
                return null;
            }
        }
    }
}
