using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace DeadByDawn
{
    public class EntityBehaviorSunburn : EntityBehavior
    {
        private float startHour = 5f;
        private float endHour = 6f;
        private float duskHour = 18f;
        private bool ignoreDeepDayLight = true;
        private float deepDayLightOffset = 2f;
        private float tickIntervalSec = 1f;
        private EnumDespawnReason despawnReason = EnumDespawnReason.Expire;

        private float accumSec;
        private float personalDawnHour;

        public EntityBehaviorSunburn(Entity entity) : base(entity) { }

        public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
        {
            startHour = typeAttributes["startHour"].AsFloat(5f);
            endHour = typeAttributes["endHour"].AsFloat(6f);
            duskHour = typeAttributes["duskHour"].AsFloat(18f);
            ignoreDeepDayLight = typeAttributes["ignoreDeepDayLight"].AsBool(true);
            deepDayLightOffset = typeAttributes["deepDayLightOffset"].AsFloat(2f);
            tickIntervalSec = Math.Max(0.1f, typeAttributes["tickIntervalSec"].AsFloat(1f));

            string reasonStr = typeAttributes["despawnReason"].AsString("Expire");
            if (!Enum.TryParse(reasonStr, true, out despawnReason))
            {
                despawnReason = EnumDespawnReason.Expire;
            }

            long id = Math.Abs(entity.EntityId);
            float spread = Math.Max(0f, endHour - startHour);
            personalDawnHour = startHour + (id % 997) / 997f * spread;

            accumSec = (id * 0.137f) % tickIntervalSec;
        }

        public override void OnGameTick(float deltaTime)
        {
            if (entity.World.Side != EnumAppSide.Server || !entity.Alive) return;

            accumSec += deltaTime;
            if (accumSec < tickIntervalSec) return;
            accumSec = 0f;

            if (ignoreDeepDayLight && entity.Pos.Y < entity.World.SeaLevel - deepDayLightOffset) return;

            IGameCalendar cal = entity.World.Calendar;
            double clockHour = cal.HourOfDay / cal.HoursPerDay * 24.0;

            if (clockHour >= duskHour || clockHour < startHour) return;

            if (clockHour < endHour)
            {
                if (clockHour >= personalDawnHour) entity.Die(despawnReason);
                return;
            }

            entity.Die(despawnReason);
        }

        public override string PropertyName() => "sunburn";
    }
}
