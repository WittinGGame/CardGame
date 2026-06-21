using System;

namespace CardBattle.Core
{
    [Serializable]
    public class RunMapNodeState
    {
        public string nodeId;
        public MapNodeState state;

        public string NodeId => nodeId;
        public MapNodeState State => state;

        public RunMapNodeState()
        {
        }

        public RunMapNodeState(string nodeId, MapNodeState state)
        {
            this.nodeId = nodeId;
            this.state = state;
        }

        public void SetState(MapNodeState newState)
        {
            state = newState;
        }

        public RunMapNodeState Clone()
        {
            return new RunMapNodeState(nodeId, state);
        }
    }
}
