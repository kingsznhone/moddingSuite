namespace BGManager;

public class SimpleBG
{
	private BG _bg = null;

	public uint Id => _bg.Id;

	public string Name => _bg.Name;

	public string Country => _bg.Country;

	public string Campaign => _bg.CD.Name;

	public SimpleBG(BG bg)
	{
		_bg = bg;
	}
}
