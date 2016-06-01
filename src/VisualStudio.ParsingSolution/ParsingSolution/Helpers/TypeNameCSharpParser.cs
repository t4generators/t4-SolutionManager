using System;
using System.Collections.Generic;
using System.Linq;

// Copyright Christophe Bertrand.

namespace VisualStudio.ParsingSolution
{


    /// <summary>
    /// type info reference
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{ToString()}")]
    public class TypeNameCSharpParser
    {

        private List<TypeNameCSharpParser> _list = new List<TypeNameCSharpParser>();
        private int _rank = 0;

        public TypeNameCSharpParser(string type)
        {
            Set(type);
        }

        TypeNameCSharpParser()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        private void Set(string type)
        {
            this.Name = type;
            this._list = new List<TypeNameCSharpParser>();
            this._rank = 0;
            int i = 0;
            Parse(Name, this, ref i);
        }

        private void Parse(string type, TypeNameCSharpParser t, ref int p)
        {


            System.Text.StringBuilder s = new System.Text.StringBuilder();
            for (int i = p; i < type.Length; i++)
            {

                deb:
                var c = type[i];
                switch (c)
                {

                    case '<':
                        i++;
                        p = i;
                        t.Name = s.ToString();
                        var t2 = new TypeNameCSharpParser();
                        t.Add(t2);
                        Parse(type, t2, ref p);
                        i = p;

                        if (s.ToString() == "Nullable" || s.ToString() == "System.Nullable")
                            this.IsNullable = true;

                        if (i < type.Length)
                        {

                            c = type[p];

                            if (c != ',')
                                goto deb;

                            while ((c = type[p]) == ',')
                            {
                                p++;
                                t2 = new TypeNameCSharpParser();
                                t.Add(t2);
                                Parse(type, t2, ref p);
                                i = p;
                                if (i >= type.Length)
                                    return;
                            }

                            c = type[p];

                            if (c != ',')
                                goto deb;

                        }

                        return;

                    case '>':
                        p = i;
                        p++;
                        t.Name = s.ToString();
                        return;

                    case '[':
                        t.Name = s.ToString();
                        p = i;
                        t.ParseArray(type, ref p);
                        i = p;
                        break;

                    case ',':
                        t.Name = s.ToString();
                        p = i;
                        return;

                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                        break;


                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case 'a':
                    case 'b':
                    case 'c':
                    case 'd':
                    case 'e':
                    case 'f':
                    case 'g':
                    case 'h':
                    case 'i':
                    case 'j':
                    case 'k':
                    case 'l':
                    case 'm':
                    case 'n':
                    case 'o':
                    case 'p':
                    case 'q':
                    case 'r':
                    case 's':
                    case 't':
                    case 'u':
                    case 'v':
                    case 'w':
                    case 'x':
                    case 'y':
                    case 'z':
                    case 'A':
                    case 'B':
                    case 'C':
                    case 'D':
                    case 'E':
                    case 'F':
                    case 'G':
                    case 'H':
                    case 'I':
                    case 'J':
                    case 'K':
                    case 'L':
                    case 'M':
                    case 'N':
                    case 'O':
                    case 'P':
                    case 'Q':
                    case 'R':
                    case 'S':
                    case 'T':
                    case 'U':
                    case 'V':
                    case 'W':
                    case 'X':
                    case 'Y':
                    case 'Z':
                    case '.':
                    case '_':
                        s.Append(c);
                        break;

                    case '?':
                        t.IsNullable = true;
                        s.Append(c);
                        break;
                    default:
                        throw new FormatException(type);

                }

            }

        }

        private void ParseArray(string type, ref int p)
        {
            for (int i = p + 1; i < type.Length; i++)
            {
                p = i;
                _rank++;
                var c = type[i];
                if (c == ']')
                    break;
            }
        }

        private void Add(TypeNameCSharpParser t2)
        {
            this._list.Add(t2);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsGeneric { get { return this.ElementItems.Any(); } }

        /// <summary>
        /// 
        /// </summary>
        public int Rank { get { return _rank; } }

        /// <summary>
        /// 
        /// </summary>
        public string ClassName
        {
            get
            {
                var i = Name.LastIndexOf('.');
                if (i > 0)
                    return Name.Substring(i + 1);

                return Name;

            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<TypeNameCSharpParser> ElementItems { get { return this._list; } }

        /// <summary>
        /// 
        /// </summary>
        public override string ToString()
        {

            System.Text.StringBuilder s = new System.Text.StringBuilder();

            s.Append(Name);

            if (this.ElementItems.Count() > 0)
            {

                s.Append("<");
                bool a = false;

                foreach (var item in this.ElementItems)
                {
                    if (a)
                        s.Append(", ");
                    s.Append(item.ToString());
                    a = true;
                }

                s.Append(">");

            }

            if (_rank > 0)
            {
                s.Append("[");
                for (int m = 1; m < _rank; m++)
                    s.Append(",");
                s.Append("]");
            }

            return s.ToString();

        }

    }


}
