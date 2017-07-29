using HoloToolkit.Unity.InputModule;

public class DrawButton : ButtonBase {
    public override void OnInputClicked(InputClickedEventData eventData) {
        base.OnInputClicked(eventData);
        GlobalVoiceCommands.Instance.enterDrawingMode();
    }
}
