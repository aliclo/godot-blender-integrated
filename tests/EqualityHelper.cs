using System.Linq;
using Godot;
using Godot.Collections;

public class EqualityHelper
{

    public string IsEqual(Variant a, Variant b)
    {
        return IsEqual(a, b, "");
    }

    public string IsEqual(Variant a, Variant b, string path)
    {
        if (a.VariantType != b.VariantType)
        {
            return $"{path}: Variant type not equal: ${a.VariantType} != ${b.VariantType}";
        }

        if (a.Obj == b.Obj || a.Obj.Equals(b.Obj))
        {
            return null;
        }

        var varType = a.VariantType;

        if (varType == Variant.Type.Array)
        {
            var arrayA = (Array)a;
            var arrayB = (Array)b;
            if (arrayA.Count != arrayB.Count)
            {
                return $"{path}: Array count mismatch: ${arrayA.Count} != ${arrayB.Count}";
            }

            return arrayA.Zip(arrayB, (ea, eb) => new { ea, eb })
                .Select((r, i) => new { r.ea, r.eb, i })
                .Select(r => IsEqual(r.ea, r.eb, $"{path}[{r.i}]"))
                .FirstOrDefault(r => r != null);
        }
        else if (varType == Variant.Type.Object)
        {
            var objA = (GodotObject)a;
            var objB = (GodotObject)b;

            var propListA = objA.GetPropertyList();
            var propListB = objB.GetPropertyList();

            var propMetaMismatch = IsEqual(propListA, propListB, $"{path}:PropMeta");

            if (propMetaMismatch != null)
            {
                return $"{path}: prop meta mismatch";
            }

            var propNames = propListA.Select(p => (string)p["name"]);

            return propNames
                .Select(pn => IsEqual(objA.Get(pn), objB.Get(pn), $"{path}.{pn}"))
                .FirstOrDefault(r => r != null);
        }
        else if (varType == Variant.Type.Dictionary)
        {
            var dictA = (Dictionary)a;
            var dictB = (Dictionary)b;

            if (dictA.Count != dictB.Count)
            {
                return $"{path}: Dictionary count mismatch: ${dictA.Count} != ${dictB.Count}";
            }

            var abKeys = dictA.Keys.Select(ak => new { ak, bk = dictB.Keys.SingleOrDefault(bk => IsEqual(ak, bk, $"{path}:contains({ak})") == null) });

            var isMissingKey = abKeys.Any(abk => abk.bk.Obj == null);

            if (isMissingKey)
            {
                return $"{path}: Dictionary keys mismatch";
            }

            return abKeys
                .Select(k => IsEqual(dictA[k.ak], dictB[k.bk], $"{path}[{k.ak}]"))
                .FirstOrDefault(r => r != null);
        }
        else
        {
            return $"{path}: {a} != {b}";
        }
    }

}