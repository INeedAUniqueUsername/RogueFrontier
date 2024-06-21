// See https://aka.ms/new-console-template for more information
using ExtSadConsole;
using LibGamer;
using RogueFrontier;
using System.Text;
const int WIDTH = 96, HEIGHT = 50;
SadConsole.Settings.WindowTitle = $"Inspector";
Runner.Run(Fonts.IBMCGA_6X8_FONT, r => {
	r.Go(new EditorMain(WIDTH, HEIGHT));
});
public class EditorMain : IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }
	public Sf sf;

	public object root;
	public EditorMain(int w, int h) {
		sf = new Sf(32, h, Fonts.FONT_6x8);
		SetCurrent(new StationType() {  codename = "aaaaa", name = "Murmur Outpost"});
	}
	void IScene.Update(System.TimeSpan delta) {

	}
	void IScene.HandleMouse(LibGamer.HandState mouse) {
		foreach(var c in controls) c.HandleMouse(mouse);
	}
	void IScene.HandleKey(LibGamer.KB kb) {
		foreach(var c in controls) c.HandleKey(kb);
	}

	void SetCurrent(object current) {
		controls.Clear();
		int y = 0;

		var fields = from f in current.GetType().GetFields(
				System.Reflection.BindingFlags.NonPublic |
				System.Reflection.BindingFlags.Public |
				System.Reflection.BindingFlags.Instance)
					 where f.GetCustomAttributes(false).OfType<InspectField>().Any()
					 select f;

		foreach(var f in fields) {
			new Dictionary<Type, Action> {
				{ typeof(string), () => {
					controls.Add(new SfLabel(sf, (0, y), f.Name));
					var val = (string)f.GetValue(current);
					controls.Add(new SfField(sf, (16, y), 16, val) {
						TextChanged = s => f.SetValue(current, s.text)
					});
				}},
				{ typeof(bool), () => {
					controls.Add(new SfLabel(sf, (0, y), f.Name));
					controls.Add(new SfBool(sf, (16, y)) {
						 StateChanged = s => f.SetValue(current, s.state)
					});

				} }
			}.GetValueOrDefault(f.FieldType)?.Invoke();
			y++;
		}
	}
	List<SfControl> controls = [];
	void IScene.Render(System.TimeSpan delta) {
		sf.Clear();
		foreach(var c in controls) c.Render(delta);
		Draw?.Invoke(sf);
	}
}
public static class Fonts {
	public static string ROOT = "Assets";
	public static string IBMCGA_6X8_FONT { get; } = $"{ROOT}/font/IBMCGA+_6x8.font";
	public static Tf FONT_6x8 { get; } = new Tf(File.ReadAllBytes($"{ROOT}/font/IBMCGA+_6x8.png"), "IBMCGA+_6x8", 6, 8, 192 / 6, 64 / 8, 219);
}