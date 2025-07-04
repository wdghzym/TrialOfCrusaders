﻿using KorzUtils.Enums;
using KorzUtils.Helper;
using TrialOfCrusaders.Controller;
using TrialOfCrusaders.Data;
using TrialOfCrusaders.Enums;
using TrialOfCrusaders.Powers.Rare;
using UnityEngine;

namespace TrialOfCrusaders.Powers.Common;

internal class SharpShadow : Power
{
    public override bool CanAppear => PDHelper.HasShadowDash && !CombatController.HasPower<ShiningBound>(out _);

    public override (float, float, float) BonusRates => new(5f, 0f, 5f);

    public override DraftPool Pools => DraftPool.Charm | DraftPool.Combat | DraftPool.Upgrade;

    public override Sprite Sprite => SpriteHelper.CreateSprite<TrialOfCrusaders>("Sprites.Abilities." + GetType().Name);

    protected override void Enable() => CharmHelper.EnsureEquipCharm(CharmRef.SharpShadow);

    protected override void Disable() => CharmHelper.UnequipCharm(CharmRef.SharpShadow);
}
