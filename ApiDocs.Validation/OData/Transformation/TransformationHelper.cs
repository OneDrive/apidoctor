using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation.OData.Transformation
{
    public delegate bool HandlePropertyModification(string propertyName, object modifiedValue);

    public static class TransformationHelper
    {
        private static readonly Type GenericDictionaryType = typeof(Dictionary<,>);
        private static readonly Type IEnumerableType = typeof(IEnumerable);
        private static readonly Type StringType = typeof(String);
        private static readonly Type BaseModificationsType = typeof(BaseModifications);

        

        public static void ApplyTransformation(ITransformable target, BaseModifications modifications, EntityFramework edmx, string[] versions, HandlePropertyModification alternativeHandler = null)
        {
            // For eacn property on the modifications instance, if there is a matching property on target
            // then we copy the value from modification to target, if the modification value is not null
            var modificationProperties = modifications.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(x => x.DeclaringType != typeof(BaseModifications));
            var targetProperties = target.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).ToDictionary(x => x.Name, x => x);

            foreach (var modsProp in modificationProperties)
            {
                var modsValue = modsProp.GetValue(modifications);
                if (null == modsValue)
                {
                    continue;
                }

                if (null != alternativeHandler && alternativeHandler(modsProp.Name, modsValue))
                {
                    continue;
                }

                Type modificationType;
                if (modsProp.IsModificationDictionary(out modificationType))
                {
                    PropertyInfo targetProp;
                    if (targetProperties.TryGetValue(modsProp.Name, out targetProp))
                    {
                        var targetValue = targetProp.GetValue(target);
                        InvokeApplyTransformationToCollection((IDictionary)modsValue, (IList)targetValue, edmx, versions);
                    }
                    else
                    {
                        Console.WriteLine($"Failed to locate a target property named '{modsProp.Name}' on '{target.GetType().Name}'.");
                    }
                }
                else if (modsProp.IsSimpleType())
                {
                    PropertyInfo targetProp;
                    if (targetProperties.TryGetValue(modsProp.Name, out targetProp))
                    {
                        targetProp.SetValue(target, modsValue);
                    }
                    else
                    {
                        Console.WriteLine($"Failed to locate a target property named '{modsProp.Name}' on '{target.GetType().Name}'.");
                    }
                }
                else
                {
                    // See if the object we're pointing at can be transformed
                    PropertyInfo targetProp;
                    if (targetProperties.TryGetValue(modsProp.Name, out targetProp))
                    {
                        object targetPropertyValue = targetProp.GetValue(target);
                        if (null == targetPropertyValue)
                        {
                            // Create a new default instance of this class, since we're trying to set properties on it
                            targetPropertyValue = CreateNewInstanceOfType(targetProp.PropertyType);
                            // Store the newly created instance back on the target object
                            targetProp.SetValue(target, targetPropertyValue);
                        }

                        ITransformable transformableTargetProperty = targetPropertyValue as ITransformable;
                        if (null != transformableTargetProperty)
                        {
                            transformableTargetProperty.ApplyTransformation((BaseModifications)modsValue, edmx, versions);
                            continue;
                        }
                    }
                    //Unsupported
                    throw new NotSupportedException();
                }
            }
        }

        private static object CreateNewInstanceOfType(Type t)
        {
            return Activator.CreateInstance(t);
        }

        private static void InvokeApplyTransformationToCollection(IDictionary mods, IList target, EntityFramework edmx, string[] versions)
        {
            Type T_mod = mods.GetType().GetGenericArguments()[1];    // Get the type used for the value
            Type T_target = target.GetType().GetGenericArguments()[0];   // Get the type used by the list

            var method = typeof(TransformationHelper).GetMethod("ApplyTransformationToCollection", BindingFlags.Static | BindingFlags.Public);
            method = method.MakeGenericMethod(T_mod, T_target);
            method.Invoke(null, new object[] { mods, target, edmx, versions });
        }

        private static bool IsVersionMatch(string[] versionsToPublish, string[] versionsAvailable)
        {
            if (versionsToPublish == null || versionsAvailable == null)
            {
                return true;
            }

            var intersection = versionsToPublish.Intersect(versionsAvailable);
            return intersection.Any();
        }

        public static void ApplyTransformationToCollection<T_mod, T_target>(Dictionary<string, T_mod> modifications, List<T_target> targets, EntityFramework edmx, string[] versions)
            where T_target : ITransformable, new()
            where T_mod : BaseModifications
        {
            foreach (var mod in modifications)
            {
                var filteredTargets = targets.Where(x => x.ElementIdentifier.Equals(mod.Key)).ToArray();
                if (filteredTargets.Any())
                {
                    foreach (var target in filteredTargets)
                    {
                        if (mod.Value.Remove)
                        {
                            targets.Remove(target);
                        }
                        else if (versions != null && mod.Value.AvailableInVersions != null && !IsVersionMatch(versions, mod.Value.AvailableInVersions))
                        {
                            targets.Remove(target);
                        }
                        else
                        {
                            target.ApplyTransformation(mod.Value, edmx, versions);
                        }
                    }
                }
                else if (!mod.Value.Remove && (versions == null || mod.Value.AvailableInVersions == null || IsVersionMatch(versions, mod.Value.AvailableInVersions)))
                {
                    // Create the target and apply properties
                    var target = new T_target();
                    target.ElementIdentifier = mod.Key;
                    target.ApplyTransformation(mod.Value, edmx, versions);
                    targets.Add(target);
                }
                else
                {
                    // Hmm, we probably missed something that we expected to find.
                    Console.WriteLine($"Unable to locate {mod.Key}.");
                }
            }
        }

        private static bool IsSimpleType(this PropertyInfo prop)
        {
            var type = prop.PropertyType;
            return IsSimple(type);
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

        private static bool IsCollection(this PropertyInfo prop)
        {
            var t = prop.PropertyType;
            return IEnumerableType.IsAssignableFrom(t) && !StringType.IsAssignableFrom(t);
        }

        private static bool IsDictionary(this PropertyInfo prop)
        {
            var t = prop.PropertyType;
            return t.IsGenericType && t.GetGenericTypeDefinition() == GenericDictionaryType;
        }

        private static bool IsModificationDictionary(this PropertyInfo prop, out Type valueType)
        {
            var t = prop.PropertyType;
            bool isDict = t.IsGenericType && t.GetGenericTypeDefinition() == GenericDictionaryType;
            if (isDict)
            {
                Type keyType = t.GetGenericArguments()[0];
                valueType = t.GetGenericArguments()[1];
                return StringType.IsAssignableFrom(keyType) && BaseModificationsType.IsAssignableFrom(valueType);
            }
            valueType = null;
            return false;
        }
    }
}
