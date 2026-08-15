using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DeadByDawn
{
    public class DeadByDawnSystem : ModSystem
    {
        private ICoreServerAPI sapi;

        public override void Start(ICoreAPI api)
        {
            api.RegisterEntityBehaviorClass("sunburn", typeof(EntityBehaviorSunburn));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            api.Event.OnTrySpawnEntity += OnTrySpawnEntity;
        }

        private bool OnTrySpawnEntity(IBlockAccessor ba, ref EntityProperties properties, Vec3d spawnPosition, long herdId)
        {
            JsonObject sunburnCfg = FindSunburnConfig(properties);
            if (sunburnCfg == null) return true;

            float endHour = sunburnCfg["endHour"].AsFloat(6f);
            float duskHour = sunburnCfg["duskHour"].AsFloat(18f);
            // Variant suffixes (e.g. "drifter-normal", "shiver-surface") that carry the buffed surface horde.
            string surfaceVariants = sunburnCfg["surfaceVariants"].AsString("normal,surface");
            // How far below the worldgen surface a spawn must be before it counts as a cave.
            int surfaceTolerance = sunburnCfg["surfaceTolerance"].AsInt(1);

            // Decide "open sky" vs "cave" by whether worldgen terrain roofs this spot, not by a fixed depth
            // line. GetTerrainMapheightAt is the ground surface ignoring leaves/placed blocks, so forest
            // floors still read as surface while a cavern chamber - even one near sea level - reads as cave.
            int terrainY = ba.GetTerrainMapheightAt(new BlockPos((int)spawnPosition.X, (int)spawnPosition.Y, (int)spawnPosition.Z));
            bool underground = spawnPosition.Y < terrainY - surfaceTolerance;

            if (underground)
            {
                // Leave vanilla cave spawning alone, but never let the buffed surface-horde variants spawn
                // here - that leak is what was flooding caverns at dusk and blocking access.
                return !IsSurfaceVariant(properties, surfaceVariants);
            }

            // Open sky: peaceful by day, horde by night.
            IGameCalendar cal = sapi.World.Calendar;
            double clockHour = cal.HourOfDay / cal.HoursPerDay * 24.0;

            if (clockHour >= endHour && clockHour < duskHour) return false;

            return true;
        }

        private static bool IsSurfaceVariant(EntityProperties properties, string surfaceVariants)
        {
            string path = properties?.Code?.Path;
            if (path == null) return false;

            foreach (string entry in surfaceVariants.Split(','))
            {
                string variant = entry.Trim();
                if (variant.Length > 0 && path.EndsWith("-" + variant, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private JsonObject FindSunburnConfig(EntityProperties properties)
        {
            JsonObject[] behaviors = properties?.Server?.BehaviorsAsJsonObj;
            if (behaviors == null) return null;
            for (int i = 0; i < behaviors.Length; i++)
            {
                if (behaviors[i]["code"].AsString() == "sunburn") return behaviors[i];
            }
            return null;
        }
    }
}
