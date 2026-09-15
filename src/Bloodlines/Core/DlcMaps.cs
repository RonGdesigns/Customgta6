using System;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// The one place that registers the shipped DLC map data, and the one way to ask for an
    /// IPL.
    ///
    /// Story Mode does not have the DLC map archives registered by default. A single native
    /// registers them, and until it has run, `REQUEST_IPL` on anything that came with a DLC
    /// pack quietly does nothing: the name never goes active, and whatever was waiting for it
    /// reports that its map did not load.
    ///
    /// This is not a theoretical hazard. That registration used to happen as a side effect of
    /// the bunker drawing its map blip on the first frame of every session — which is what
    /// gave Ron a loading screen at every startup. Moving it out of startup fixed the loading
    /// screen and broke the Paleto heist, because the cove yacht is Cayo Perico map data and
    /// had been relying on the bunker to register it. One subsystem was paying for another's
    /// dependency without either of them saying so.
    ///
    /// So it lives here, every IPL request goes through <see cref="RequestIpl"/>, and the
    /// registration happens on the first request of the session rather than at startup. It
    /// still costs a pause the first time. It is now paid for by whoever actually needs it,
    /// at the moment they need it.
    ///
    /// This loads map data the game already shipped with. It does not join or enable GTA
    /// Online, and nothing here should ever be changed in a direction that could.
    /// </summary>
    public static class DlcMaps
    {
        /// <summary>_LOAD_MP_DLC_MAPS: registers the shipped DLC map archives for Story Mode.</summary>
        private const ulong RegisterDlcMaps = 0x0888C3502DBBEEF5UL;

        private static bool _registered;

        /// <summary>Whether the DLC map data has been registered this session.</summary>
        public static bool Registered => _registered;

        /// <summary>
        /// Register the shipped DLC map archives, once per session. Safe to call from
        /// anywhere; the second call and every one after it do nothing.
        /// </summary>
        public static void EnsureRegistered()
        {
            if (_registered) return;
            try
            {
                Function.Call((Hash)RegisterDlcMaps);
                _registered = true;
                Logger.Info("Registered the shipped DLC map data for Story Mode. This is map data the game already has; it does not join Online.");
            }
            catch (Exception ex) { Logger.Error("Registering the shipped DLC map data", ex); }
        }

        /// <summary>
        /// Ask for a map by name, registering the DLC archives first if they are not already.
        /// Use this instead of calling REQUEST_IPL directly: a DLC name requested before the
        /// registration simply never becomes active, and the failure surfaces somewhere else
        /// entirely as a map that did not load.
        /// </summary>
        public static void RequestIpl(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            EnsureRegistered();
            try { Function.Call(Hash.REQUEST_IPL, name); }
            catch (Exception ex) { Logger.Error("Requesting map " + name, ex); }
        }
    }
}
