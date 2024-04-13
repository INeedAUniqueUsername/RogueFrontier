# ASECII: ASCII Sprite Editor Console II

![Banner](https://user-images.githubusercontent.com/15680274/194045908-117d3e8a-4f01-41bb-b0a1-357310de8ea8.png)

ASECII is a custom-made ASCII painter developed for Rogue Frontier.
## Features
### Menu
- Size
  - Infinite
  - Finite
- Palette
  - Tile
  - Color
- Color Selection
  - Modes: RGBA / HSBA
  - Picker
  - Bars
- History (press Z to show)
- Layer
  - Add
  - Rename
  - Move
  - Merge Down
  - Visible
  - Apply Glyph
  - Apply Foreground
  - Apply Background
### Keyboard controls
- Brush (B)
- Fill (F)
- Type (T)
- Undo (Left Ctrl + U)
- Redo (Left Ctrl + R)
- Shape Select (S): Rect / Circle / Freeform / Polygon
- Wand Select (W): Local / Global
- Deselect (D)
- Move (M)
- Delete (Del)
- Pan (Space)
- Mouse Zoom (Ctrl + Wheel)
- Mouse Scroll (Wheel / Shift + Wheel): Vertical / Horizontal

Image files export to JSON-serialized SpriteModel objects, JSON-serialized `Dictionary<Point, ColoredGlyph>`, and plain text (without color data).
