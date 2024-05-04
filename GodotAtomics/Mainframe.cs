using Godot;
using System;

public partial class Mainframe : Node{
	Surface Readout => GetNode<Surface>(nameof(Readout));
	Surface Viewport => GetNode<Surface>(nameof(Viewport));
	public override void _Ready() {
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		Viewport.Print(0, 0, "Hello");
		Viewport.QueueRedraw();
	}
}
