using Plukit.Base;
using Staxel;
using Staxel.Client;
using Staxel.Draw;
using Staxel.Effects;
using Staxel.Logic;
using Staxel.Rendering;

namespace DeamonsSprinklerMod {
    sealed class SprinklerTileStateEntityPainter : EntityPainter {
        EffectRenderer _effectRenderer = Allocator.EffectRenderer.Allocate();
        protected override void Dispose(bool disposing) {
            if (disposing) {
                if (_effectRenderer != null) {
                    _effectRenderer.Dispose();
                    Allocator.EffectRenderer.Release(ref _effectRenderer);
                }
            }
        }

        public override void RenderUpdate(Timestep timestep, Entity entity, AvatarController avatarController, EntityUniverseFacade facade) {
            _effectRenderer.RenderUpdate(timestep, entity.Effects, entity, this, facade, entity.Physics.Position);
            var logic = entity.Logic as SprinklerTileStateEntityLogic;
            if (logic == null)
                return;
            logic.WaterParticles.Offset = logic.GetBottomOffset() + new Vector3D(0.5, 0.5, 0.5);
        }

        public override void ClientUpdate(Timestep timestep, Entity entity, AvatarController avatarController, EntityUniverseFacade facade) { }
        public override void ClientPostUpdate(Timestep timestep, Entity entity, AvatarController avatarController, EntityUniverseFacade facade) { }
        public override void BeforeRender(DeviceContext graphics, Vector3D renderOrigin, Entity entity, AvatarController avatarController, Timestep renderTimestep) { }

        public override void Render(DeviceContext graphics, Matrix4F matrix, Vector3D renderOrigin, Entity entity, AvatarController avatarController, Timestep renderTimestep, RenderMode renderMode) {
            _effectRenderer.Render(entity, this, renderTimestep, graphics, matrix, renderOrigin, renderMode);
            if (renderMode != RenderMode.Normal)
                return;
            var logic = entity.Logic as SprinklerTileStateEntityLogic;
            if (logic == null)
                return;
            logic.WaterParticles.Render(renderTimestep, renderMode);
        }

        public override bool AssociatedWith(Entity entity) {
            return entity.Logic is SprinklerTileStateEntityLogic;
        }

        public override void StartEmote(Entity entity, Timestep renderTimestep, EmoteConfiguration emote) { }
    }
}
