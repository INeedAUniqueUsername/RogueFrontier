using SadConsole;

namespace IslandHopper;

class Animation {
    int tick = 0;
    private ColoredGlyph main;
    private ColoredGlyph[] frames;
    public Animation(ColoredGlyph main, params ColoredGlyph[] frames) {
        this.main = main;
        this.frames = frames;
    }
    public void Set(params ColoredGlyph[] frames) {
        this.frames = frames;
    }
    public void Update() {
        tick++;
    }
    public ColoredGlyph GetFrame() {
        int mod = tick % (30 + frames.Length * 10);
        if (mod < 30) {
            return main;
        } else {
            mod -= 30;
            return frames[mod / 10];
        }
    }
}
