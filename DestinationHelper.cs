using System.Linq;
using Godot;
using Godot.Collections;

public class DestinationHelper {
    
    public void HandleDestinationChange(DestinationPropertyInfo destinationPropertyInfo)
    {
        PipeContext pipeContext = destinationPropertyInfo.PipeContext;
        Node node = destinationPropertyInfo.Node;
        string destinationNodeName = destinationPropertyInfo.DestinationNodeName;
        Array<NodePath> previousDestinationPaths = destinationPropertyInfo.PreviousDestinationPaths;
        Array<NodePath> newDestinationPaths = destinationPropertyInfo.NewDestinationPaths;
        ICloneableValue cloneableValue = destinationPropertyInfo.CloneableValue;

        var destinationPipesPathsToRemove = previousDestinationPaths.Except(newDestinationPaths);
        var destinationPipesToRemove = destinationPipesPathsToRemove.Select(p => node.GetNodeOrNull<IReceivePipe>(p)).Where(p => p != null);

        foreach(var destinationPipeToRemove in destinationPipesToRemove) {
            destinationPipeToRemove.PipeDisconnect();
        }

        if(!node.IsNodeReady()) {
            return;
        }

        if(pipeContext != null) {
            var destinationPipesPathsToAdd = newDestinationPaths.Except(previousDestinationPaths);
            var destinationPipesToAdd = destinationPipesPathsToAdd.Select(p => node.GetNodeOrNull<IReceivePipe>(p)).Where(p => p != null);

            foreach(var destinationPipeToAdd in destinationPipesToAdd) {
                destinationPipeToAdd.Register(pipeContext, destinationNodeName);
                if(cloneableValue != null) {
                    pipeContext.ProcessPipe(destinationPipeToAdd, cloneableValue);
                }
            }
        }
    }

}