using System.Threading.Tasks;
using Lana.Entities.Lavalink;

namespace Lana.Entities.Music
{
	public delegate Task NowPlayingEventHandler(TrackInfo track);
}
