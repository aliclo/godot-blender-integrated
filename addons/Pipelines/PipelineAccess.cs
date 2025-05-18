using System.Collections.Generic;
using System.Text.Json;
using Godot;

public class PipelineAccess {

    public List<PipelineContextStore> Read(string sceneFilePath) {
        string filePath = $"{sceneFilePath}.pipelines.json";

        if(!FileAccess.FileExists(filePath)) {
            return null;
        }

        string pipelineData;
        using(var pipelineFile = FileAccess.Open(filePath, FileAccess.ModeFlags.Read)) {
            pipelineData = pipelineFile.GetAsText();
        }
        
        return JsonSerializer.Deserialize<List<PipelineContextStore>>(pipelineData);
    }

    public void Write(string sceneFilePath, PipelineGraph pipelineEditor) {
        string filePath = $"{sceneFilePath}.pipelines.json";

        var pipelineContextStore = (PipelineContextStore) pipelineEditor.GetData();

        var pipelineContextStores = Read(sceneFilePath);

        if (pipelineContextStores == null)
        {
            pipelineContextStores = new List<PipelineContextStore>();
        }
        else
        {
            pipelineContextStores.RemoveAll(pcs => pcs.Name == pipelineContextStore.Name);
        }

        pipelineContextStores.Add(pipelineContextStore);

        using (var pipelineFile = FileAccess.Open(filePath, FileAccess.ModeFlags.Write))
        {
            var json = JsonSerializer.Serialize(pipelineContextStores);
            pipelineFile.StoreLine(json);
        }
    }

}