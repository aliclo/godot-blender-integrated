#if TOOLS
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

public partial class PipelineAccess: Node
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

    public void Write(string sceneFilePath, PipeContext pipelineContext)
    {
        string filePath = $"{sceneFilePath}.pipelines.json";

        var pipelineContextStore = (PipelineContextStore)pipelineContext.GetData();

        var pipelineContextStores = Read(sceneFilePath);

        if (pipelineContextStores == null)
        {
            pipelineContextStores = new Array<PipelineContextStore>();
        }
        else
        {
            var oldPipelineContextStore = pipelineContextStores.SingleOrDefault(pcs => pcs.Name == pipelineContextStore.Name);
            if (oldPipelineContextStore != null)
            {
                pipelineContextStores.Remove(oldPipelineContextStore);
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
#endif