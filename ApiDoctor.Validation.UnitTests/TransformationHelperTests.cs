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
    using System.Linq;
    using ApiDoctor.Validation.OData;
    using ApiDoctor.Validation.OData.Transformation;
    using NUnit.Framework;

    [TestFixture]
    public class TransformationHelperTests
    {
        #region Helpers

        private static EntityFramework EmptyEdmx(string ns = "test") =>
            new EntityFramework(new[] { new Schema
            {
                Namespace = ns,
                EntityTypes = new List<EntityType>(),
                ComplexTypes = new List<ComplexType>(),
                EntityContainers = new List<EntityContainer>(),
                Functions = new List<Function>(),
                Actions = new List<OData.Action>(),
                Terms = new List<Term>(),
                Annotations = new List<Annotations>(),
                Enumerations = new List<EnumType>()
            }});

        #endregion

        #region ApplyTransformation — simple property copy

        [Test]
        public void ApplyTransformation_SimpleStringProperty_CopiesValue()
        {
            var prop = new Property { Name = "id", Type = "Edm.String" };
            var mod = new PropertyModification { Type = "Edm.Guid" };

            TransformationHelper.ApplyTransformation(prop, mod, null, null);

            Assert.That(prop.Type, Is.EqualTo("Edm.Guid"));
        }

        [Test]
        public void ApplyTransformation_NullModificationValue_LeavesTargetUnchanged()
        {
            var prop = new Property { Name = "id", Type = "Edm.String" };
            var mod = new PropertyModification { Type = null };

            TransformationHelper.ApplyTransformation(prop, mod, null, null);

            Assert.That(prop.Type, Is.EqualTo("Edm.String"));
        }

        [Test]
        public void ApplyTransformation_MultipleSimpleProperties_AllCopied()
        {
            var prop = new Property { Name = "id", Type = "Edm.String" };
            var mod = new PropertyModification { Type = "Edm.Int32", VirtualNavigationPropertyName = "linkedEntity" };

            TransformationHelper.ApplyTransformation(prop, mod, null, null);

            Assert.That(prop.Type, Is.EqualTo("Edm.Int32"));
            Assert.That(prop.VirtualNavigationPropertyName, Is.EqualTo("linkedEntity"));
        }

        [Test]
        public void ApplyTransformation_AlternativeHandlerIntercepts_SkipsNormalCopy()
        {
            var prop = new Property { Name = "id", Type = "Edm.String" };
            var mod = new PropertyModification { Type = "Edm.Int32" };
            bool handlerCalled = false;

            TransformationHelper.ApplyTransformation(prop, mod, null, null, (key, value) =>
            {
                if (key == "Type") { handlerCalled = true; return true; }
                return false;
            });

            Assert.That(handlerCalled, Is.True);
            Assert.That(prop.Type, Is.EqualTo("Edm.String"), "Handler returned true — normal copy should be skipped.");
        }

        [Test]
        public void ApplyTransformation_AlternativeHandlerReturnsFalse_CopiesNormally()
        {
            var prop = new Property { Name = "id", Type = "Edm.String" };
            var mod = new PropertyModification { Type = "Edm.Int32" };

            TransformationHelper.ApplyTransformation(prop, mod, null, null, (key, value) => false);

            Assert.That(prop.Type, Is.EqualTo("Edm.Int32"));
        }

        #endregion

        #region ApplyTransformation — GraphPropertyName rename via Property.ApplyTransformation

        [Test]
        public void Property_ApplyTransformation_GraphPropertyName_RenamesAndPreservesWorkloadName()
        {
            var prop = new Property { Name = "id", Type = "Edm.String" };
            var mod = new PropertyModification { GraphPropertyName = "userId" };

            // Call through the overridden method that uses the alternativeHandler
            prop.ApplyTransformation(mod, null, null);

            Assert.That(prop.Name, Is.EqualTo("userId"));
            Assert.That(prop.WorkloadName, Is.EqualTo("id"));
        }

        [Test]
        public void Property_ApplyTransformation_GraphPropertyName_AlsoAppliesOtherProperties()
        {
            var prop = new Property { Name = "id", Type = "Edm.String" };
            var mod = new PropertyModification { GraphPropertyName = "userId", Type = "Edm.Guid" };

            prop.ApplyTransformation(mod, null, null);

            Assert.That(prop.Name, Is.EqualTo("userId"));
            Assert.That(prop.Type, Is.EqualTo("Edm.Guid"));
        }

        #endregion

        #region ApplyTransformationToCollection — exact match

        [Test]
        public void ApplyTransformationToCollection_ExactMatch_AppliesModification()
        {
            var mods = new Dictionary<string, PropertyModification>
            {
                { "id", new PropertyModification { Type = "Edm.Guid" } }
            };
            var targets = new List<Property>
            {
                new Property { Name = "id", Type = "Edm.String" },
                new Property { Name = "name", Type = "Edm.String" }
            };

            TransformationHelper.ApplyTransformationToCollection(mods, targets, null, null);

            Assert.That(targets.First(p => p.Name == "id").Type, Is.EqualTo("Edm.Guid"));
            Assert.That(targets.First(p => p.Name == "name").Type, Is.EqualTo("Edm.String"));
        }

        [Test]
        public void ApplyTransformationToCollection_NoMatchingElement_ListUnchanged()
        {
            var mods = new Dictionary<string, PropertyModification>
            {
                { "missing", new PropertyModification { Type = "Edm.Guid" } }
            };
            var targets = new List<Property>
            {
                new Property { Name = "id", Type = "Edm.String" }
            };

            TransformationHelper.ApplyTransformationToCollection(mods, targets, null, null);

            Assert.That(targets.Count, Is.EqualTo(1));
            Assert.That(targets[0].Type, Is.EqualTo("Edm.String"));
        }

        #endregion

        #region ApplyTransformationToCollection — wildcard

        [Test]
        public void ApplyTransformationToCollection_WildcardMatch_AppliesModToAllMatchingElements()
        {
            var mods = new Dictionary<string, PropertyModification>
            {
                { "display*", new PropertyModification { Type = "Edm.Guid" } }
            };
            var targets = new List<Property>
            {
                new Property { Name = "displayName", Type = "Edm.String" },
                new Property { Name = "displayTitle", Type = "Edm.String" },
                new Property { Name = "id", Type = "Edm.String" }
            };

            TransformationHelper.ApplyTransformationToCollection(mods, targets, null, null);

            Assert.That(targets.First(p => p.Name == "displayName").Type, Is.EqualTo("Edm.Guid"));
            Assert.That(targets.First(p => p.Name == "displayTitle").Type, Is.EqualTo("Edm.Guid"));
            Assert.That(targets.First(p => p.Name == "id").Type, Is.EqualTo("Edm.String"));
        }

        [Test]
        public void ApplyTransformationToCollection_WildcardMatchesNone_ListUnchanged()
        {
            var mods = new Dictionary<string, PropertyModification>
            {
                { "xyz*", new PropertyModification { Type = "Edm.Guid" } }
            };
            var targets = new List<Property>
            {
                new Property { Name = "id", Type = "Edm.String" }
            };

            TransformationHelper.ApplyTransformationToCollection(mods, targets, null, null);

            Assert.That(targets[0].Type, Is.EqualTo("Edm.String"));
        }

        #endregion

        #region ApplyTransformationToCollection — Remove

        [Test]
        public void ApplyTransformationToCollection_RemoveTrue_RemovesMatchedElement()
        {
            var mods = new Dictionary<string, PropertyModification>
            {
                { "id", new PropertyModification { Remove = true } }
            };
            var targets = new List<Property>
            {
                new Property { Name = "id" },
                new Property { Name = "name" }
            };

            TransformationHelper.ApplyTransformationToCollection(mods, targets, null, null);

            Assert.That(targets.Count, Is.EqualTo(1));
            Assert.That(targets[0].Name, Is.EqualTo("name"));
        }

        [Test]
        public void ApplyTransformationToCollection_RemoveFalse_DoesNotRemove()
        {
            var mods = new Dictionary<string, PropertyModification>
            {
                { "id", new PropertyModification { Remove = false, Type = "Edm.Guid" } }
            };
            var targets = new List<Property>
            {
                new Property { Name = "id", Type = "Edm.String" }
            };

            TransformationHelper.ApplyTransformationToCollection(mods, targets, null, null);

            Assert.That(targets.Count, Is.EqualTo(1));
            Assert.That(targets[0].Type, Is.EqualTo("Edm.Guid"));
        }

        #endregion

        #region ApplyTransformationToCollection — version filtering

        [Test]
        public void ApplyTransformationToCollection_VersionMismatch_RemovesElement()
        {
            var mods = new Dictionary<string, PropertyModification>
            {
                { "id", new PropertyModification { AvailableInVersions = new[] { "v2.0" } } }
            };
            var targets = new List<Property>
            {
                new Property { Name = "id" }
            };

            TransformationHelper.ApplyTransformationToCollection(mods, targets, null, new[] { "v1.0" });

            Assert.That(targets.Count, Is.EqualTo(0));
        }

        [Test]
        public void ApplyTransformationToCollection_VersionMatch_KeepsElement()
        {
            var mods = new Dictionary<string, PropertyModification>
            {
                { "id", new PropertyModification { AvailableInVersions = new[] { "v1.0", "v2.0" }, Type = "Edm.Guid" } }
            };
            var targets = new List<Property>
            {
                new Property { Name = "id", Type = "Edm.String" }
            };

            TransformationHelper.ApplyTransformationToCollection(mods, targets, null, new[] { "v1.0" });

            Assert.That(targets.Count, Is.EqualTo(1));
            Assert.That(targets[0].Type, Is.EqualTo("Edm.Guid"));
        }

        [Test]
        public void ApplyTransformationToCollection_NullVersionsToPublish_AlwaysAppliesModification()
        {
            var mods = new Dictionary<string, PropertyModification>
            {
                { "id", new PropertyModification { AvailableInVersions = new[] { "v1.0" }, Type = "Edm.Guid" } }
            };
            var targets = new List<Property>
            {
                new Property { Name = "id", Type = "Edm.String" }
            };

            TransformationHelper.ApplyTransformationToCollection(mods, targets, null, versions: null);

            Assert.That(targets[0].Type, Is.EqualTo("Edm.Guid"));
        }

        [Test]
        public void ApplyTransformationToCollection_NullAvailableVersions_AlwaysAppliesModification()
        {
            var mods = new Dictionary<string, PropertyModification>
            {
                { "id", new PropertyModification { AvailableInVersions = null, Type = "Edm.Guid" } }
            };
            var targets = new List<Property>
            {
                new Property { Name = "id", Type = "Edm.String" }
            };

            TransformationHelper.ApplyTransformationToCollection(mods, targets, null, new[] { "v1.0" });

            Assert.That(targets[0].Type, Is.EqualTo("Edm.Guid"));
        }

        #endregion

        #region ApplyTransformationToCollection — Add

        [Test]
        public void ApplyTransformationToCollection_AddTrue_CreatesNewElement()
        {
            var mods = new Dictionary<string, PropertyModification>
            {
                { "newProp", new PropertyModification { Add = true, Type = "Edm.String" } }
            };
            var targets = new List<Property>();

            TransformationHelper.ApplyTransformationToCollection(mods, targets, null, null);

            Assert.That(targets.Count, Is.EqualTo(1));
            Assert.That(targets[0].Name, Is.EqualTo("newProp"));
            Assert.That(targets[0].Type, Is.EqualTo("Edm.String"));
        }

        [Test]
        public void ApplyTransformationToCollection_AddFalse_NoElementCreated()
        {
            var mods = new Dictionary<string, PropertyModification>
            {
                { "newProp", new PropertyModification { Add = false, Type = "Edm.String" } }
            };
            var targets = new List<Property>();

            TransformationHelper.ApplyTransformationToCollection(mods, targets, null, null);

            Assert.That(targets.Count, Is.EqualTo(0));
        }

        [Test]
        public void ApplyTransformationToCollection_AddWithVersionMatch_CreatesNewElement()
        {
            var mods = new Dictionary<string, PropertyModification>
            {
                { "newProp", new PropertyModification { Add = true, AvailableInVersions = new[] { "v1.0" }, Type = "Edm.String" } }
            };
            var targets = new List<Property>();

            TransformationHelper.ApplyTransformationToCollection(mods, targets, null, new[] { "v1.0" });

            Assert.That(targets.Count, Is.EqualTo(1));
        }

        [Test]
        public void ApplyTransformationToCollection_AddWithVersionMismatch_DoesNotCreate()
        {
            var mods = new Dictionary<string, PropertyModification>
            {
                { "newProp", new PropertyModification { Add = true, AvailableInVersions = new[] { "v2.0" }, Type = "Edm.String" } }
            };
            var targets = new List<Property>();

            TransformationHelper.ApplyTransformationToCollection(mods, targets, null, new[] { "v1.0" });

            Assert.That(targets.Count, Is.EqualTo(0));
        }

        #endregion
    }
}
