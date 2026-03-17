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
    using System.Collections.Generic;
    using ApiDoctor.Validation.OData;
    using NUnit.Framework;

    [TestFixture]
    public class ActionOrFunctionTests
    {
        #region Helpers

        private static EntityFramework EmptyEdmx() =>
            new EntityFramework(new[] { new Schema
            {
                Namespace = "af.test",
                EntityTypes = new List<EntityType>(),
                ComplexTypes = new List<ComplexType>(),
                EntityContainers = new List<EntityContainer>(),
                Functions = new List<Function>(),
                Actions = new List<OData.Action>(),
                Terms = new List<Term>(),
                Annotations = new List<Annotations>(),
                Enumerations = new List<EnumType>()
            }});

        private static EntityFramework EdmxWithTypes(string ns, List<ComplexType> complexTypes)
        {
            var schema = new Schema
            {
                Namespace = ns,
                ComplexTypes = complexTypes,
                EntityTypes = new List<EntityType>(),
                EntityContainers = new List<EntityContainer>(),
                Functions = new List<Function>(),
                Actions = new List<OData.Action>(),
                Terms = new List<Term>(),
                Annotations = new List<Annotations>(),
                Enumerations = new List<EnumType>()
            };
            return new EntityFramework(new[] { schema });
        }

        private static Function MakeFunction(string name, bool isBound, List<Parameter> parameters, ReturnType returnType = null) =>
            new Function
            {
                Name = name,
                IsBound = isBound,
                Parameters = parameters ?? new List<Parameter>(),
                ReturnType = returnType
            };

        private static Parameter MakeParam(string name, string type, bool? isNullable = null, bool? unicode = null)
        {
            var p = new Parameter { Name = name, Type = type };
            if (isNullable.HasValue) p.IsNullable = isNullable;
            if (unicode.HasValue) p.Unicode = unicode;
            return p;
        }

        private static ReturnType MakeReturn(string type, bool nullable = false) =>
            new ReturnType { Type = type, Nullable = nullable };

        #endregion

        #region CanSubstituteFor — basic matching

        [Test]
        public void CanSubstituteFor_IdenticalFunctions_ReturnsTrue()
        {
            var f1 = MakeFunction("getThings", true,
                new List<Parameter> { MakeParam("bindingParameter", "af.test.User") },
                MakeReturn("Edm.String"));
            var f2 = MakeFunction("getThings", true,
                new List<Parameter> { MakeParam("bindingParameter", "af.test.User") },
                MakeReturn("Edm.String"));

            Assert.That(f1.CanSubstituteFor(f2, EmptyEdmx()), Is.True);
        }

        [Test]
        public void CanSubstituteFor_NullOther_ReturnsFalse()
        {
            var f1 = MakeFunction("getThings", true, new List<Parameter>());

            Assert.That(f1.CanSubstituteFor(null, EmptyEdmx()), Is.False);
        }

        [Test]
        public void CanSubstituteFor_DifferentName_ReturnsFalse()
        {
            var f1 = MakeFunction("getThings", true, new List<Parameter>(), MakeReturn("Edm.String"));
            var f2 = MakeFunction("fetchThings", true, new List<Parameter>(), MakeReturn("Edm.String"));

            Assert.That(f1.CanSubstituteFor(f2, EmptyEdmx()), Is.False);
        }

        [Test]
        public void CanSubstituteFor_DifferentIsBound_ReturnsFalse()
        {
            var f1 = MakeFunction("getThings", true, new List<Parameter>(), MakeReturn("Edm.String"));
            var f2 = MakeFunction("getThings", false, new List<Parameter>(), MakeReturn("Edm.String"));

            Assert.That(f1.CanSubstituteFor(f2, EmptyEdmx()), Is.False);
        }

        [Test]
        public void CanSubstituteFor_DifferentParameterCount_ReturnsFalse()
        {
            var f1 = MakeFunction("getThings", true,
                new List<Parameter> { MakeParam("p1", "Edm.String") },
                MakeReturn("Edm.String"));
            var f2 = MakeFunction("getThings", true,
                new List<Parameter> { MakeParam("p1", "Edm.String"), MakeParam("p2", "Edm.Int32") },
                MakeReturn("Edm.String"));

            Assert.That(f1.CanSubstituteFor(f2, EmptyEdmx()), Is.False);
        }

        [Test]
        public void CanSubstituteFor_DifferentReturnType_ReturnsFalse()
        {
            var f1 = MakeFunction("getThings", true, new List<Parameter>(), MakeReturn("Edm.String"));
            var f2 = MakeFunction("getThings", true, new List<Parameter>(), MakeReturn("Edm.Int32"));

            Assert.That(f1.CanSubstituteFor(f2, EmptyEdmx()), Is.False);
        }

        [Test]
        public void CanSubstituteFor_BothNullReturnType_ReturnsTrue()
        {
            var f1 = MakeFunction("doAction", true, new List<Parameter>(), returnType: null);
            var f2 = MakeFunction("doAction", true, new List<Parameter>(), returnType: null);

            Assert.That(f1.CanSubstituteFor(f2, EmptyEdmx()), Is.True);
        }

        [Test]
        public void CanSubstituteFor_OneNullReturnType_ReturnsFalse()
        {
            var f1 = MakeFunction("getThings", true, new List<Parameter>(), MakeReturn("Edm.String"));
            var f2 = MakeFunction("getThings", true, new List<Parameter>(), returnType: null);

            Assert.That(f1.CanSubstituteFor(f2, EmptyEdmx()), Is.False);
        }

        [Test]
        public void CanSubstituteFor_ActionVsFunction_ReturnsFalse()
        {
            var func = MakeFunction("getThings", true, new List<Parameter>(), MakeReturn("Edm.String"));
            var action = new OData.Action
            {
                Name = "getThings",
                IsBound = true,
                Parameters = new List<Parameter>(),
                ReturnType = MakeReturn("Edm.String")
            };

            Assert.That(func.CanSubstituteFor(action, EmptyEdmx()), Is.False);
        }

        #endregion

        #region CanSubstituteFor — parameter matching

        [Test]
        public void CanSubstituteFor_ParameterNameNotInOther_ReturnsFalse()
        {
            var f1 = MakeFunction("getThings", true,
                new List<Parameter> { MakeParam("input", "Edm.String") },
                MakeReturn("Edm.String"));
            var f2 = MakeFunction("getThings", true,
                new List<Parameter> { MakeParam("differentName", "Edm.String") },
                MakeReturn("Edm.String"));

            Assert.That(f1.CanSubstituteFor(f2, EmptyEdmx()), Is.False);
        }

        [Test]
        public void CanSubstituteFor_ParameterIsNullableMismatch_ReturnsFalse()
        {
            var f1 = MakeFunction("getThings", true,
                new List<Parameter> { MakeParam("p1", "Edm.String", isNullable: true) },
                MakeReturn("Edm.String"));
            var f2 = MakeFunction("getThings", true,
                new List<Parameter> { MakeParam("p1", "Edm.String", isNullable: false) },
                MakeReturn("Edm.String"));

            Assert.That(f1.CanSubstituteFor(f2, EmptyEdmx()), Is.False);
        }

        [Test]
        public void CanSubstituteFor_ParameterUnicodeMismatch_ReturnsFalse()
        {
            var f1 = MakeFunction("getThings", true,
                new List<Parameter> { MakeParam("p1", "Edm.String", unicode: true) },
                MakeReturn("Edm.String"));
            var f2 = MakeFunction("getThings", true,
                new List<Parameter> { MakeParam("p1", "Edm.String", unicode: false) },
                MakeReturn("Edm.String"));

            Assert.That(f1.CanSubstituteFor(f2, EmptyEdmx()), Is.False);
        }

        [Test]
        public void CanSubstituteFor_ParameterTypeMismatch_NoInheritance_ReturnsFalse()
        {
            var f1 = MakeFunction("getThings", true,
                new List<Parameter> { MakeParam("bindingParameter", "af.test.TypeA") },
                MakeReturn("Edm.String"));
            var f2 = MakeFunction("getThings", true,
                new List<Parameter> { MakeParam("bindingParameter", "af.test.TypeB") },
                MakeReturn("Edm.String"));

            Assert.That(f1.CanSubstituteFor(f2, EmptyEdmx()), Is.False);
        }

        #endregion

        #region CanSubstituteFor — contravariance via inheritance

        [Test]
        public void CanSubstituteFor_ThisBoundToBaseType_OtherBoundToDerivedType_ReturnsTrue()
        {
            // f1 is bound to BaseItem (base type) — can substitute for f2 bound to DriveItem (derived)
            // because it's contravariant on the input type.
            var ns = "af.contra";
            var baseType = new ComplexType { Name = "BaseItem", Namespace = ns, Properties = new List<Property>() };
            var derivedType = new ComplexType { Name = "DriveItem", Namespace = ns, BaseType = $"{ns}.BaseItem", Properties = new List<Property>() };
            var edmx = EdmxWithTypes(ns, new List<ComplexType> { baseType, derivedType });

            var f1 = MakeFunction("getThings", true,
                new List<Parameter> { MakeParam("bindingParameter", $"{ns}.BaseItem") },
                MakeReturn("Edm.String"));
            var f2 = MakeFunction("getThings", true,
                new List<Parameter> { MakeParam("bindingParameter", $"{ns}.DriveItem") },
                MakeReturn("Edm.String"));

            Assert.That(f1.CanSubstituteFor(f2, edmx), Is.True);
        }

        [Test]
        public void CanSubstituteFor_ThisBoundToDerivedType_OtherBoundToBaseType_ReturnsFalse()
        {
            // Contravariance only works one way: this must be the base, other must be derived.
            var ns = "af.contra.rev";
            var baseType = new ComplexType { Name = "RevBase", Namespace = ns, Properties = new List<Property>() };
            var derivedType = new ComplexType { Name = "RevDerived", Namespace = ns, BaseType = $"{ns}.RevBase", Properties = new List<Property>() };
            var edmx = EdmxWithTypes(ns, new List<ComplexType> { baseType, derivedType });

            var f1 = MakeFunction("getThings", true,
                new List<Parameter> { MakeParam("bindingParameter", $"{ns}.RevDerived") },
                MakeReturn("Edm.String"));
            var f2 = MakeFunction("getThings", true,
                new List<Parameter> { MakeParam("bindingParameter", $"{ns}.RevBase") },
                MakeReturn("Edm.String"));

            Assert.That(f1.CanSubstituteFor(f2, edmx), Is.False);
        }

        [Test]
        public void CanSubstituteFor_MultiLevelContravariance_ReturnsTrue()
        {
            // f1 bound to Level1 (grandparent), f2 bound to Level3 (grandchild) — should still match
            var ns = "af.multilevel";
            var level1 = new ComplexType { Name = "Level1", Namespace = ns, Properties = new List<Property>() };
            var level2 = new ComplexType { Name = "Level2", Namespace = ns, BaseType = $"{ns}.Level1", Properties = new List<Property>() };
            var level3 = new ComplexType { Name = "Level3", Namespace = ns, BaseType = $"{ns}.Level2", Properties = new List<Property>() };
            var edmx = EdmxWithTypes(ns, new List<ComplexType> { level1, level2, level3 });

            var f1 = MakeFunction("getThings", true,
                new List<Parameter> { MakeParam("bindingParameter", $"{ns}.Level1") },
                MakeReturn("Edm.String"));
            var f2 = MakeFunction("getThings", true,
                new List<Parameter> { MakeParam("bindingParameter", $"{ns}.Level3") },
                MakeReturn("Edm.String"));

            Assert.That(f1.CanSubstituteFor(f2, edmx), Is.True);
        }

        #endregion

        #region Function deserialization

        [Test]
        public void Deserialize_Function_ParsesNameParametersAndReturnType()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<edmx:Edmx Version=""4.0"" xmlns:edmx=""http://docs.oasis-open.org/odata/ns/edmx"">
  <edmx:DataServices>
    <Schema Namespace=""microsoft.graph"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <Function Name=""getMemberObjects"" IsBound=""true"">
        <Parameter Name=""bindingParameter"" Type=""microsoft.graph.DirectoryObject""/>
        <Parameter Name=""securityEnabledOnly"" Type=""Edm.Boolean""/>
        <ReturnType Type=""Collection(Edm.String)""/>
      </Function>
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var func = ef.DataServices.Schemas[0].Functions[0];

            Assert.That(func.Name, Is.EqualTo("getMemberObjects"));
            Assert.That(func.IsBound, Is.True);
            Assert.That(func.Parameters.Count, Is.EqualTo(2));
            Assert.That(func.Parameters[0].Name, Is.EqualTo("bindingParameter"));
            Assert.That(func.Parameters[1].Name, Is.EqualTo("securityEnabledOnly"));
            Assert.That(func.ReturnType.Type, Is.EqualTo("Collection(Edm.String)"));
        }

        [Test]
        public void Deserialize_Function_IsComposable_ParsedCorrectly()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<edmx:Edmx Version=""4.0"" xmlns:edmx=""http://docs.oasis-open.org/odata/ns/edmx"">
  <edmx:DataServices>
    <Schema Namespace=""microsoft.graph"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <Function Name=""composableFunc"" IsBound=""true"" IsComposable=""true"">
        <Parameter Name=""bindingParameter"" Type=""microsoft.graph.Item""/>
        <ReturnType Type=""Edm.String""/>
      </Function>
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var func = ef.DataServices.Schemas[0].Functions[0];

            Assert.That(func.IsComposable, Is.True);
        }

        [Test]
        public void Deserialize_Action_ParsesNameAndParameters()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<edmx:Edmx Version=""4.0"" xmlns:edmx=""http://docs.oasis-open.org/odata/ns/edmx"">
  <edmx:DataServices>
    <Schema Namespace=""microsoft.graph"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <Action Name=""sendMail"" IsBound=""true"">
        <Parameter Name=""bindingParameter"" Type=""microsoft.graph.User""/>
        <Parameter Name=""Message"" Type=""microsoft.graph.Message""/>
      </Action>
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var action = ef.DataServices.Schemas[0].Actions[0];

            Assert.That(action.Name, Is.EqualTo("sendMail"));
            Assert.That(action.IsBound, Is.True);
            Assert.That(action.Parameters.Count, Is.EqualTo(2));
            Assert.That(action.ReturnType, Is.Null);
        }

        #endregion
    }
}
