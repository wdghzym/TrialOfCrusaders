﻿using KorzUtils.Helper;
using TrialOfCrusaders.Data;
using TrialOfCrusaders.Enums;
using TrialOfCrusaders.Powers.Uncommon;
using UnityEngine;

namespace TrialOfCrusaders.Powers.Rare;

internal class ShadeSoul : Power
{
    public override (float, float, float) BonusRates => new(0f, 100f, 0f);

    public override Rarity Tier => Rarity.Rare;

    public override DraftPool Pools => DraftPool.Spirit | DraftPool.Ability | DraftPool.Upgrade;

    public override bool CanAppear => HasPower<VengefulSpirit>();

    public override Sprite Sprite => SpriteHelper.CreateSprite<TrialOfCrusaders>("Sprites.Abilities." + GetType().Name);

    protected override void Enable()
    {
        PDHelper.FireballLevel = 2;
        PDHelper.HasSpell = true;
    }

    protected override void Disable()
    {
        PDHelper.FireballLevel = 0;
        PDHelper.HasSpell = false;
    }
}
