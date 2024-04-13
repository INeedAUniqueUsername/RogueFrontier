using Common;
using Newtonsoft.Json;
using SadConsole;
using SadRogue.Primitives;
using SFML.Audio;
using System;
using System.Linq;

namespace RogueFrontier;

public interface IPlayerMessage {
    bool Active { get; }
    ColoredString message { get; }
    string text => message.String;
    void Flash();
    void Update(double delta);
    ColoredString Draw();
    bool Equals(IPlayerMessage other);
}
public class Transmission : IPlayerMessage {
    public Entity source;
    public Message info;
    public Sound sound;
    public Transmission() { }
    public Transmission(Entity source, string message) {
        this.source = source;
        this.info = new Message(message);
    }
    public Transmission(Entity source, ColoredString message) {
        this.source = source;
        this.info = new Message(message);
    }
    public bool Active => info.Active;
    public ColoredString message => info.message;
    public string text => message.String;
    public void Flash() => info.Flash();
    public ColoredString Draw() => info.Draw();
    public void Update(double delta) => info.Update(delta);
    public bool Equals(IPlayerMessage other) {
        return other is Transmission t && t.source == source && t.text == text;
    }
}
public class Message : IPlayerMessage {
    [JsonProperty]
    public ColoredString message { get; private set; }
    public string text => message.String;
    public double index;
    public double timeRemaining;
    public double flash;
    public Message() { }
    public Message(string text) : this(
        new ColoredString(text, Color.White, Color.Black)) {
    }
    public Message(ColoredString message) {
        this.message = message;
        index = 0;
        timeRemaining = 5;
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
    public ColoredString Draw() {
        var a = (byte)Math.Min(255, timeRemaining * 255);
        var result = message.SubString(0, (int)Math.Min(index, message.Length)).WithOpacity(a, a);
        if (flash > 0) {
            var value = 255;
            result.SetBackground(new Color(value, 0, 0));
        }
        return result;
    }
    public bool Equals(IPlayerMessage other) {
        return other is Message m && m.text == text;
    }
}
