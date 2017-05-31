using System;
using System.Collections.Generic;
using Plukit.Base;
using Staxel;
using Staxel.Effects;
using Staxel.Logic;
using Staxel.Particles;
using Staxel.Tiles;
using Staxel.TileStates;

namespace DeamonsSprinklerMod {
    internal sealed class SprinklerTileStateEntityLogic : TileStateEntityLogic {
        internal SprinklerComponentBuilder.SprinklerComponent SprinklerComp;
        internal ParticleSource WaterParticles;
        private readonly Blob _blob;
        private TileConfiguration _configuration;
        private uint _variant;
        private Vector3D _bottomOffset;
        private Timestep _lastCheck;
        private bool _done;
        private List<Vector2I> _positions;
        private int _currentTile;

        public SprinklerTileStateEntityLogic(Entity entity) {
            Entity = entity;
            _blob = entity.Blob.FetchBlob("logic");
            Entity.Physics.PriorityChunkRadius(0);
        }

        public override void PreUpdate(Timestep timestep, EntityUniverseFacade entityUniverseFacade) {
            //Set it up so it does not water on first place or first shown
            if (_lastCheck == Timestep.Null)
                _lastCheck = timestep;
        }

        public override void Update(Timestep timestep, EntityUniverseFacade universe) {
            Tile tile;
            //Don't let this logic do anything if tile does not exist. Might be accessed after removal of tile.
            if (!universe.ReadTile(Location, out tile))
                return;
            //If the tile/variant has changed, or it doesn't have a Sprinkler Component set this logic to be removed.
            if ((tile.Configuration != _configuration) || (_variant != tile.Variant()) || (SprinklerComp == null)) {
                _done = true;
                return;
            }

            //Reset the location of the tile entity. Mainly used for tile offsets.
            _bottomOffset = Location.ToVector3D();
            Vector3F tileOffset;
            if (universe.TileOffset(Location, out tileOffset))
                _bottomOffset.Y += tileOffset.Y;

            //Actual watering logic
            if (_lastCheck + SprinklerComp.CheckTime <= timestep) {
                _lastCheck = timestep + (int)Math.Floor(SprinklerComp.RandomCheckTime * GameContext.RandomSource.NextDouble(-1, 1));
                _currentTile++;
                if (_currentTile >= _positions.Count) {
                    _currentTile = 0;
                    GameContext.RandomSource.Shuffle(_positions);
                }
                var checkLocation = Location + SprinklerComp.Offset + new Vector3I(_positions[_currentTile].X, 0, _positions[_currentTile].Y);

                WaterPlant(universe, checkLocation);
            }
        }

        /// <summary>
        /// Function set up to water the tile, and play an effect.
        /// </summary>
        /// <param name="facade">Universe this entity is located in</param>
        /// <param name="tileLocation">The world location to water</param>
        private void WaterPlant(EntityUniverseFacade facade, Vector3I tileLocation) {
            var tileInteractionState = facade.FetchTileStateEntityLogic(tileLocation);
            if (tileInteractionState == null || tileInteractionState.GetType() != typeof(DirtTileStateEntityLogic))
                return;
            GameContext.FarmingDatabase.EntityWaterCanAction(Entity, tileLocation, facade);
            if (!SprinklerComp.WateredPlotEffect.IsNullOrEmpty())
                tileInteractionState.Entity.Effects.Trigger(new EffectTrigger(SprinklerComp.WateredPlotEffect));
        }

        public override void PostUpdate(Timestep timestep, EntityUniverseFacade universe) {
            //If this has been set. Remove this entity.
            if (_done)
                universe.RemoveEntity(Entity.Id);
        }

        /// <summary>
        /// Set temporarily saved data
        /// </summary>
        public override void Store() {
            _blob.FetchBlob("location").SetVector3I(Location);
            _blob.SetLong("variant", _variant);
            _blob.FetchBlob("bottomOffset").SetVector3D(_bottomOffset);
            _blob.SetString("tile", _configuration.Code);
            _blob.SetBool("done", _done);
        }

        /// <summary>
        /// Restore the entity from temporarily saved data
        /// </summary>
        public override void Restore() {
            Location = _blob.FetchBlob("location").GetVector3I();
            _variant = (uint)_blob.GetLong("variant");
            _bottomOffset = _blob.GetBlob("bottomOffset").GetVector3D();
            _done = _blob.GetBool("done");
            _configuration = GameContext.TileDatabase.GetTileConfiguration(_blob.GetString("tile"));
            SprinklerComp = _configuration.Components.GetOrDefault<SprinklerComponentBuilder.SprinklerComponent>();
            WaterParticles = new ParticleSource(GameContext.ParticleDatabase.GetParticle(SprinklerComp.SprinklerEffect).ParticleData);
            _lastCheck = Timestep.Null;
        }

