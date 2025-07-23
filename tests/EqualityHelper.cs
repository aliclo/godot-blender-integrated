using System.Linq;
using Godot;
using Godot.Collections;

public class EqualityHelper
{

    public bool IsEqual(Variant a, Variant b)
    {
        return IsEqual(a, b, "");
    }

    public void LogFalse(bool enabled, string message)
    {
        if (enabled)
        {
            GD.Print(message);
        }
    }

    public bool IsEqual(Variant a, Variant b, string path)
    {
        bool logsEnabled = path != null;
        if (a.VariantType != b.VariantType)
        {
            LogFalse(logsEnabled, $"{path}: Variant type not equal: ${a.VariantType} != ${b.VariantType}");
            return false;
        }

        if (a.Obj == b.Obj || a.Obj.Equals(b.Obj))
        {
            return true;
        }

        var varType = a.VariantType;

        if (varType == Variant.Type.Array)
        {
            var arrayA = (Array)a;
            var arrayB = (Array)b;
            if (arrayA.Count != arrayB.Count)
            {
                LogFalse(logsEnabled, $"{path}: Array count mismatch: ${arrayA.Count} != ${arrayB.Count}");
                return false;
            }

            return arrayA.Zip(arrayB, (ea, eb) => new { ea, eb })
                .Select((r, i) => new { r.ea, r.eb, i })
                .All(r => IsEqual(r.ea, r.eb, $"{path}[{r.i}]"));
        }
        else if (varType == Variant.Type.Object)
        {
            var objA = (GodotObject)a;
            var objB = (GodotObject)b;

            var propListA = objA.GetPropertyList();
            var propListB = objB.GetPropertyList();

            var isPropMetaEqual = IsEqual(propListA, propListB, $"{path}:PropMeta");

            if (!isPropMetaEqual)
            {
                LogFalse(logsEnabled, $"{path}: prop meta mismatch");
                return false;
            }

            var propNames = propListA.Select(p => (string)p["name"]);

            return propNames.All(pn => IsEqual(objA.Get(pn), objB.Get(pn), $"{path}.{pn}"));
        }
        else if (varType == Variant.Type.Dictionary)
        {
            var dictA = (Dictionary)a;
            var dictB = (Dictionary)b;

            if (dictA.Count != dictB.Count)
            {
                LogFalse(logsEnabled, $"{path}: Dictionary count mismatch: ${dictA.Count} != ${dictB.Count}");
                return false;
            }

            var abKeys = dictA.Keys.Select(ak => new { ak, bk = dictB.Keys.SingleOrDefault(bk => IsEqual(ak, bk, null)) });

            var missingKey = abKeys.FirstOrDefault(abk => abk.bk.Obj == null);

            if (missingKey != null)
            {
                LogFalse(logsEnabled, $"{path}: Dictionary keys mismatch for key {missingKey.ak}");
                return false;
            }

            return abKeys.All(k => IsEqual(dictA[k.ak], dictB[k.bk], $"{path}[{k.ak}]"));
        }
        else
        {
            LogFalse(logsEnabled, $"{path}: {a} != {b}");
            return false;
        }
    }

}