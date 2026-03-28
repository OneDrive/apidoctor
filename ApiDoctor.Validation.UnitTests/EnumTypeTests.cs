/*
 * API Doctor
 * Copyright (c) Microsoft Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the ""Software""), to deal in
 * the Software without restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
 * Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace ApiDoctor.Validation.UnitTests
{
    using System.Linq;
    using ApiDoctor.Validation.Error;
    using ApiDoctor.Validation.OData;
    using NUnit.Framework;

    [TestFixture]
    public class EnumTypeTests
    {
        private const string EdmxOpen = @"<?xml version=""1.0"" encoding=""utf-8""?>
<edmx:Edmx Version=""4.0"" xmlns:edmx=""http://docs.oasis-open.org/odata/ns/edmx"">
  <edmx:DataServices>
    <Schema Namespace=""microsoft.graph"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">";

        private const string EdmxClose = @"
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";

        #region Deserialization

        [Test]
        public void Deserialize_EnumType_ParsesNameAndMembers()
        {
            var xml = EdmxOpen + @"
      <EnumType Name=""BodyType"">
        <Member Name=""text"" Value=""0""/>
        <Member Name=""html"" Value=""1""/>
      </EnumType>" + EdmxClose;

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var enumType = ef.DataServices.Schemas.Single().Enumerations.Single();

            Assert.That(enumType.Name, Is.EqualTo("BodyType"));
            Assert.That(enumType.Members.Count, Is.EqualTo(2));
            Assert.That(enumType.Members[0].Name, Is.EqualTo("text"));
            Assert.That(enumType.Members[0].Value, Is.EqualTo("0"));
            Assert.That(enumType.Members[1].Name, Is.EqualTo("html"));
            Assert.That(enumType.Members[1].Value, Is.EqualTo("1"));
        }

        [Test]
        public void Deserialize_EnumType_UnderlyingType_ParsedCorrectly()
        {
            var xml = EdmxOpen + @"
      <EnumType Name=""WeekIndex"" UnderlyingType=""Edm.Int32"">
        <Member Name=""first"" Value=""0""/>
      </EnumType>" + EdmxClose;

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var enumType = ef.DataServices.Schemas.Single().Enumerations.Single();

            Assert.That(enumType.UnderlyingType, Is.EqualTo("Edm.Int32"));
        }

        [Test]
        public void Deserialize_EnumType_IsFlags_ParsedCorrectly()
        {
            var xml = EdmxOpen + @"
      <EnumType Name=""CalendarRoleType"" IsFlags=""true"">
        <Member Name=""none"" Value=""0""/>
        <Member Name=""freeBusyRead"" Value=""1""/>
        <Member Name=""limitedRead"" Value=""2""/>
      </EnumType>" + EdmxClose;

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var enumType = ef.DataServices.Schemas.Single().Enumerations.Single();

            Assert.That(enumType.IsFlags, Is.True);
        }

        [Test]
        public void Deserialize_EnumType_IsFlags_DefaultsFalse()
        {
            var xml = EdmxOpen + @"
      <EnumType Name=""StatusType"">
        <Member Name=""active"" Value=""1""/>
      </EnumType>" + EdmxClose;

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var enumType = ef.DataServices.Schemas.Single().Enumerations.Single();

            Assert.That(enumType.IsFlags, Is.False);
        }

        [Test]
        public void Deserialize_EnumType_UnderlyingType_NullWhenAbsent()
        {
            var xml = EdmxOpen + @"
      <EnumType Name=""SimpleEnum"">
        <Member Name=""value1"" Value=""0""/>
      </EnumType>" + EdmxClose;

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var enumType = ef.DataServices.Schemas.Single().Enumerations.Single();

            Assert.That(enumType.UnderlyingType, Is.Null);
        }

        [Test]
        public void Deserialize_EnumType_MemberWithoutValue_ParsedWithNullValue()
        {
            var xml = EdmxOpen + @"
      <EnumType Name=""StatusEnum"">
        <Member Name=""unknown""/>
        <Member Name=""active"" Value=""1""/>
      </EnumType>" + EdmxClose;

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var members = ef.DataServices.Schemas.Single().Enumerations.Single().Members;

            Assert.That(members.First(m => m.Name == "unknown").Value, Is.Null);
            Assert.That(members.First(m => m.Name == "active").Value, Is.EqualTo("1"));
        }

        [Test]
        public void Deserialize_MultipleEnumTypes_AllParsed()
        {
            var xml = EdmxOpen + @"
      <EnumType Name=""BodyType"">
        <Member Name=""text"" Value=""0""/>
      </EnumType>
      <EnumType Name=""Importance"">
        <Member Name=""low"" Value=""0""/>
        <Member Name=""normal"" Value=""1""/>
        <Member Name=""high"" Value=""2""/>
      </EnumType>" + EdmxClose;

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var enums = ef.DataServices.Schemas.Single().Enumerations;

            Assert.That(enums.Count, Is.EqualTo(2));
            Assert.That(enums.Select(e => e.Name), Is.EquivalentTo(new[] { "BodyType", "Importance" }));
        }

        [Test]
        public void Deserialize_EnumType_TypeIdentifier_EqualsName()
        {
            var xml = EdmxOpen + @"
      <EnumType Name=""DayOfWeek"">
        <Member Name=""sunday"" Value=""0""/>
      </EnumType>" + EdmxClose;

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var enumType = ef.DataServices.Schemas.Single().Enumerations.Single();

            Assert.That(enumType.TypeIdentifier, Is.EqualTo("DayOfWeek"));
        }

        #endregion

        #region Navigation — NotImplementedException boundaries

        [Test]
        public void EnumType_NavigateByUriComponent_ThrowsNotImplementedException()
        {
            var enumType = new EnumType { Name = "TestEnum" };
            var edmx = new EntityFramework();
            var issues = new IssueLogger();

            Assert.That(() => enumType.NavigateByUriComponent("anything", edmx, issues, false),
                Throws.TypeOf<System.NotImplementedException>());
        }

        [Test]
        public void EnumType_NavigateByEntityTypeKey_ThrowsNotImplementedException()
        {
            var enumType = new EnumType { Name = "TestEnum" };
            var edmx = new EntityFramework();
            var issues = new IssueLogger();

            Assert.That(() => enumType.NavigateByEntityTypeKey(edmx, issues),
                Throws.TypeOf<System.NotImplementedException>());
        }

        #endregion
    }
}
