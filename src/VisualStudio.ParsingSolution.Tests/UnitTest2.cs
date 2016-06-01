using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VisualStudio.ParsingSolution.Tests
{
    [TestClass]
    public class UnitTest2
    {


        [TestMethod]
        public void TestTypeStringStandard()
        {
            var typeName = typeof(string).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            TypeNameCSharpParser p2 = new TypeNameCSharpParser(typeResult);
            var typeResult2 = p2.ToString();
            Assert.AreEqual(typeResult, typeResult2);
        }

        [TestMethod]
        public void TestTypeStringArray1()
        {
            var typeName = typeof(string[]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            TypeNameCSharpParser p2 = new TypeNameCSharpParser(typeResult);
            var typeResult2 = p2.ToString();
            Assert.AreEqual(typeResult, typeResult2);
        }

        [TestMethod]
        public void TestTypeStringArray2()
        {
            var typeName = typeof(string[,,]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            TypeNameCSharpParser p2 = new TypeNameCSharpParser(typeResult);
            var typeResult2 = p2.ToString();
            Assert.AreEqual(typeResult, typeResult2);
        }

        [TestMethod]
        public void TestTypeGenericStandard()
        {
            var typeName = typeof(IEquatable<ulong>).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            TypeNameCSharpParser p2 = new TypeNameCSharpParser(typeResult);
            var typeResult2 = p2.ToString();
            Assert.AreEqual(typeResult, typeResult2);
        }


        [TestMethod]
        public void TestTypeGenericNullableStandard()
        {
            var typeName = typeof(Nullable<ulong>).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            TypeNameCSharpParser p2 = new TypeNameCSharpParser(typeResult);
            var typeResult2 = p2.ToString();
            Assert.AreEqual(typeResult, typeResult2);
        }


    }
}
