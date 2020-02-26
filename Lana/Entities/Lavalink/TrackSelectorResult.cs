namespace Lana.Entities.Lavalink
{
	public class TrackSelectorResult
	{
		public bool TimedOut { get; set; }
		public bool Cancelled { get; set; }
		public TrackInfo Info { get; set; }
	}
}
