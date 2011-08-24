using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AsmDiff.Lib;
using Mono.Cecil;
using System.Reflection;
using Moq;

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

        [TestMethod]
        public void GetMethodsSholdReturnAllMethodsIncludingConstructorsGettersAndSetters()
        {
            var engine = new Engine();
            
            var testPath = typeof(CoolLibrary.Test).Assembly.Location;
            var asm = Mono.Cecil.AssemblyDefinition.ReadAssembly(testPath);
            
            var methods =   engine.GetMethods(asm);

       
            Assert.AreEqual<int>(22, methods.Count());
            
        }
        [TestMethod]
        [Ignore]
        public void ConverToMethodDictionaryShouldReturnTheSameAmountOfMethods()
        {
            var engine = new Engine();

            var testPath = typeof(CoolLibrary.Test).Assembly.Location;
            var asm = Mono.Cecil.AssemblyDefinition.ReadAssembly(testPath);

            var methods = engine.ConvertToMethodDictionary(asm);
            Assert.AreEqual<int>(14, methods.Count);
        }
        
        [TestMethod]
        [Ignore]
        public void MethodInfoEqualsCheck()
        {
            var info1 = new AsmDiff.Lib.MethodInfo();

            var type1 = new Mock<TypeReference>();
          
            var param1 = new Moq.Mock<ParameterDefinition>();
            
            var info2 = new AsmDiff.Lib.MethodInfo();
        }
    }
}
