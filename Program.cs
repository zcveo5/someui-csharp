using SkiaSharp;

namespace someui;


public class Program
{
    static void Main(string[] args)
    {
        var window = new Window("Hello World", height: 600, headerless: true);
        var rect = new Container(window, SKColors.DimGray);
        rect.Place(new PlacementConfig
        {
            X = new PlacementValue(PlacementType.Relative, 0.05f),
            Y = new PlacementValue(PlacementType.Relative, 0.05f),
            Width = new PlacementValue(PlacementType.Relative, 0.9f),
            Height = new PlacementValue(PlacementType.Relative, 0.9f)
        });

        var label = new Label(rect, "Hello world!", backgroundColor: SKColors.Gray);
        label.PlacementConfig.Y.Type = PlacementType.Auto;

        var buttons = new Container(rect, SKColors.DimGray);
        buttons.PlacementConfig.Y.Type = PlacementType.Auto;
        buttons.ChildrenPlacementConfig.Propagate = true;
        
        var btn = new Button(buttons, "Hello button!", () => { label.SetText($"[{label.Text}]"); }, backgroundIdleColor: SKColors.Gray);
        btn.PlacementConfig.Y.Type = PlacementType.Auto;
            
        var container2 = new Container(rect, SKColors.Red);
        container2.Name = "Container2";
        container2.PlacementConfig.Y.Type = PlacementType.Auto;
        container2.ChildrenPlacementConfig.Propagate = true;

        for (int i = 0; i < 32; i++)
        {
            var l = new Label(container2, $"Iter{i}") { Name = $"Iter{i}"};
            l.PlacementConfig.Y.Type = PlacementType.Auto;
        }
        
        var btnDump = new Button(buttons, "Dump", () =>
        {
            foreach (var child in container2.Children)
            {
                Console.WriteLine($"{child} {child.ActualX} {child.ActualY} {child.ActualWidth} {child.ActualHeight}");
            }
        }, backgroundIdleColor: SKColors.Gray);
        btnDump.PlacementConfig.Y.Type = PlacementType.Auto;
        
        var btnReload = new Button(buttons, "Reload", () =>
        {
            foreach (var child in container2.Children)
            {
                if (child is Label)
                {
                    var lbl = (Label)child;
                    lbl.SetText($"{child.Name}");
                }
            }
        }, backgroundIdleColor: SKColors.Gray);
        btnReload.PlacementConfig.Y.Type = PlacementType.Auto;
        
        window.Run();
    }
}