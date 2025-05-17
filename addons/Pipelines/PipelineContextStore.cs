using System.Collections.Generic;

public class PipelineContextStore
{
    public IList<PipelineNodeStore> Nodes { get; set; }
    public IList<PipelineConnection> Connections { get; set; }
}