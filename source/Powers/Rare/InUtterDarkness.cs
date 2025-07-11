﻿using HutongGames.PlayMaker.Actions;
using KorzUtils.Helper;
using System.Collections.Generic;
using TrialOfCrusaders.Data;
using TrialOfCrusaders.Enums;
using TrialOfCrusaders.Manager;
using TrialOfCrusaders.UnityComponents.PowerElements;
using UnityEngine;

namespace TrialOfCrusaders.Powers.Rare;

internal class InUtterDarkness : Power
{
    public List<GameObject> ActiveSiblings { get; set; } = [];

    public static GameObject Sibling { get; set; }

    public override (float, float, float) BonusRates => new(50f, 50f, 0f);

    public override Rarity Tier => Rarity.Rare;

    public override DraftPool Pools => DraftPool.Instant | DraftPool.Risk | DraftPool.Upgrade | DraftPool.Debuff;

    public bool EffectGranted { get; set; }

    protected override void Enable()
    {
        if (!EffectGranted)
            HeroController.instance.MaxHealth();
        EffectGranted = true;
        On.HeroController.AddHealth += HeroController_AddHealth;
        On.HutongGames.PlayMaker.Actions.SetVector3Value.OnEnter += SetVector3Value_OnEnter;
        On.HutongGames.PlayMaker.Actions.IntCompare.OnEnter += IntCompare_OnEnter;
        On.SetHP.OnEnter += SetHP_OnEnter;
    }

    protected override void Disable()
    { 
        On.HeroController.AddHealth -= HeroController_AddHealth;
        On.HutongGames.PlayMaker.Actions.SetVector3Value.OnEnter -= SetVector3Value_OnEnter;
        On.HutongGames.PlayMaker.Actions.IntCompare.OnEnter -= IntCompare_OnEnter;
        On.SetHP.OnEnter -= SetHP_OnEnter;
        EffectGranted = false;
    }

    private void SetHP_OnEnter(On.SetHP.orig_OnEnter orig, SetHP self)
    {
        if (self.IsCorrectContext("Shade Control", null, "Friendly Idle"))
            self.hp = 99999;
        orig(self);
    }

    private void IntCompare_OnEnter(On.HutongGames.PlayMaker.Actions.IntCompare.orig_OnEnter orig, IntCompare self)
    {
        if (self.IsCorrectContext("Shade Control", "Void Shade", "Friendly?"))
            self.integer1.Value = 4; // Sets the shade to friendly.
        orig(self);
    }

    private void HeroController_AddHealth(On.HeroController.orig_AddHealth orig, HeroController self, int amount)
    {
        if (!TreasureManager.EnduranceHealthGrant)
            amount = 0;
        orig(self, amount);
    }

    private void SetVector3Value_OnEnter(On.HutongGames.PlayMaker.Actions.SetVector3Value.orig_OnEnter orig, SetVector3Value self)
    {
        if (self.IsCorrectContext("Spell Control", "Knight", "Focus Heal*"))
        {
            int amount = self.Fsm.Variables.FindFsmInt("Health Increase").Value;
            GameObject shade = Object.Instantiate(GameManager.instance.sm.hollowShadeObject);
            shade.SetActive(true);
            shade.name = "Void Shade";
            shade.transform.position = HeroController.instance.transform.position + new Vector3(0f, 2f, 0f);
            shade.GetComponent<BoxCollider2D>().enabled = false;
            GameObject voidZone = Object.Instantiate(VoidZone.Ring, shade.transform);
            voidZone.transform.SetParent(shade.transform);
            voidZone.transform.localPosition = new(0f, 0f);
            voidZone.transform.localScale = new(1f, 1f);
            voidZone.AddComponent<VoidZone>().LeftTime = amount * 3;
            voidZone.SetActive(true);
        }
        orig(self);
    }
}
