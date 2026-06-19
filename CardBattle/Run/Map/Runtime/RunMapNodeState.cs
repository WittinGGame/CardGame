using System;

namespace CardBattle.Core
{
    [Serializable]
    public class RunMapNodeState
    {
        [UnityEngine.SerializeField] private string nodeId;
        [UnityEngine.SerializeField] private MapNodeState state;

        public string NodeId => nodeId;
        public MapNodeState State => state;

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
