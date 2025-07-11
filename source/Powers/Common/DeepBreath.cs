﻿using KorzUtils.Helper;
using TrialOfCrusaders.Controller;
using TrialOfCrusaders.Data;
using TrialOfCrusaders.Enums;
using UnityEngine;

namespace TrialOfCrusaders.Powers.Common;

internal class DeepBreath : Power
{
    public override (float, float, float) BonusRates => new(0f, 10f, 0f);

    public override StatScaling Scaling => StatScaling.Spirit;

    public override DraftPool Pools => DraftPool.Spirit | DraftPool.Burst;

    protected override void Enable() => UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;

    protected override void Disable() => UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;

    private void SceneManager_activeSceneChanged(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.Scene arg1)
    {
        float soulToRestore = 5 + CombatController.SpiritLevel;
        float missingHealth = PDHelper.MaxHealth / PDHelper.Health;
        soulToRestore *= missingHealth;
        HeroController.instance.AddMPCharge(Mathf.CeilToInt(soulToRestore));
    }
}
