namespace someui.CrossPlatformHeaderless;

public interface INativeHeaderless : IDisposable
{
    void SetHeaderWidget(BaseWidget? wdg);
    void SetMaximizeWidget(BaseWidget? wdg);
}