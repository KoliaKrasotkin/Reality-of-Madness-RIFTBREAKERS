using UnityEngine.UI;
namespace ROM.UI.Sprint01
{
    public sealed class RiftStyledButton : Button
    {
        protected override void DoStateTransition(SelectionState state,bool instant)
        {
            var frame=targetGraphic as RiftButtonGraphic;
            if(frame)frame.SetVisualState((int)state,instant);
        }
    }
}
