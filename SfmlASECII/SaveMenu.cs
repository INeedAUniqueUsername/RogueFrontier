using Newtonsoft.Json;
using SadConsole;
using SadConsole.Input;
using SadConsole.UI;
using SadConsole.UI.Controls;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using static SadConsole.Input.Keys;
using Console = SadConsole.Console;
using LibSadConsole;
using Newtonsoft.Json.Serialization;
using System.Threading.Tasks;

namespace ASECII {
    public interface FileMode {
        string InitialPath { get; }
        void Enter(Console console, string text);
    }
    public static class SFileMode {
        public static readonly JsonSerializerSettings settings = new JsonSerializerSettings {
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            TypeNameHandling = TypeNameHandling.All,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
    }
    class SaveMode : FileMode {
        SpriteModel model;
        Console renderer;
        public string InitialPath => model.filepath ?? Environment.CurrentDirectory;
        public SaveMode(SpriteModel model, Console renderer) {
            this.model = model;
            this.renderer = renderer;
        }
        public void Enter(Console console, string filepath) {
            model.filepath = filepath;
            model.Save();
            Game.Instance.Screen = console;
        }
    }
    class LoadMode : FileMode {
        public string InitialPath => Environment.CurrentDirectory;
        public LoadMode() {

        }
        public void Enter(Console console, string filepath) {
            var Width = console.Width;
            var Height = console.Height;

            Settings.WindowTitle = $"ASECII: {filepath}";
            if (File.Exists(filepath)) {
                try {
                    var sprite = ASECIILoader.DeserializeObject<SpriteModel>(File.ReadAllText(filepath));

                    if(sprite.filepath != filepath) {
                        sprite.filepath = filepath;
                        File.WriteAllText($"{filepath}", ASECIILoader.SerializeObject(sprite));
                    }

                    sprite.OnLoad();
                    Game.Instance.Screen = new EditorMain(Width, Height, sprite);

                    Program.SaveState(new EditorState(filepath));
                } catch {
                    throw;
                }
            } else {
                var model = new SpriteModel(Width, Height) { filepath = filepath };
                model.sprite.layers.Add(new Layer());
                File.WriteAllText(filepath, ASECIILoader.SerializeObject(model));
                console.Children.Add(new EditorMain(Width, Height, model));
            }
        }
    }
    public interface ILoadResult {}
    public class LoadFailure : ILoadResult {
        public string message;
        public LoadFailure(string message) => this.message = message;
    }
    public class LoadBusy : ILoadResult {
        public LoadBusy() { }
    }
    public class LoadNonexistent : ILoadResult {
        public LoadNonexistent() { }
    }
    public class LoadSuccess : ILoadResult {
        public SpriteModel preview;
        public LoadSuccess(SpriteModel preview) => this.preview = preview;
    }
    class FileMenu : ControlsConsole {
        public static string RECENTFILES = "RecentFiles.json";

        SpriteModel hoveredFile;
        Dictionary<string, ILoadResult> preloaded;

        public static HashSet<string> LoadRecentFiles () => File.Exists(RECENTFILES) ? ASECIILoader.DeserializeObject<HashSet<string>>(File.ReadAllText(RECENTFILES)).Where(f => File.Exists(f)).ToHashSet() : new HashSet<string>();


		HashSet<string> recentFiles;
        List<LabelButton> recentListing;
        
        List<LabelButton> folderListing;
        TextField textbox;
        FileMode mode;

        int folderListingX;

        string console = "";

        public FileMenu(int width, int height, FileMode mode) : base(width, height) {
            this.recentFiles = LoadRecentFiles();
            this.preloaded = new Dictionary<string, ILoadResult>();
            this.recentListing = new List<LabelButton>();
            int n = 3;

            if(recentFiles.Any()) {
                folderListingX = 32;
                foreach (var f in recentFiles) {
                    var p = Path.GetFileName(f);
                    var b = new LabelButton(p, Load, () => textbox.text = f) {
                        Position = new Point(4, n),
                    };

                    b.MouseEnter += (e, args) => {
                        ShowPreview(f);
                    };

                    this.Children.Add(b);
                    recentListing.Add(b);
                    n++;

                    void Load() {
                        mode.Enter(this, f);
                        //AddRecentFile(f);
                    }
                }
            } else {
                folderListingX = 8;
            }
            

            this.mode = mode;
            UseMouse = true;
            UseKeyboard = true;
            IsFocused = true;
            FocusOnMouseClick = true;
            folderListing = new List<LabelButton>();

            textbox = new TextField(width - folderListingX) {
                Position = new Point(folderListingX, 1),
                
                UseKeyboard = true,
                UseMouse = true,
                IsFocused = true,
                text = mode.InitialPath,
            };
            textbox.TextChanged += tf => UpdateListing(textbox.text);
            textbox.EnterPressed += tf => EnterFile();
            this.Children.Add(textbox);
            UpdateListing(textbox.text);
        }

        public void AddRecentFile(string s) {
            recentFiles.Add(s);
            File.WriteAllText(RECENTFILES, ASECIILoader.SerializeObject(recentFiles));
        }

