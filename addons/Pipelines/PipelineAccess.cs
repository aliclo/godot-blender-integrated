using System.Linq;
using System.Text.Json;
using Godot;

public class PipelineAccess {

    public object Read(string sceneFilePath) {
        // TODO: Handle multiple contexts within the same pipeline file
        string filePath = $"{sceneFilePath}.pipelines.json";

        if(!FileAccess.FileExists(filePath)) {
            return null;
        }

        string pipelineData;
        using(var pipelineFile = FileAccess.Open(filePath, FileAccess.ModeFlags.Read)) {
            pipelineData = pipelineFile.GetAsText();
        }
        
        return JsonSerializer.Deserialize<PipelineContextStore>(pipelineData);
    }

    public void Write(string sceneFilePath, PipelineGraph pipelineEditor) {
        string filePath = $"{sceneFilePath}.pipelines.json";

        var pipelineContextStore = pipelineEditor.GetData();

        using(var pipelineFile = FileAccess.Open(filePath, FileAccess.ModeFlags.Write)) {
            var json = JsonSerializer.Serialize(pipelineContextStore);
            pipelineFile.StoreLine(json);
        }
    }

}