using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VisualStudio.ParsingSolution.Tests
{
    [TestClass]
    public class UnitTest1
    {

        #region string

        [TestMethod]
        public void TestTypeStringStandard()
        {
            var typeName = typeof(string).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.String", typeResult);
        }

        [TestMethod]
        public void TestTypeStringArray1()
        {
            var typeName = typeof(string[]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.String[]", typeResult);
        }

        [TestMethod]
        public void TestTypeStringArray2()
        {
            var typeName = typeof(string[,,]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.String[,,]", typeResult);
        }

        [TestMethod]
        public void TestTypeStringStandardSystem()
        {
            var typeName = typeof(string).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("string", typeResult);
        }

        [TestMethod]
        public void TestTypeStringArray1System()
        {
            var typeName = typeof(string[]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("string[]", typeResult);
        }

        [TestMethod]
        public void TestTypeStringArray2System()
        {
            var typeName = typeof(string[,,]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("string[,,]", typeResult);
        }

        #endregion string

        #region decimal

        [TestMethod]
        public void TestTypedecimalStandard()
        {
            var typeName = typeof(decimal).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.Decimal", typeResult);
        }

        [TestMethod]
        public void TestTypedecimalArray1()
        {
            var typeName = typeof(decimal[]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.Decimal[]", typeResult);
        }

        [TestMethod]
        public void TestTypedecimalArray2()
        {
            var typeName = typeof(decimal[,,]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.Decimal[,,]", typeResult);
        }

        [TestMethod]
        public void TestTypedecimalStandardSystem()
        {
            var typeName = typeof(decimal).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("decimal", typeResult);
        }


        [TestMethod]
        public void TestTypedecimalArray1System()
        {
            var typeName = typeof(decimal[]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("decimal[]", typeResult);
        }

        [TestMethod]
        public void TestTypedecimalArray2System()
        {
            var typeName = typeof(decimal[,,]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("decimal[,,]", typeResult);
        }

        #endregion decimal

        #region float

        [TestMethod]
        public void TestTypefloatStandard()
        {
            var typeName = typeof(float).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.Single", typeResult);
        }

        [TestMethod]
        public void TestTypefloatArray1()
        {
            var typeName = typeof(float[]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.Single[]", typeResult);
        }

        [TestMethod]
        public void TestTypefloatArray2()
        {
            var typeName = typeof(float[,,]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.Single[,,]", typeResult);
        }

        [TestMethod]
        public void TestTypefloatStandardSystem()
        {
            var typeName = typeof(float).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("float", typeResult);
        }

        [TestMethod]
        public void TestTypefloatArray1System()
        {
            var typeName = typeof(float[]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("float[]", typeResult);
        }

        [TestMethod]
        public void TestTypefloatArray2System()
        {
            var typeName = typeof(float[,,]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("float[,,]", typeResult);
        }

        #endregion float

        #region double

        [TestMethod]
        public void TestTypedoubleStandard()
        {
            var typeName = typeof(double).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.Double", typeResult);
        }

        [TestMethod]
        public void TestTypedoubleArray1()
        {
            var typeName = typeof(double[]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.Double[]", typeResult);
        }

        [TestMethod]
        public void TestTypedoubleArray2()
        {
            var typeName = typeof(double[,,]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.Double[,,]", typeResult);
        }

        [TestMethod]
        public void TestTypedoubleStandardSystem()
        {
            var typeName = typeof(double).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("double", typeResult);
        }

        [TestMethod]
        public void TestTypedoubleArray1System()
        {
            var typeName = typeof(double[]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("double[]", typeResult);
        }

        [TestMethod]
        public void TestTypedoubleArray2System()
        {
            var typeName = typeof(double[,,]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("double[,,]", typeResult);
        }

        #endregion double

        #region short

        [TestMethod]
        public void TestTypeshortStandard()
        {
            var typeName = typeof(short).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.Int16", typeResult);
        }

        [TestMethod]
        public void TestTypeshortArray1()
        {
            var typeName = typeof(short[]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.Int16[]", typeResult);
        }

        [TestMethod]
        public void TestTypeshortArray2()
        {
            var typeName = typeof(short[,,]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.Int16[,,]", typeResult);
        }

        [TestMethod]
        public void TestTypeshortStandardSystem()
        {
            var typeName = typeof(short).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("short", typeResult);
        }

        [TestMethod]
        public void TestTypeshortArray1System()
        {
            var typeName = typeof(short[]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("short[]", typeResult);
        }

        [TestMethod]
        public void TestTypeshortArray2System()
        {
            var typeName = typeof(short[,,]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("short[,,]", typeResult);
        }

        #endregion short

        #region ushort

        [TestMethod]
        public void TestTypeushortStandard()
        {
            var typeName = typeof(ushort).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.UInt16", typeResult);
        }

        [TestMethod]
        public void TestTypeushortArray1()
        {
            var typeName = typeof(ushort[]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.UInt16[]", typeResult);
        }

        [TestMethod]
        public void TestTypeushortArray2()
        {
            var typeName = typeof(ushort[,,]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.UInt16[,,]", typeResult);
        }

        [TestMethod]
        public void TestTypeushortStandardSystem()
        {
            var typeName = typeof(ushort).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("ushort", typeResult);
        }

        [TestMethod]
        public void TestTypeushortArray1System()
        {
            var typeName = typeof(ushort[]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("ushort[]", typeResult);
        }

        [TestMethod]
        public void TestTypeushortArray2System()
        {
            var typeName = typeof(ushort[,,]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("ushort[,,]", typeResult);
        }

        #endregion ushort

        #region int

        [TestMethod]
        public void TestTypeintStandard()
        {
            var typeName = typeof(int).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.Int32", typeResult);
        }

        [TestMethod]
        public void TestTypeintArray1()
        {
            var typeName = typeof(int[]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.Int32[]", typeResult);
        }

        [TestMethod]
        public void TestTypeintArray2()
        {
            var typeName = typeof(int[,,]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.Int32[,,]", typeResult);
        }

        [TestMethod]
        public void TestTypeintStandardSystem()
        {
            var typeName = typeof(int).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("int", typeResult);
        }

        [TestMethod]
        public void TestTypeintArray1System()
        {
            var typeName = typeof(int[]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("int[]", typeResult);
        }

        [TestMethod]
        public void TestTypeintArray2System()
        {
            var typeName = typeof(int[,,]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("int[,,]", typeResult);
        }

        #endregion int

        #region uint

        [TestMethod]
        public void TestTypeuintStandard()
        {
            var typeName = typeof(uint).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.UInt32", typeResult);
        }

        [TestMethod]
        public void TestTypeuintArray1()
        {
            var typeName = typeof(uint[]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.UInt32[]", typeResult);
        }

        [TestMethod]
        public void TestTypeuintArray2()
        {
            var typeName = typeof(uint[,,]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.UInt32[,,]", typeResult);
        }

        [TestMethod]
        public void TestTypeuintStandardSystem()
        {
            var typeName = typeof(uint).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("uint", typeResult);
        }

        [TestMethod]
        public void TestTypeuintArray1System()
        {
            var typeName = typeof(uint[]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("uint[]", typeResult);
        }

        [TestMethod]
        public void TestTypeuintArray2System()
        {
            var typeName = typeof(uint[,,]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("uint[,,]", typeResult);
        }

        #endregion uint

        #region long

        [TestMethod]
        public void TestTypelongStandard()
        {
            var typeName = typeof(long).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.Int64", typeResult);
        }

        [TestMethod]
        public void TestTypelongArray1()
        {
            var typeName = typeof(long[]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.Int64[]", typeResult);
        }

        [TestMethod]
        public void TestTypelongArray2()
        {
            var typeName = typeof(long[,,]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.Int64[,,]", typeResult);
        }

        [TestMethod]
        public void TestTypelongStandardSystem()
        {
            var typeName = typeof(long).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("long", typeResult);
        }

        [TestMethod]
        public void TestTypelongArray1System()
        {
            var typeName = typeof(long[]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("long[]", typeResult);
        }

        [TestMethod]
        public void TestTypelongArray2System()
        {
            var typeName = typeof(long[,,]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("long[,,]", typeResult);
        }

        #endregion long

        #region ulong

        [TestMethod]
        public void TestTypeulongStandard()
        {
            var typeName = typeof(ulong).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.UInt64", typeResult);
        }

        [TestMethod]
        public void TestTypeulongArray1()
        {
            var typeName = typeof(ulong[]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.UInt64[]", typeResult);
        }

        [TestMethod]
        public void TestTypeulongArray2()
        {
            var typeName = typeof(ulong[,,]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.UInt64[,,]", typeResult);
        }

        [TestMethod]
        public void TestTypeulongStandardSystem()
        {
            var typeName = typeof(ulong).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("ulong", typeResult);
        }

        [TestMethod]
        public void TestTypeulongArray1System()
        {
            var typeName = typeof(ulong[]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("ulong[]", typeResult);
        }

        [TestMethod]
        public void TestTypeulongArray2System()
        {
            var typeName = typeof(ulong[,,]).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("ulong[,,]", typeResult);
        }

        #endregion ulong

        [TestMethod]
        public void TestTypeGenericStandard()
        {
            var typeName = typeof(IEquatable<ulong>).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp();
            Assert.AreEqual("System.IEquatable<System.UInt64>", typeResult);
        }

        [TestMethod]
        public void TestTypeGenericSystemStandard()
        {
            var typeName = typeof(IEquatable<ulong>).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("System.IEquatable<ulong>", typeResult);
        }

        [TestMethod]
        public void TestTypeGeneric2Standard()
        {
            var typeName = typeof(System.Collections.Generic.List<IEquatable<ulong>>).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("System.Collections.Generic.List<System.IEquatable<ulong>>", typeResult);
        }

        [TestMethod]
        public void TestTypeGenericWithArrayStandard()
        {
            var typeName = typeof(System.Collections.Generic.List<IEquatable<ulong[,]>>).AssemblyQualifiedName;
            ParsedAssemblyQualifiedName parser = new ParsedAssemblyQualifiedName(typeName);
            var typeResult = parser.ToCSharp(FormatRule.System);
            Assert.AreEqual("System.Collections.Generic.List<System.IEquatable<ulong[,]>>", typeResult);
        }

    }
}
