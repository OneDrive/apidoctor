namespace ApiDoctor.Console.UnitTests
{
    using System.Linq;
    using ApiDoctor.ConsoleApp;
    using NUnit.Framework;

    [TestFixture]
    public class FileSplicerTests
    {
        [Test]
        public void FileSplicer_InsertsAfterSpecifiedOffset()
        {
            var original = new[] { "line0", "line1", "line2" };
            var result = Program.FileSplicer(original, 1, "inserted").ToArray();

            Assert.That(result, Is.EqualTo(new[] { "line0", "line1", "inserted", "line2" }));
        }

        [Test]
        public void FileSplicer_InsertsAfterFirstElement()
        {
            var original = new[] { "line0", "line1", "line2" };
            var result = Program.FileSplicer(original, 0, "inserted").ToArray();

            Assert.That(result, Is.EqualTo(new[] { "line0", "inserted", "line1", "line2" }));
        }

        [Test]
        public void FileSplicer_InsertsAfterLastElement()
        {
            var original = new[] { "line0", "line1", "line2" };
            var result = Program.FileSplicer(original, 2, "inserted").ToArray();

            Assert.That(result, Is.EqualTo(new[] { "line0", "line1", "line2", "inserted" }));
        }

        [Test]
        public void FileSplicer_SingleElementArray()
        {
            var original = new[] { "only" };
            var result = Program.FileSplicer(original, 0, "inserted").ToArray();

            Assert.That(result, Is.EqualTo(new[] { "only", "inserted" }));
        }
    }
}
