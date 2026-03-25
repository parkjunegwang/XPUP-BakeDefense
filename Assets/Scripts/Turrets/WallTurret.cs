using UnityEngine;

namespace Underdark
{
    public class WallTurret : TurretBase
    {
        public override bool IsPassable => false;

        protected override void Awake()
        {
            // turretType은 PlaceSelectedTurret에서 주입됨 (Wall, Wall2x1, Wall1x2, Wall2x2 공유)
            if (statData == null) { damage = 0f; range = 0f; fireRate = 0f; hp = 200f; }
            base.Awake();

            if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
            if (bodyRenderer != null) bodyRenderer.sortingOrder = SLayer.Turret;
        }

        protected override void OnTick() { }
        protected override void Update() { }
    }
}
