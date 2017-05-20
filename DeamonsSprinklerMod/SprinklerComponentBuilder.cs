using Plukit.Base;
using Staxel.Core;

namespace DeamonsSprinklerMod {
    sealed class SprinklerComponentBuilder : IComponentBuilder {

        public string Kind() {
            return "sprinkler";
        }

        public object Instance(Blob config) {
            return new SprinklerComponent(config);
        }

        public sealed class SprinklerComponent {
            public Vector2I Distance { get; private set; }
            public Vector3I Offset { get; private set; }
            public int CheckTime { get; private set; }
            public int RandomCheckTime { get; private set; }
            public string SprinklerEffect { get; private set; }
            public string WateredPlotEffect { get; private set; }
            public bool IsCurved { get; private set; }

            public SprinklerComponent(Blob config) {
                Distance = config.Contains("distance") ? config.GetBlob("distance").GetVector2I() : new Vector2I(2, 2);
                Offset = config.Contains("offset") ? config.GetBlob("offset").GetVector3I() : new Vector3I(0, -1, 0);
                CheckTime = (int)(config.GetDouble("checkTime", 5) * 1000000);
                RandomCheckTime = (int)(config.GetDouble("randomCheckTime", 2.5) * 1000000);
                SprinklerEffect = config.GetString("sprinklerEffect", "");
                WateredPlotEffect = config.GetString("wateredPlotEffect", "");
                IsCurved = config.GetBool("isCurved", true);
            }
        }
    }
}
