using SadRogue.Primitives;
using Newtonsoft.Json;
using System.IO;
using Color = SadRogue.Primitives.Color;
using SadConsole;
using SadConsole.UI;
using System.Diagnostics;
using System;
using System.Reflection;

namespace ASECII {
    public class Program {
        public static int width = 160;
        public static int height = 90;
        public static int tileSize = 8;

        public static (int, int) calculate(int size) => (width * tileSize / size, height * tileSize / size);

        public static string STATE_FILE = "state.json";
        private static void Main(string[] args) {
            //SadConsole.Settings.UnlimitedFPS = true;
            SadConsole.Settings.UseDefaultExtendedFont = true;
            Settings.WindowTitle = "ASECII";

            SadConsole.Game.Create(width, height, $"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/Assets/IBMCGA+.font", (args, k) => { });
            SadConsole.Game.Instance.Started +=
                (a, k) => Init(args is [{ } path] ? path.Replace("\"", "") : null);
            SadConsole.Game.Instance.Run();
            SadConsole.Game.Instance.Dispose();
		}
        private static void Init(string path = null) {
			if(path != null) {
                Debug.WriteLine($"Opening {path}");
                var model = ASECIILoader.DeserializeObject<SpriteModel>(File.ReadAllText(path));
                if(model.filepath != path) {
                    model.filepath = path;
                    model.Save();
                }
				model.OnLoad();
                FileMenu.SaveRecentFile(path);

				Game.Instance.Screen = new EditorMain(width, height, model);
                return;
            }
            if(LoadState()) {
                return;
            }
            // Create your console
            var c = new FileMenu(width, height, new LoadMode());
            SadConsole.Game.Instance.Screen = c;
            c.FocusOnMouseClick = true;
        }
        public static void SaveState(ProgramState state) {
            if (state != null) {
                File.WriteAllText(STATE_FILE, ASECIILoader.SerializeObject(state));
            } else {
                File.Delete(STATE_FILE);
            }
        }
        public static bool LoadState() {
            if (File.Exists(STATE_FILE)) {
                try {
                    var state = ASECIILoader.DeserializeObject<ProgramState>(File.ReadAllText(STATE_FILE));
                    if(state is EditorState e && File.Exists(e.loaded)) {
                        var model = ASECIILoader.DeserializeObject<SpriteModel>(File.ReadAllText(e.loaded));

                        model.OnLoad();
                        Game.Instance.Screen = new EditorMain(width, height, model);
                        return true;
                    
                    }
                } catch {
                    throw;
                }
            }
            return false;
        }
    }

    public interface ProgramState {
        
    }
    public class EditorState : ProgramState {
        public string loaded;
        public EditorState(string loaded) {
            this.loaded = loaded;
        }
    }
}