using System;
using System.Linq;
using System.Reflection;
using Godot;
using Godot.Collections;

public static class GodotJsonParser
{

    public static Variant ToJsonType(Variant obj)
    {
        if (obj.VariantType == Variant.Type.Array)
        {
            var array = (Godot.Collections.Array)obj;
            return new Godot.Collections.Array(array.Select(ToJsonType));
        }
        else if (obj.VariantType == Variant.Type.Object)
        {
            var godotObject = (GodotObject)obj;

            var dictionary = new Dictionary();

            foreach (var prop in godotObject.GetPropertyList().Where(p => (((int)p["usage"]) & (int)PropertyUsageFlags.ScriptVariable) == (int)PropertyUsageFlags.ScriptVariable))
            {
                var propName = (string)prop["name"];
                var propValue = godotObject.Get(propName);

                dictionary[propName] = ToJsonType(propValue);
            }

            return dictionary;
        }
        else
        {
            return obj;
        }
    }
    
    public static T FromJsonType<T>(Variant obj)
    {
        return (T)FromVariant(typeof(T), obj);
    }

    private static object FromVariant(Type type, Variant obj)
    {
        object instance;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Array<>))
        {
            instance = Activator.CreateInstance(type);
            if (obj.VariantType != Variant.Type.Array)
            {
                GD.PrintErr("Expected array but was ", obj.VariantType, " instead");
                return instance;
            }

            var arrayStore = (Array<Variant>)obj;
            var typeArgument = type.GetGenericArguments()[0];

            var addMethod = type.GetMethod("Add", new Type[] { typeArgument });

            foreach (var element in arrayStore)
            {
                addMethod.Invoke(instance, new object[] { FromVariant(typeArgument, element) });
            }
        }
        else if (obj.VariantType == Variant.Type.Dictionary)
        {
            if (type == typeof(Variant))
            {
                instance = obj;
            }
            else
            {
                instance = Activator.CreateInstance(type);
                var dictionary = (Dictionary)obj;
                FromDictionary(instance, dictionary);
            }
        }
        else
        {
            instance = Convert.ChangeType(obj.Obj, type);
        }

        return instance;
    }

    private static void FromDictionary(object instance, Dictionary store)
    {
        var type = instance.GetType();
        
        var properties = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.DeclaringType != typeof(GodotObject));

        foreach (var prop in properties)
        {
            var propValue = FromVariant(prop.PropertyType, store[prop.Name]);
            prop.SetValue(instance, propValue);
        }
    }

}