using SadConsole.Input;
using SadRogue.Primitives;
using SadConsole;
using Console = SadConsole.Console;
namespace RogueFrontier;
public static partial class SScene {
    public static void ProcessMouseTree(this IScreenObject root, Mouse m) {
        List<IScreenObject> s = new List<IScreenObject>();
        AddChildren(root);
        void AddChildren(IScreenObject parent) {
            s.Add(parent);
            foreach (var c in parent.Children) {
                AddChildren(c);
            }
        }
        foreach (var c in s) {
            c.ProcessMouse(new MouseScreenObjectState(c, m));
        }
    }
}
public class HeroImageDisplay : Console {
    double time;
    string[] heroImage;
    Color tint;
    public HeroImageDisplay(Console prev, string[] heroImage, Color tint) : base(prev.Width, prev.Height) {
        this.heroImage = heroImage;
        this.tint = tint;
    }
    public override void Update(TimeSpan delta) {
        time += delta.TotalSeconds;
    }
    public override void Render(TimeSpan delta) {
        int width = heroImage.Max(line => line.Length);
        int height = heroImage.Length;
        int x = 8;
        int y = (Height - height * 2) / 2;
        byte GetAlpha(int x, int y) {
            return (byte)(Math.Sin(time * 1.5 + Math.Sin(x) * 5 + Math.Sin(y) * 5) * 25 + 230);
        }
        int lineY = 0;
        foreach (var line in heroImage) {
            void DrawLine() {
                for (int lineX = 0; lineX < line.Length; lineX++) {
                    var color = tint.SetAlpha(GetAlpha(lineX, lineY));
                    this.SetCellAppearance(x + lineX, y + lineY, new ColoredGlyph(color, Color.Black, line[lineX]));
                }
                lineY++;
            }
            DrawLine();
            DrawLine();
        }
        base.Render(delta);
    }
}