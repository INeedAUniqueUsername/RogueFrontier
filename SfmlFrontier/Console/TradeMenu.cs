using LibGamer;
using SadConsole;
using SadConsole.Input;
using Console = SadConsole.Console;

namespace RogueFrontier;
public class TradeMenu : IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }

	IScene prev;
    Player player;
    GetPrice GetBuyPrice, GetSellPrice;

    public Sf sf;


	public TradeMenu((IScene scene, Sf sf) prev, PlayerShip playerShip, ITrader docked, GetPrice GetBuyPrice, GetPrice GetSellPrice) {
        this.sf = new Sf(prev.sf.GridWidth, prev.sf.GridHeight, Fonts.FONT_6x8);
		this.prev = prev.scene;
        this.player = playerShip.person;
        
        this.GetBuyPrice = GetBuyPrice;
        this.GetSellPrice = GetSellPrice;
    }
    public void Transact() {

    }
    public void Exit() {
        prev.Show();
    }
    public void HandleKey(KB keyboard) {
    }
    public void Update(TimeSpan delta) {
    }
    public void Render(TimeSpan delta) {
    }
}