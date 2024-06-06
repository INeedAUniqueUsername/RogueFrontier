using Godot;
using System;
using System.Linq;
public partial class Mainframe : Node {
	public static int WIDTH = 100, HEIGHT = 60;
	public override void _Ready() {
		var r = new Runner();
		AddChild(r);
		r.Go(new LibTerminator.Mainframe(WIDTH, HEIGHT));
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		
	}
}
