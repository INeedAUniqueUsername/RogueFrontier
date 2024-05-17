using SadRogue.Primitives;
using Newtonsoft.Json;
using System.IO;
using Color = SadRogue.Primitives.Color;
using SadConsole;
using SadConsole.UI;
using LibGamer;

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
            SadConsole.Game.Create(width, height, "Assets/IBMCGA+.font", (args, k) => { });
            SadConsole.Game.Instance.Started += (a, k) => Init();
            SadConsole.Game.Instance.Run();
            SadConsole.Game.Instance.Dispose();
        }

        private static void Init() {
            if(LoadState()) {
                return;
            }

            // Create your console
            var firstConsole = new FileMenu(width, height, new LoadMode());

            SadConsole.Game.Instance.Screen = firstConsole;
            firstConsole.FocusOnMouseClick = true;
        }
        public static void SaveState(ProgramState state) {
            if (state != null) {
                File.WriteAllText(STATE_FILE, ImageLoader.SerializeObject(state));
            } else {
                File.Delete(STATE_FILE);
            }
        }
        public static bool LoadState() {
            if (File.Exists(STATE_FILE)) {
                try {
                    var state = ImageLoader.DeserializeObject<ProgramState>(File.ReadAllText(STATE_FILE));
                    if(state is EditorState e && File.Exists(e.loaded)) {
                        var sprite = ImageLoader.DeserializeObject<SpriteModel>(File.ReadAllText(e.loaded));

                        sprite.OnLoad();
                        Settings.WindowTitle = $"ASECII: {sprite.filepath}";
                        Game.Instance.Screen = new EditorMain(width, height, sprite);
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