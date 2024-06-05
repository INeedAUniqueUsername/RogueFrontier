using Godot;
using System;
using System.Linq;

public partial class Mainframe : Node{
	Surface Readout => GetNode<Surface>(nameof(Readout));
	Surface Viewport => GetNode<Surface>(nameof(Viewport));
	(Surface, Surface) Surfaces => (Readout, Viewport);
	public override void _Ready() {
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		var (Readout, Viewport) = Surfaces;
		foreach(var(x,y) in Enumerable.Range(0, Viewport.GridWidth).SelectMany(x => Enumerable.Range(0, Viewport.GridHeight).Select(y => (x, y)))) {
			Viewport.Print(x, y, '#');
		}
		Viewport.QueueRedraw();
	}
}
