using System.Collections.Generic;

public class PipelineContextStore
{
    public string Name { get; set; }
    public IList<PipelineNodeStore> Nodes { get; set; }
    public IList<PipelineConnection> Connections { get; set; }
}