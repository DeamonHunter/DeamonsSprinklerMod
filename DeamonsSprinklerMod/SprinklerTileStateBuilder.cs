using Plukit.Base;
using Staxel.Logic;
using Staxel.Tiles;
using Staxel.TileStates;

namespace DeamonsSprinklerMod {
    class SprinklerTileStateBuilder : ITileStateBuilder {
        public void Dispose() { }
        public void Load() { }

        public string Kind() {
            return "mods.sprinkler.tileState.sprinkler";
        }

        public Entity Instance(Vector3I location, Tile tile, Universe universe) {
            return SprinklerTileStateEntityBuilder.Spawn(universe, tile, location);
        }
    }
}
