using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AsmDiff.Lib;
using Mono.Cecil;
using System.Reflection;
using Microsoft.CSharp;
using System.CodeDom.Compiler;


namespace AsmDiff.Tests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class EngineTest
    {
        public EngineTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        private AssemblyDefinition Compile(string assmeblyName, string code)
        {
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();

            System.CodeDom.Compiler.CompilerParameters parameters =
 new CompilerParameters();
            parameters.GenerateInMemory = true;
            //Make sure we generate an DLL, not an EXE
            parameters.GenerateExecutable = false;
            parameters.OutputAssembly = assmeblyName;
            CompilerResults results =
 codeProvider.CompileAssemblyFromSource(parameters, code);
            Assert.AreEqual<int>(0, results.Errors.Count);
            return AssemblyDefinition.ReadAssembly(results.CompiledAssembly.ManifestModule.ScopeName);
            
        }

        [TestMethod]
        public void MethodswithAllModifiersShouldInResultSet()
        {
            var engine = new Engine();
            var code = @" class C {
                            public void SomePublicMethod(){}
                            private void SomePrivateMethod(){}
                            protected void SomeProtectedMethod(){}
                            internal void SomeInternalMethod(){}
                            static void SomeStaticMethod(){}
                            public virtual void SomeVirtualMethod(){}
                           
                            public override string ToString()
                            {
                                return base.ToString();
                            }
                          } ";
            var asm = Compile("EmptyClass.dll", code);
            var type = asm.Modules.First().GetType("C");

            var methods = engine.GetMethods(type);

            // 7 methods and one constructor
            Assert.AreEqual<int>(8, methods.Count());
        }

        [TestMethod]
        public void InnerClassesShouldNotAffectMethodCount()
        {
            var engine = new Engine();
            var code = @"  class C
                {


                    public void SomePublicMethod() { }
                    private void SomePrivateMethod() { }
                    protected void SomeProtectedMethod() { }
                    internal void SomeInternalMethod() { }
                    static void SomeStaticMethod() { }
                    public virtual void SomeVirtualMethod() { }

                    public override string ToString()
                    {
                        return base.ToString();
                    }

                    class CInner1
                    {
                        public void SomePublicMethod() { }
                        private void SomePrivateMethod() { }
                        protected void SomeProtectedMethod() { }
                        internal void SomeInternalMethod() { }
                        static void SomeStaticMethod() { }
                        public virtual void SomeVirtualMethod() { }

                        public override string ToString()
                        {
                            return base.ToString();
                        }
                    }
                }";
            var asm = Compile("EmptyClass.dll", code);
            var outerType = asm.Modules.First().GetType("C");
            var outerMethods = engine.GetMethods(outerType);

            // 7 methods and one constructor
            Assert.AreEqual<int>(8, outerMethods.Count());
        }

        [TestMethod]
        public void ClassWithoutMethodsShouldReturnConstructor()
        {
            var engine = new Engine();
            var code = @" class C {} ";
            var asm = Compile("EmptyClass.dll", code);
            var type = asm.Modules.First().GetType("C");
            var dic = engine.GetMethods(type);

            Assert.AreEqual<int>(1, dic.Count());
        }

        [TestMethod]
        public void GettersAndSettersShoulBeInResultSet()
        {
            var engine = new Engine();
            var code = @" class C { int Prop {get; set;}} ";
            var asm = Compile("EmptyClass.dll", code);
            var type = asm.Modules.First().GetType("C");
            var dic = engine.GetMethods(type);

            // constructor, getter and setter
            Assert.AreEqual<int>(3, dic.Count());
        }

        [TestMethod]
        public void GenericMethtodShouldNotBeConfusedWithNonGeneric()
        {
            var engine = new Engine();
            var code = @" class C {
                            public void SomePublicMethod(){}
                            public void SomePublicMethod<T>(){}
                          } ";
            var asm = Compile("EmptyClass.dll", code);
            var type = asm.Modules.First().GetType("C");
            var dic = engine.GetMethods(type);

            // 7 methods and one constructor
            Assert.AreEqual<int>(3, dic.Count());
        }

        
       
        [TestMethod]
        public void ConverToMethodDictionaryShouldReturnTheSameAmountOfMethods()
        {
            var engine = new Engine();

            var testPath = typeof(CoolLibrary.Test).Assembly.Location;
            var asm = Mono.Cecil.AssemblyDefinition.ReadAssembly(testPath);

            var type = asm.MainModule.GetType("CoolLibrary.Test");
            Assert.IsNotNull(type);
            var methods = engine.GetMethods(type);
            Assert.AreEqual<int>(20, methods.Count);
        }
    }
}
