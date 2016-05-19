﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

// Copyright Christophe Bertrand.

namespace VisualStudio.ParsingSolution
{

    public class ParsedAssemblyQualifiedName
    {

        private string _name;
        private string _namespace;

        public Lazy<AssemblyName> AssemblyNameDescriptor;
        public Lazy<Type> FoundType;

        public string Namespace
        {
            get
            {
                if (_namespace == null)
                {

                    StringBuilder sb = new StringBuilder();

                    for (int i = 0; i < TypeName.Length; i++)
                    {

                        char c = TypeName[i];

                        if (c == ' ' || c == '`' || c == '+')
                            break;

                        sb.Append(c);

                    }

                    this._namespace = sb.ToString();

                    int l = this._namespace.LastIndexOf('.');
                    if (l > 0)
                        this._namespace = this._namespace.Substring(0, l);

                }

                return _namespace;

            }
        }

        public string Name
        {
            get
            {

                if (_name == null)
                {
                    var n = Namespace;
                    _name = TypeName.Substring(n.Length, TypeName.Length - n.Length);
                    _name = _name.TrimStart('.');

                    StringBuilder sb = new StringBuilder();

                    for (int i = 0; i < _name.Length; i++)
                    {

                        char c = _name[i];

                        if (c == '`')
                        {
                            sb.Append(c);
                            while (char.IsDigit(c = _name[++i]))
                                sb.Append(c);
                            break;
                        }

                        if (c == ' ' || c == '+')
                            break;

                        sb.Append(c);

                    }

                    this._name = sb.ToString();

                }

                return _name;

            }
        }

        public bool IsValid { get; private set; }
        public string AssemblyDescriptionString { get; private set; }
        public string TypeName { get; private set; }
        public string Path { get; private set; }
        public bool IsRef { get; private set; }
        public bool IsArray { get; private set; }
        public int RankArray { get; private set; }
        public bool IsPointer { get; private set; }
        public string ShortAssemblyName { get; private set; }
        public string Version { get; private set; }
        public string Culture { get; private set; }
        public string PublicKeyToken { get; private set; }

        public List<ParsedAssemblyQualifiedName> GenericParameters = new List<ParsedAssemblyQualifiedName>();
        public Lazy<string> CSharpStyleName;
        public Lazy<string> VBNetStyleName;

        public ParsedAssemblyQualifiedName(string AssemblyQualifiedName)
        {

            IsValid = false;

            if (string.IsNullOrEmpty(AssemblyQualifiedName))
                return;

            try
            {
                Parse(AssemblyQualifiedName);
                IsValid = true;
            }
            catch (Exception)
            {

            }

        }


