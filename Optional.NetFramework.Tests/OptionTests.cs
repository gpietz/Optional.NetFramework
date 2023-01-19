using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Optional.NetFramework.Tests
{
    [TestClass]
    public class OptionTests
    {
        [TestMethod]
        public void Option_CreateAndCheckExistence_AllShouldBeNone()
        {
            var noneStruct = Option.None<int>();
            var noneNullable = Option.None<int?>();
            var noneClass = Option.None<string>();
            
            Assert.IsFalse(noneStruct.HasValue);
            Assert.IsFalse(noneNullable.HasValue);
            Assert.IsFalse(noneClass.HasValue);
        }

        [TestMethod]
        public void Option_CreateAndCheckExistence_AllShouldBeSome()
        {
            var someStruct = Option.Some<int>(1);
            var someNullable = Option.Some<int?>(1);
            var someNullableEmpty = Option.Some<int?>(null);
            var someClass = Option.Some("1");
            var someClassNull = Option.Some<string>(null);
            
            Assert.IsTrue(someStruct.HasValue);
            Assert.IsTrue(someNullable.HasValue);
            Assert.IsTrue(someNullableEmpty.HasValue);
            Assert.IsTrue(someClass.HasValue);
            Assert.IsTrue(someClassNull.HasValue);
        }
    }
}
