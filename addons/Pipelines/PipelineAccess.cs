using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

public class PipelineAccess
{

    public Array<PipelineContextStore> Read(string sceneFilePath)
    {
        string filePath = $"{sceneFilePath}.pipelines.json";

        if (!FileAccess.FileExists(filePath))
        {
            return null;
        }

        string pipelineData;
        using (var pipelineFile = FileAccess.Open(filePath, FileAccess.ModeFlags.Read))
        {
            pipelineData = pipelineFile.GetAsText();
        }

        var pipelineJson = Json.ParseString(pipelineData);

        var model = GodotJsonParser.FromJsonType<Array<PipelineContextStore>>(pipelineJson);

        return model;
    }

    public void Write(string sceneFilePath, PipelineGraph pipelineEditor)
    {
        string filePath = $"{sceneFilePath}.pipelines.json";

        var pipelineContextStore = (PipelineContextStore)pipelineEditor.GetData();

        var pipelineContextStores = Read(sceneFilePath);

        if (pipelineContextStores == null)
        {
            pipelineContextStores = new Array<PipelineContextStore>();
        }
        else
        {
            foreach (var contextStore in pipelineContextStores.Where(pcs => pcs.Name == pipelineContextStore.Name))
            {
                pipelineContextStores.Remove(contextStore);
            }
        }

        pipelineContextStores.Add(pipelineContextStore);

        var pipelineContextStoreDict = GodotJsonParser.ToJsonType(pipelineContextStores);

        using (var pipelineFile = FileAccess.Open(filePath, FileAccess.ModeFlags.Write))
        {
            var json = Json.Stringify(pipelineContextStoreDict);
            pipelineFile.StoreLine(json);
        }
    }

}