        /// <summary>
        /// Called whenever the tile entity is created. Sets up all needed values
        /// </summary>
        /// <param name="arguments">The xml data being passed into this entity</param>
        /// <param name="entityUniverseFacade">The universe this entity is located in</param>
        public override void Construct(Blob arguments, EntityUniverseFacade entityUniverseFacade) {
            _configuration = GameContext.TileDatabase.GetTileConfiguration(arguments.GetString("tile"));
            SprinklerComp = _configuration.Components.GetOrDefault<SprinklerComponentBuilder.SprinklerComponent>();
            Location = arguments.FetchBlob("location").GetVector3I();
            _variant = (uint)arguments.GetLong("variant");
            if (SprinklerComp.IsCurved)
                FillListWithPointsInEllipse();
            else
                FillListWithPointsInRectangle();
            WaterParticles = new ParticleSource(GameContext.ParticleDatabase.GetParticle(SprinklerComp.SprinklerEffect).ParticleData);
            _lastCheck = Timestep.Null;
            _currentTile = _positions.Count;
            Entity.Physics.Construct(arguments.FetchBlob("position").GetVector3D(), Vector3D.Zero);
            Entity.Physics.MakePhysicsless();
        }


        /// <summary>
        /// Fills _positions with all points in the ellipse specified by SprinklerComp.Distance
        /// Point (i,j) is in list if it satisfies i^2*Distance.Y^2 + j^2*Distance.X^2 &lt;= Distance.Y^2 * Distance.X^2
        /// </summary>
        private void FillListWithPointsInEllipse() {
            _positions = new List<Vector2I>();
            var x = (_variant / 1024) % 2 == 1 ? SprinklerComp.Distance.X : SprinklerComp.Distance.Y;
            var y = (_variant / 1024) % 2 == 1 ? SprinklerComp.Distance.Y : SprinklerComp.Distance.X;
            for (int i = -x; i <= x; i++) {
                for (int j = -y; j <= y; j++) {
                    var ellipseX = Math.Pow(i, 2) * Math.Pow(y, 2);
                    var ellipseY = Math.Pow(j, 2) * Math.Pow(x, 2);
                    if (ellipseX + ellipseY <= Math.Pow(x, 2) * Math.Pow(y, 2)) {
                        var vec = new Vector2I(i, j);
                        _positions.Add(vec);
                    }
                }
            }
        }

        /// <summary>
        /// Fills _positions with all points in the rectangle specified by SprinklerComp.Distance
        /// </summary>
        private void FillListWithPointsInRectangle() {
            _positions = new List<Vector2I>();
            var x = (_variant / 1024) % 2 == 1 ? SprinklerComp.Distance.X : SprinklerComp.Distance.Y;
            var y = (_variant / 1024) % 2 == 1 ? SprinklerComp.Distance.Y : SprinklerComp.Distance.X;
            for (int i = -x; i <= x; i++) {
                for (int j = -y; j <= y; j++) {
                    var vec = new Vector2I(i, j);
                    _positions.Add(vec);
                }
            }
        }

        public override void Bind() { }

        public override void Interact(Entity entity, EntityUniverseFacade facade, ControlState main, ControlState alt) { }

        public override bool CheckChangingActiveItem() {
            return false;
        }

        public override bool IsPersistent() {
            return true;
        }

        public override bool IsAtLastSavedPosition() {
            return true;
        }

        public override ChunkKey GetLastSavedPosition() {
            return new ChunkKey(Entity.Physics.Position);
        }

        /// <summary>
        /// Set the saved data
        /// </summary>
        /// <param name="data"></param>
        public override void StorePersistenceData(Blob data) {
            var constructData = data.FetchBlob("constructData");
            constructData.SetString("tile", _configuration.Code);
            constructData.FetchBlob("location").SetVector3I(Location);
            constructData.SetLong("variant", _variant);
            constructData.FetchBlob("position").SetVector3D(Entity.Physics.Position);
            data.SetBool("done", _done);
            data.FetchBlob("bottomOffset").SetVector3D(_bottomOffset);
        }

        /// <summary>
        /// Restor from saved data
        /// </summary>
        /// <param name="data"></param>
        public override void RestoreFromPersistedData(Blob data, EntityUniverseFacade facade) {
            Entity.Construct(data.GetBlob("constructData"), facade);
            _done = data.GetBool("done");
            _bottomOffset = data.GetBlob("bottomOffset").GetVector3D();
            Store();
        }

        public Vector3D GetBottomOffset() {
            return _bottomOffset;
        }

        public override bool IsLingering() {
            return _done;
        }

        public override void KeepAlive() { }

        public override void BeingLookedAt(Entity entity) { }

        public override bool IsBeingLookedAt() {
            return false;
        }

        public override bool Interactable() {
            return false;
        }
    }
}
