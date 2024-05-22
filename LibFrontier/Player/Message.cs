using Common;
using LibGamer;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace RogueFrontier;

public interface IPlayerMessage {
    bool Active { get; }
    Tile[] message { get; }
    string text { get; }
    void Flash();
    void Update(double delta);
    Tile[] Draw();
    bool Equals(IPlayerMessage other);
}
public class Transmission : IPlayerMessage {
    public Entity source;
    public Message msg;
    public byte[] sound;
    public Transmission() { }
	public Transmission (Entity source, string text, uint Foreground = ABGR.White, uint Background = ABGR.Black) {
		this.source = source;
		this.msg = new(text, Foreground, Background);
	}
	public Transmission(Entity source, Message msg) {
        this.source = source;
        this.msg = msg;
    }
    public bool Active => msg.Active;
    public Tile[] message => msg.message;
    public string text => msg.text;
    public void Flash() => msg.Flash();
    public Tile[] Draw() => msg.Draw();
    public void Update(double delta) => msg.Update(delta);
    public bool Equals(IPlayerMessage other) {
        return other is Transmission t && t.source == source && t.text == text;
    }
}
public class Message : IPlayerMessage {
    [JsonProperty]
    public Tile[] message { get; private set; }
    public string text { get; private set; }
    public double index;
    public double timeRemaining;
    public double flash;

    private uint Foreground, Background;
    public Message() { }
    public Message(string text, uint Foreground = ABGR.White, uint Background = ABGR.Black) {
        this.Foreground = Foreground;
        this.Background = Background;
        SetMessage(text);
        index = 0;
        timeRemaining = 5;
    }
    public void SetMessage(string text) {
        this.text = text;
		message = [.. text.Select(c => new Tile(Foreground, Background, c))];
	}
    public void Flash() {
        timeRemaining = 2.5;
        flash = 0.25;
    }
    public void Update(double delta) {
        if (index < message.Length) {
            index += Math.Max(20, 3 * (message.Length - index)) * delta;
        } else if (timeRemaining > 0) {
            timeRemaining -= delta;
        }
        if (flash > 0) {
            flash -= delta;
        }
    }
    public bool Scrolling => index < message.Length;
    public bool Active => timeRemaining > 0;
    public Tile[] Draw() {
        var a = (byte)Math.Min(255, timeRemaining * 255);
        var result = Tile.WithA(message[0..(int)Math.Min(index, message.Length)], a, a);
        if (flash > 0) {
            byte value = 255;
            result = result.Select(t => t with { Background = ABGR.RGB(value, 0, 0) }).ToList();
        }
        return [..result];
    }
    public bool Equals(IPlayerMessage other) {
        return other is Message m && m.text == text;
    }
}
