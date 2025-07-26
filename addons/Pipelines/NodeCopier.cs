using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

public class NodeCopier
{

    private static readonly List<string> PropNamesToIgnore = new List<string>() {
        "Node",
        "_import_path",
        "name",
        "unique_name_in_owner",
        "scene_file_path",
        "owner",
        "multiplayer",
        "Process",
        "Node3D",
        "Thread Group",
        "Transform",
        "global_transform",
        "global_position",
        "global_basis",
        "global_rotation",
        "global_rotation_degrees",
        "Visibility",
        "visibility_parent",
        "VisualInstance3D",
        "Sorting",
        "GeometryInstance3D",
        "Geometry",
        "Global Illumination",
        "Visibility Range",
        "MeshInstance3D",
        "Skeleton",
        "MyScript",
        "current_animation_length",
        "current_animation_position",
        "assigned_animation",
        "resource_path"
    };

    public Node CopyValues(Node original, Node altered, List<string[]> excludePropPaths, List<string[]> includePropPaths)
    {
        if (original.GetType() != altered.GetType())
        {
            return altered.Duplicate();
        }

        var result = altered.Duplicate();

        CopyValues(original, altered, result, excludePropPaths, includePropPaths, 0);

        return result;
    }

    public Node CopyValues(Node original, ICloneablePipeValue altered)
    {
        var pipeValue = altered.ClonePipeValue();
        var alteredValue = pipeValue.Value;
        var result = altered.ClonePipeValue().Value;
        
        if (original.GetType() != result.GetType())
        {
            return result;
        }

        CopyValues(original, alteredValue, result, pipeValue.UntouchedProperties, pipeValue.TouchedProperties, 0);

        return result;
    }

    // Traverse the excluded properties, and search by included properties
    // Since you can get finer excluded properties, keep track of which were processed
    // Remove these if they were used and traverse the rest
    private void CopyValues(Variant original, Variant altered, Variant result,
        List<string[]> excludePropPaths, List<string[]> includePropPaths, int depth)
    {
        if (original.VariantType != altered.VariantType || altered.VariantType != result.VariantType)
        {
            throw new Exception("Types don't match");
        }

        var variantType = original.VariantType;

        var excludedPropPathsCurrentDepth = excludePropPaths
            .Where(e => e.Count() - 1 == depth)
            .ToList();

        var includedPropPathsCurrentDepth = includePropPaths
            .Where(i => i.Count() - 1 == depth)
            .ToList();

        var excludedPropPaths = excludedPropPathsCurrentDepth
            .Select(epp => epp[depth])
            .ToList();

        var includedPropPaths = includedPropPathsCurrentDepth
            .Select(ipp => ipp[depth])
            .ToList();

        var excludedPropPathsFutureDepth = excludePropPaths
            .Except(excludedPropPathsCurrentDepth)
            .GroupBy(epp => epp[depth]);

        var includedPropPathsFutureDepth = includePropPaths
            .Except(includedPropPathsCurrentDepth)
            .GroupBy(ipp => ipp[depth]);

        var propPathsFutureDepth = excludedPropPathsFutureDepth
            .Select(epp => epp.Key)
            .Union(includedPropPathsFutureDepth.Select(ipp => ipp.Key))
            .Distinct();

        if (variantType == Variant.Type.Dictionary)
        {
            var originalDict = (Dictionary)original;
            var alteredDict = (Dictionary)altered;
            var resultDict = (Dictionary)result;

            var propKeys = resultDict.Keys.Where(k => k.Obj is not string ks || !PropNamesToIgnore.Contains(ks));
            IEnumerable<Variant> keysOfPropsToCopyFromOrig;
            IEnumerable<Variant> keysOfPropsToCopyFromNew;

            if (excludedPropPaths.Any())
            {
                keysOfPropsToCopyFromOrig = propKeys.Where(p => excludedPropPaths.Contains(p.Obj));
                keysOfPropsToCopyFromNew = propKeys.Where(p => !excludedPropPaths.Contains(p.Obj));
            }
            else if (includedPropPaths.Any())
            {
                keysOfPropsToCopyFromOrig = propKeys.Where(p => !includedPropPaths.Contains(p.Obj));
                keysOfPropsToCopyFromNew = propKeys.Where(p => includedPropPaths.Contains(p.Obj));
            }
            else
            {
                keysOfPropsToCopyFromOrig = Enumerable.Empty<Variant>();
                keysOfPropsToCopyFromNew = Enumerable.Empty<Variant>();
            }

            foreach (var propKey in keysOfPropsToCopyFromOrig)
            {
                resultDict[propKey] = originalDict[propKey];
            }

            foreach (var propKey in keysOfPropsToCopyFromNew)
            {
                resultDict[propKey] = alteredDict[propKey];
            }

            foreach (var propPath in propPathsFutureDepth)
            {
                var excludedCurrentPropPathsFutureDepth = excludedPropPathsFutureDepth
                    .SingleOrDefault(epp => epp.Key == propPath)?.ToList() ?? new List<string[]>();

                var includedCurrentPropPathsFutureDepth = includedPropPathsFutureDepth
                    .SingleOrDefault(epp => epp.Key == propPath)?.ToList() ?? new List<string[]>();

                CopyValues(originalDict[propPath], alteredDict[propPath], resultDict[propPath],
                    excludedCurrentPropPathsFutureDepth, includedCurrentPropPathsFutureDepth, depth + 1);
            }
        }
        else if (variantType == Variant.Type.Object)
        {
            var originalObj = (GodotObject)original;
            var alteredObj = (GodotObject)altered;
            var resultObj = (GodotObject)result;

            var objProps = resultObj
                .GetPropertyList()
                .Select(p => (string)p["name"])
                .Where(pn => !PropNamesToIgnore.Contains(pn));

            IEnumerable<string> keysOfPropsToCopyFromOrig;
            IEnumerable<string> keysOfPropsToCopyFromNew;

            if (excludedPropPaths.Any())
            {
                keysOfPropsToCopyFromOrig = objProps.Where(p => excludedPropPaths.Contains(p));
                keysOfPropsToCopyFromNew = objProps.Where(p => !excludedPropPaths.Contains(p));
            }
            else if (includedPropPaths.Any())
            {
                keysOfPropsToCopyFromOrig = objProps.Where(p => !includedPropPaths.Contains(p));
                keysOfPropsToCopyFromNew = objProps.Where(p => includedPropPaths.Contains(p));
            }
            else
            {
                keysOfPropsToCopyFromOrig = Enumerable.Empty<string>();
                keysOfPropsToCopyFromNew = Enumerable.Empty<string>();
            }

            foreach (var propKey in keysOfPropsToCopyFromOrig)
            {
                resultObj.Set(propKey, originalObj.Get(propKey));
            }

            foreach (var propKey in keysOfPropsToCopyFromNew)
            {
                resultObj.Set(propKey, alteredObj.Get(propKey));
            }

            foreach (var propPath in propPathsFutureDepth)
            {
                var excludedCurrentPropPathsFutureDepth = excludedPropPathsFutureDepth
                    .SingleOrDefault(epp => epp.Key == propPath)?.ToList() ?? new List<string[]>();

                var includedCurrentPropPathsFutureDepth = includedPropPathsFutureDepth
                    .SingleOrDefault(epp => epp.Key == propPath)?.ToList() ?? new List<string[]>();

                CopyValues(originalObj.Get(propPath), alteredObj.Get(propPath), resultObj.Get(propPath),
                    excludedCurrentPropPathsFutureDepth, includedCurrentPropPathsFutureDepth, depth + 1);
            }
        }
    }
}