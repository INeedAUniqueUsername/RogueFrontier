using LibGamer;
namespace RogueFrontier;
public interface IProgram {
	public void Draw (Sf sf);
	public void PlaySound (SoundCtx s);
	public void Go (IScene scene);
}