		public static void SaveRecentFile (string s) {
            var recentFiles = LoadRecentFiles();
			recentFiles.Add(s);
			File.WriteAllText(RECENTFILES, ASECIILoader.SerializeObject(recentFiles));
		}
		public void UpdateListing(string filepath) {
            folderListing.ForEach(b => this.Children.Remove(b));
            folderListing.Clear();
            int i = 2;
            if (string.IsNullOrWhiteSpace(filepath)) {
                filepath = Environment.CurrentDirectory;
            } else {
                filepath = Path.GetFullPath(filepath);
            }
            if (Directory.Exists(filepath)) {

                i++;
                var b = new LabelButton("..", () => textbox.text = Directory.GetParent(filepath).FullName) {
                    Position = new Point(folderListingX, i),
                };
                folderListing.Add(b);

                ShowDirectories(Directory.GetDirectories(filepath).Where(p => p.StartsWith(filepath)));
                ShowFiles(Directory.GetFiles(filepath).Where(p => p.StartsWith(filepath)));
            } else {
                var parent = Directory.GetParent(filepath)?.FullName;
                if (Directory.Exists(parent)) {
                    i++;
                    var b = new LabelButton("..", () => textbox.text = parent) {
                        Position = new Point(folderListingX, i),
                    };
                    folderListing.Add(b);

                    ShowDirectories(Directory.GetDirectories(parent).Where(p => p.StartsWith(filepath)));
                    ShowFiles(Directory.GetFiles(parent).Where(p => p.StartsWith(filepath)));
                }
            }
            foreach (var button in folderListing.Take(64)) {
                Children.Add(button);
            }
            void ShowDirectories(IEnumerable<string> directories) {
                foreach (var directory in directories) {
                    i++;
                    var b = new LabelButton(Path.GetFileName(directory) + "\\", () => {
                        textbox.text = directory + "\\";
                        textbox.index = textbox.text.Length;
                    }) {
                        Position = new Point(folderListingX, i),
                    };
                    folderListing.Add(b);
                }
            }
            void ShowFiles(IEnumerable<string> files) {
                foreach (var file in files) {
                    i++;
                    var b = new LabelButton(Path.GetFileName(file), Load) {
                        Position = new Point(folderListingX, i),
                    };
                    b.MouseEnter += (e, args) => {
                        ShowPreview(file);
                    };
                    folderListing.Add(b);

                    void Load() {
                        EnterFile(file);
                    }
                }
            }
        }
        public void Log(string message) {
            console = $"{message}";
        }
        public void ShowPreview(string file) {
            if (preloaded.TryGetValue(file, out var result)) {
                Handle(file, result);
            } else {
                Load(file);
            }
        }
        public void Handle(string file, ILoadResult result) {
            switch (result) {
                case LoadBusy:
                    Log($"[{file}]\nLoading...");
                    hoveredFile = null;
                    break;
                case LoadFailure f:
                    Log($"[{file}]\n{f.message}");
                    hoveredFile = null;
                    break;
                case LoadSuccess s:
                    Log($"[{file}]\nLoaded successfully");
                    hoveredFile = s.preview;
                    break;

                case LoadNonexistent s:
                    Log($"[{file}]\nDoes not exist");
                    hoveredFile = null;
                    break;
            }
        }
        public async Task Load(string file) {
            if(!File.Exists(file)) {
                preloaded[file] = new LoadNonexistent();
                return;
            }

            preloaded[file] = new LoadBusy();
            await Task.Run(StartLoad);
            void StartLoad() {
                try {
                    var model = ASECIILoader.DeserializeObject<SpriteModel>(File.ReadAllText(file));
                    if (model?.filepath == null) {
                        Failed("Filepath does not exist");
                        return;
                    }
                    model.filepath = Path.GetFullPath(file);
                    model.Save();
                    var s = new LoadSuccess(model);
                    preloaded[file] = s;
                    Handle(file, s);
                } catch (Exception e) {
                    Failed(e.Message);
                }
                void Failed(string message) {
                    var f = new LoadFailure(message);
                    preloaded[file] = f;
                    Handle(file, f);
                }
            }
        }
        public override void Render(TimeSpan delta) {
            base.Render(delta);
            this.Clear();

            var c1 = new Color(25, 25, 25);
            var c2 = new Color(51, 51, 51);
            for (int x = 0; x < Width; x++) {
                for(int y = 0; y < Height; y++) {
                    this.SetBackground(x, y, (x + y) % 2 == 0 ? c1 : c2);
                }
            }
            if (hoveredFile != null && hoveredFile.sprite != null) {
                var s = hoveredFile.sprite;
                var previewX = (Width - (s.end - s.origin).X) < 64 ? 0 : 64;
                var previewY = 0;
                var origin = hoveredFile.sprite.origin;
                
                var previewStart = new Point(previewX, previewY);
                for (int x = previewX; x < Width; x++) {
                    for (int y = previewY; y < Height; y++) {
                        var screenPos = new Point(x, y);
                        var spritePos = screenPos - previewStart + origin;
                        if (hoveredFile.sprite.preview.TryGetValue(spritePos, out var tile)) {
                            this.SetCellAppearance(x, y, tile);
                        }
                    }
                }
            }
            int yy = Height - 16;
            foreach(var line in console.Split('\n')) {
                this.Print(16, yy++, line, Color.White, Color.Black);
            }
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            if (keyboard.IsKeyPressed(Enter)) {
                EnterFile();
            }
            return base.ProcessKeyboard(keyboard);
        }
        public async Task EnterFile() => await EnterFile(textbox.text);

        public async Task EnterFile(string f) {
            if (preloaded.TryGetValue(f, out var result)) {
                if (result is LoadSuccess || result is LoadNonexistent) {
                    mode.Enter(this, f);
                    AddRecentFile(f);
                } else {
                    Handle(f, result);
                }
            } else {
                await Load(f);
                EnterFile(f);
            }
        }
    }

}
