using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ApiDoctor.Validation.Utility
{
    public class ObjectGraphMerger<T> where T : new()
    {
        public T Result { get; set; }

        public T[] Sources { get; private set; }

        public ObjectGraphMerger(params T[] objectGraphs)
        {
            this.Sources = objectGraphs;
            this.Result = new T();
        }

        /// <summary>
        /// Merge the frameworks in the Sources collection together into the Result
        /// </summary>
        public T Merge()
        {
            Dictionary<T, string> sourceIdentifier = new Dictionary<T, string>();
            foreach(var source in this.Sources)
            {
                sourceIdentifier[source] = Guid.NewGuid().ToString();
            }

            var input = this.Sources.Select(x => new NodeToMerge { Source = sourceIdentifier[x], Node = x }).ToArray();
            var mergerResult = MergeNodes(input);
            this.Result = (T)mergerResult;
            return this.Result;
        }


        private class NodeToMerge
        {
            public string Source { get; set; }

            public object Node { get; set;}

            public NodeToMerge()
            {

            }

            public NodeToMerge(string source, object node)
            {
                this.Source = source;
                this.Node = node;
            }
        }

        /// <summary>
        /// Takes a collection of NodeToMerge objects and returns the single resulting object that is the merger of the two nodesToMerge.Node objects.
        /// </summary>
        /// <param name="nodesToMerge"></param>
        /// <returns></returns>
        private object MergeNodes(IEnumerable<NodeToMerge> nodesToMerge)
        {
            if (!nodesToMerge.Any())
                return null;

            Type type = nodesToMerge.First().Node.GetType();
            VerifyNodesAreOfType(nodesToMerge, type);

            var result = CreateInstanceOfType(type);

            var propertyMap = GetMergerPropertyMap(type);
            foreach(var mapping in propertyMap)
            {
                var values = (from node in nodesToMerge
                              where node != null
                              select new NodeToMerge { Source = node.Source, Node = mapping.Property.GetValue(node.Node) });

                if (mapping.IsCollection)
                {
                    // We need to find and merge values within the collection, dedupe them by their CollectionIdentifier
                    // and then merge objects with the same identifier
                    var collectionMembers = EnumerableAllCollectionMembers(values);
                    var resultingMembers = DedupeMembersInCollection(mapping, collectionMembers);
                    SetCollectionProperty(result, mapping.Property, resultingMembers.ToArray());
                }
                else if (mapping.IsSimpleType)
                {
                    object mergedCollection = CalculateMergedValueForMapping(mapping, values.Select(x => x.Node));
                    mapping.Property.SetValue(result, mergedCollection);
                }
                else
                {
                    // We have complex objects at this level of hierarchy that we can't resolve, so we need to merge those nodes
                    object mergedValue = MergeNodes(values.Where(x => x.Node != null));
                    mapping.Property.SetValue(result, mergedValue);
                }
            }
            return result;
        }

        private void SetCollectionProperty(object obj, PropertyInfo collectionProperty, object[] valueToSet)
        {
            var type = collectionProperty.PropertyType;
            if (type.IsArray)
            {
                Type innerType = type.GetElementType();
                var array = Array.CreateInstance(innerType, valueToSet.Length);
                for (int i = 0; i <valueToSet.Length; i++)
                {
                    array.SetValue(valueToSet[i], i);
                }
                collectionProperty.SetValue(obj, array);
            }
            else if (typeof(IList).IsAssignableFrom(type))
            {
                
                IList list = CreateInstanceOfType(type) as IList;
                for(int i=0; i<valueToSet.Length; i++)
                {
                    list.Add(valueToSet[i]);
                }
                collectionProperty.SetValue(obj, list);
            }
            else
            {
                throw new NotSupportedException($"Unable to set collection property of type {type.Name}.");
            }
        }

        private IEnumerable<object> DedupeMembersInCollection(MergerConditions mapping, IEnumerable<NodeToMerge> members)
        {
            if (!members.Any())
            {
                return new object[0];
            }

            Dictionary<object, object> equivelentKeyValues = new Dictionary<object, object>();
            var policyAttributeOnIdentifier = mapping?.CollectionIdentifierProperty?.GetCustomAttribute<MergePolicyAttribute>();
            if (null != policyAttributeOnIdentifier)
            {
                equivelentKeyValues = ParseEquivalentValues(policyAttributeOnIdentifier.EquivalentValues);
            }

            if (null == mapping.CollectionIdentifierProperty)
            {
                throw new ObjectGraphMergerException($"Missing a collection identifier for class {mapping.CollectionInnerType.Name} referenced from {mapping.Property.DeclaringType.Name}.{mapping.Property.Name}.");
            }

            Dictionary<string, List<NodeToMerge>> uniqueMembers = new Dictionary<string, List<NodeToMerge>>();

            // First pass, organize everything by the collapse single object matching property, if it exists
            Dictionary<string, List<NodeToMerge>> matchingMembers = new Dictionary<string, List<NodeToMerge>>();
            foreach (var obj in members)
            {
                if (mapping.CollapseSingleObjectMatchingProperty != null)
                {
                    string collapsingKey = (string)mapping.CollapseSingleObjectMatchingProperty.GetValue(obj.Node);
                    if (!string.IsNullOrEmpty(collapsingKey))
                    {
                        matchingMembers.AddToList(collapsingKey, obj);
                    }
                }
                else
                {
                    string key = (string)mapping.CollectionIdentifierProperty.GetValue(obj.Node);
                    object newKey;
                    if (equivelentKeyValues != null && equivelentKeyValues.TryGetValue(key, out newKey))
                    {
                        key = (string)newKey;
                    }
                    matchingMembers.AddToList(key, obj);
                }
            }

            foreach(var match in matchingMembers)
            {
                // Special case, the same object is defined in two different sources only once.
                if (match.Value.Count == 2 && match.Value[0].Source != match.Value[1].Source)
                {
                    // Single instance of a member, with different sources, means we should treat them as a single mergable member
                    uniqueMembers.AddToList(match.Key, match.Value[0], match.Value[1]);
                }
                else
                {
                    // Reindex these nodes using their element identifier instead of the collapsing element identifier
                    foreach (var matchedNode in match.Value)
                    {
                        string key = (string)mapping.CollectionIdentifierProperty.GetValue(matchedNode.Node);
                        uniqueMembers.AddToList(key, matchedNode);
                    }
                }
            }

            // Merge any elements where a duplicate key exists
            foreach(var key in uniqueMembers)
            {
                if (key.Value.Count > 1)
                {
                    var mergedNode = MergeNodes(key.Value.ToArray());
                    key.Value.Clear();
                    key.Value.Add(new NodeToMerge { Source = "merger", Node = mergedNode });
                }
            }

            // Return the single value from each key member
            return (from m in uniqueMembers select m.Value.Single().Node);
        }

        private static bool IsSimple(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // nullable type, check if the nested type is simple.
                return IsSimple(type.GetGenericArguments()[0]);
            }
            return type.IsPrimitive
              || type.IsEnum
              || type.Equals(typeof(string))
              || type.Equals(typeof(decimal));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<NodeToMerge> EnumerableAllCollectionMembers(IEnumerable<NodeToMerge> collections)
        {
            foreach(var collectionNode in collections)
            {
                if (collectionNode == null || collectionNode.Node == null)
                    continue;

                if (!(collectionNode.Node is IEnumerable))
                    throw new InvalidOperationException("Cannot enumerate the collection members of a node that isn't a collection.");

                foreach(var obj in (IEnumerable)collectionNode.Node)
                {
                    yield return new NodeToMerge(collectionNode.Source, obj);
                }
            }
        }

        private object CalculateMergedValueForMapping(MergerConditions mapping, IEnumerable<object> values)
        {
            if (!mapping.IsSimpleType)
            {
                throw new ObjectGraphMergerException("Attempted to calculate merged value for a non-value type map.");
            }

            switch (mapping.Policy)
            {
                case MergePolicy.EqualOrNull:
                case MergePolicy.Default:
                    {
                        // See if we need to do any mapping of values to their replacements.                        
                        if (mapping.EquivalentValues.Any())
                        {
                            object[] knownValues = values.ToArray();
                            for(int i=0; i<knownValues.Length; i++)
                            {
                                object replacementValue;
                                if (mapping.EquivalentValues.TryGetValue(knownValues[i], out replacementValue))
                                {
                                    knownValues[i] = replacementValue;
                                }
                            }
                            
                            // Replace the enumerable collection with our modified version
                            values = knownValues;
                        }

                        object knownValue = values.FirstOrDefault(x => x != null);
                        if (values.Any(x => x != null && !x.Equals(knownValue)))
                        {
                            if (IsSimple(knownValue.GetType()))
                            {
                                Console.WriteLine($"Unable to merge values for {mapping.Property.DeclaringType.Name}.{mapping.Property.Name} because values are not equal or null. Values: {values.Select(x => x.ToString()).ComponentsJoinedByString(",")}.");
                                return $"MERGE INCONSISTENCY: {values.Select(x => x.ToString()).ComponentsJoinedByString(",")}";
                            }
                            else
                            {
                                throw new InvalidOperationException($"Unable to merge values for {mapping.Property.DeclaringType.Name}.{mapping.Property.Name} because values are not equal or null. Values: {values.Select(x => x.ToString()).ComponentsJoinedByString(",")}.");
                            }
                        }
                        return knownValue;
                    }
                case MergePolicy.PreferGreaterValue:
                    return values.OrderByDescending(x => x).FirstOrDefault();

                case MergePolicy.PreferLesserValue:
                    return values.OrderBy(x => x).FirstOrDefault();
                default:
                    throw new NotImplementedException($"Unsupported merge policy value: {mapping.Policy}.");
            }
        }

        private static Dictionary<Type, MergerConditions[]> knownMergerPropertyMaps = new Dictionary<Type, MergerConditions[]>();
        private static MergerConditions[] GetMergerPropertyMap(Type type)
        {
            if (type.GetCustomAttribute(typeof(MergableAttribute), true) == null)
            {
                throw new ObjectGraphMergerException($"Object type {type.Name} is not marked with the Mergable attribute.");
            }

            MergerConditions[] result = null;
            if (!knownMergerPropertyMaps.TryGetValue(type, out result))
            {
                // Generate the map for this type and cache it
                List<MergerConditions> conditions = new List<MergerConditions>();
                var mergableProperties = type.GetProperties().Where(p => p.CanRead && p.CanWrite);
                foreach(var prop in mergableProperties)
                {
                    var policyAttrib = prop.GetCustomAttribute<MergePolicyAttribute>(true);
                    var policy = policyAttrib?.Policy ?? MergePolicy.Default;
                    if (policy != MergePolicy.Ignore)
                    {
                        var condition = new MergerConditions { Policy = policy };
                        condition.EquivalentValues = ParseEquivalentValues(policyAttrib?.EquivalentValues);
                        if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string))
                        {
                            condition.IsCollection = true;
                            // Determine the class type contained by this collection
                            var collectionType = prop.PropertyType;
                            if (collectionType.IsGenericType)
                            {
                                condition.CollectionInnerType = collectionType.GenericTypeArguments.First();
                            }
                            else
                            {
                                condition.CollectionInnerType = collectionType.GetElementType();
                            }
                            condition.CollectionIdentifierPropertyName = condition.CollectionInnerType?.GetCustomAttribute<MergableAttribute>(true)?.CollectionIdentifier;
                            if (!string.IsNullOrEmpty(condition.CollectionIdentifierPropertyName))
                            {
                                condition.CollectionIdentifierProperty = condition.CollectionInnerType?.GetProperty(condition.CollectionIdentifierPropertyName);
                            }
                            condition.CollapseSingleItemMatchingName = condition.CollectionInnerType?.GetCustomAttribute<MergableAttribute>(true)?.CollapseSingleItemMatchingProperty;
                            if (!string.IsNullOrEmpty(condition.CollapseSingleItemMatchingName))
                            {
                                condition.CollapseSingleObjectMatchingProperty = condition.CollectionInnerType?.GetProperty(condition.CollapseSingleItemMatchingName);
                            }

                        }
                        condition.IsSimpleType = IsSimple(prop.PropertyType);
                        condition.Property = prop;
                        conditions.Add(condition);
                    }
                }
                result = conditions.ToArray();
                knownMergerPropertyMaps.Add(type, result);
            }
            return result;
        }

        private static Dictionary<object, object> ParseEquivalentValues(string equivalentValues)
        {
            var output = new Dictionary<object, object>();
            if (equivalentValues != null)
            {
                var values = QueryHelpers.ParseQuery(equivalentValues);
                foreach (var key in values.Keys)
                {
                    output[key] = values[key];
                }
            }
            return output;
        }

        private class MergerConditions
        {
            public PropertyInfo Property { get; set; }
            public MergePolicy Policy { get; set; }
            public Dictionary<object, object> EquivalentValues { get; set; }
            public bool IsCollection { get; set; }
            public Type CollectionInnerType { get; set; }
            public bool IsSimpleType { get; set; }
            public string CollectionIdentifierPropertyName { get; internal set; }
            public PropertyInfo CollectionIdentifierProperty { get; internal set; }
            public string CollapseSingleItemMatchingName { get; internal set; }
            public PropertyInfo CollapseSingleObjectMatchingProperty { get; internal set; }
        }

        private static object CreateInstanceOfType(Type type)
        {
            object result = null;
            ConstructorInfo constructor = type.GetConstructor(new Type[] { });
            if (null == constructor)
            {
                throw new ObjectGraphMergerException($"Types being merged must have a parameterless public constructor: {type.Name}");
            }

            try
            {
                result = constructor.Invoke(new object[] { });
            }
            catch (Exception ex)
            {
                throw new ObjectGraphMergerException($"Unable to create a new instance of type {type.Name}.", ex);
            }

            return result;
        }

        private static void VerifyNodesAreOfType(IEnumerable<NodeToMerge> nodesToMerge, Type type)
        {
            if (nodesToMerge.Any(x => x.Node != null && x.Node.GetType() != type))
            {
                throw new ObjectGraphMergerException("Nodes to merge must all be the same class type.");
            }
        }



        //private void RecordFrameworkObject(PropertyInfo prop, object obj, Stack<object> parentObjects)
        //{
        //    if (prop != null) return;

        //    var sourceItem = obj as ITransformable;
        //    if (sourceItem != null)
        //    {
        //        var identifier = GenerateUniqueIdentifier(sourceItem, parentObjects);


        //        Type knownIdentiiferType = null;
        //        if (this.UniqueObjectIdentifiers.TryGetValue(identifier, out knownIdentiiferType))
        //        {
        //            if (knownIdentiiferType != sourceItem.GetType())
        //            {
        //                Console.WriteLine("Unique identifier found that references two different object types. That must be a bug.");
        //            }
        //        }
        //        else
        //        {
        //            this.UniqueObjectIdentifiers.Add(identifier, sourceItem.GetType());
        //        }
        //    }
        //}

        //private string GenerateUniqueIdentifier(ITransformable obj, Stack<object> parentObjects)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    foreach (var parent in parentObjects)
        //    {
        //        string identifier = (parent as ITransformable)?.ElementIdentifier;
        //        identifier = identifier ?? parent.GetType().Name;
        //        sb.Insert(0, identifier);
        //        sb.Insert(0, ".");
        //    }
        //    var output = sb.ToString().Substring(1);
        //    if (obj?.ElementIdentifier != null && !output.EndsWith(obj.ElementIdentifier))
        //    {
        //        output = output + $".{obj.ElementIdentifier}";
        //    }
        //    return output;
        //}
    }

    public class ObjectGraphMergerException : Exception
    {
        public ObjectGraphMergerException(string message) : base(message)
        {

        }

        public ObjectGraphMergerException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }


    [AttributeUsage(AttributeTargets.Class)]
    public class MergableAttribute : Attribute
    {
        public string CollectionIdentifier { get; set; }

        /// <summary>
        /// Setting this property allows use to merge a single instance of a class in two different EDMX sources into a single object, if the property value
        /// pointed at here is the same, even if the collection identifier is different.
        /// e.g. we can collapse an action with the same Name property even if the parameters are different (which are embedded in CollectionIdentifier)
        /// </summary>
        public string CollapseSingleItemMatchingProperty { get; set; }

    }

    [AttributeUsage(AttributeTargets.Property)]
    public class MergePolicyAttribute : Attribute
    {
        public MergePolicy Policy { get; set; }

        public string EquivalentValues { get; set; }
        
        public MergePolicyAttribute()
        {
            this.Policy = MergePolicy.Default;
        }

        public MergePolicyAttribute(MergePolicy policy)
        {
            this.Policy = policy;
        }
    }

    public enum MergePolicy
    {
        Default = 0,
        
        /// <summary>
        /// Indiciates that values in the objects being merged must be equal to each other or be null to have a successful merge.
        /// </summary>
        EqualOrNull = 1,

        /// <summary>
        /// The value in the objects being merged is ignored and not merged.
        /// </summary>
        Ignore = 2,

        PreferGreaterValue = 3,
        PreferTrueValue = 3,


        PreferLesserValue = 4,
        PreferFalseValue = 4,

        MustBeNull = 5,
    }
}
