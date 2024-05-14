using SadConsole;
using SadConsole.Input;
using Console = SadConsole.Console;

namespace RogueFrontier;
public class TradeMenu : Console {
    ScreenSurface prev;
    Player player;
    GetPrice GetBuyPrice, GetSellPrice;

    public TradeMenu(ScreenSurface prev, PlayerShip playerShip, ITrader docked, GetPrice GetBuyPrice, GetPrice GetSellPrice) : base(prev.Surface.Width, prev.Surface.Height) {
        this.prev = prev;
        this.player = playerShip.person;
        
        this.GetBuyPrice = GetBuyPrice;
        this.GetSellPrice = GetSellPrice;
    }
    public void Transact() {
    }
    public void Exit() {
        var p = Parent;
        p.Children.Remove(this);
        if (prev != null) {
            p.Children.Add(prev);
            prev.IsFocused = true;
        } else {
            p.IsFocused = true;
        }
    }
    public override bool ProcessKeyboard(Keyboard keyboard) {
        return base.ProcessKeyboard(keyboard);
    }
    public override void Update(TimeSpan delta) {
        base.Update(delta);
    }
    public override void Render(TimeSpan delta) {
    }
}