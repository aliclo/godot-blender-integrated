using Godot.Collections;

public class PipeMapper
{
    
    public PipelineNodeStore Map(PipelineNode pipelineNode)
    {
        return new PipelineNodeStore()
        {
            Name = pipelineNode.Name,
            Type = pipelineNode.GetType().Name,
            X = pipelineNode.PositionOffset.X,
            Y = pipelineNode.PositionOffset.Y,
            Data = pipelineNode.GetData()
        };
    }

    public PipelineConnectionStore Map(Dictionary connection)
    {
        return new PipelineConnectionStore()
        {
            FromNodeName = (string)connection["from_node"],
            FromPort = (int)connection["from_port"],
            ToNodeName = (string)connection["to_node"],
            ToPort = (int)connection["to_port"]
        };
    }

}