        private void Parse(string AssemblyQualifiedName)
        {

            int index = -1;
            block rootBlock = new block();

            int bcount = 0;
            block currentBlock = rootBlock;
            for (int i = 0; i < AssemblyQualifiedName.Length; ++i)
            {
                char c = AssemblyQualifiedName[i];
                if (c == '[')
                {
                    if (AssemblyQualifiedName[i + 1] == ']') // Array type.
                        i++;
                    else
                    {
                        ++bcount;
                        var b = new block() { iStart = i + 1, level = bcount, parentBlock = currentBlock };
                        currentBlock.innerBlocks.Add(b);
                        currentBlock = b;
                    }
                }
                else if (c == ']')
                {
                    currentBlock.iEnd = i - 1;
                    if (AssemblyQualifiedName[currentBlock.iStart] != '[')
                    {
                        currentBlock.parsedAssemblyQualifiedName = new ParsedAssemblyQualifiedName(AssemblyQualifiedName.Substring(currentBlock.iStart, i - currentBlock.iStart));
                        if (bcount == 2)
                            this.GenericParameters.Add(currentBlock.parsedAssemblyQualifiedName);
                    }
                    currentBlock = currentBlock.parentBlock;
                    --bcount;
                }
                else if (bcount == 0 && c == ',')
                {
                    index = i;
                    break;
                }
            }


            this.TypeName = AssemblyQualifiedName.Substring(0, index);

            if (this.TypeName.EndsWith("&"))
            {
                this.TypeName = this.TypeName.Substring(0, this.TypeName.Length - 1);
                this.IsRef = true;
            }

            if (this.TypeName.EndsWith("*"))
            {
                this.TypeName = this.TypeName.Substring(0, this.TypeName.Length - 1);
                this.IsPointer = true;
            }

            //if (this.TypeName.EndsWith("]"))
            //{
            //    this.IsArray = true;
            //    this.TypeName = this.TypeName.Substring(0, this.TypeName.Length - 1);
            //    this.RankArray++;

            //    while (true)
            //    {
            //        if (this.TypeName.EndsWith(","))
            //        {
            //            this.TypeName = this.TypeName.Substring(0, this.TypeName.Length - 1);
            //            this.RankArray++;
            //        }
            //        else if (this.TypeName.EndsWith("["))
            //        {
            //            this.TypeName = this.TypeName.Substring(0, this.TypeName.Length - 1);
            //            break;
            //        }
            //    }

            //}

            this.CSharpStyleName = new Lazy<string>(
                () =>
                {
                    return this.LanguageStyle("<", ">");
                });

            this.VBNetStyleName = new Lazy<string>(
                () =>
                {
                    return this.LanguageStyle("(Of ", ")");
                });

            this.AssemblyDescriptionString = AssemblyQualifiedName.Substring(index + 2);

            {
                List<string> parts = AssemblyDescriptionString.Split(',')
                                                                 .Select(x => x.Trim())
                                                                 .ToList();
                this.Version = LookForPairThenRemove(parts, "Version");
                this.Culture = LookForPairThenRemove(parts, "Culture");
                this.Path = LookForPairThenRemove(parts, "Path");
                this.PublicKeyToken = LookForPairThenRemove(parts, "PublicKeyToken");
                if (parts.Count > 0)
                    this.ShortAssemblyName = parts[0];
            }

            this.AssemblyNameDescriptor = new Lazy<AssemblyName>(
                () => new System.Reflection.AssemblyName(this.AssemblyDescriptionString));

            this.FoundType = new Lazy<Type>(
                () =>
                {
                    var searchedType = Type.GetType(AssemblyQualifiedName);
                    if (searchedType != null)
                        return searchedType;
                    foreach (var assem in Assemblies.Value)
                    {
                        searchedType =
                            assem.GetType(AssemblyQualifiedName);
                        if (searchedType != null)
                            return searchedType;
                    }
                    return null; // Not found.
                });


        }


        internal string LanguageStyle(string prefix, string suffix)
        {
            if (this.GenericParameters.Count > 0)
            {
                StringBuilder sb = new StringBuilder(this.TypeName.Substring(0, this.TypeName.IndexOf('`')));
                sb.Append(prefix);
                bool pendingElement = false;
                foreach (var param in this.GenericParameters)
                {
                    if (pendingElement)
                        sb.Append(", ");
                    sb.Append(param.LanguageStyle(prefix, suffix));
                    pendingElement = true;
                }
                sb.Append(suffix);
                return sb.ToString();
            }
            else
                return this.TypeName;
        }

        class block
        {
            internal int iStart;
            internal int iEnd;
            internal int level;
            internal block parentBlock;
            internal List<block> innerBlocks = new List<block>();
            internal ParsedAssemblyQualifiedName parsedAssemblyQualifiedName;
        }

        static string LookForPairThenRemove(List<string> strings, string Name)
        {
            for (int istr = 0; istr < strings.Count; istr++)
            {
                string s = strings[istr];
                int i = s.IndexOf(Name);
                if (i == 0)
                {
                    int i2 = s.IndexOf('=');
                    if (i2 > 0)
                    {
                        string ret = s.Substring(i2 + 1);
                        strings.RemoveAt(istr);
                        return ret;
                    }
                }
            }
            return null;
        }

        static readonly Lazy<Assembly[]> Assemblies = new Lazy<Assembly[]>(() => AppDomain.CurrentDomain.GetAssemblies());

#if DEBUG
        // Makes debugging easier.
        public override string ToString()
        {
            return this.CSharpStyleName.ToString();
        }
#endif

    }



}
