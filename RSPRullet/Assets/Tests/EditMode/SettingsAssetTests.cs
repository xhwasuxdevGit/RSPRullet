using NUnit.Framework;
using UnityEditor;

// Assets/SO의 설정 에셋을 실제로 읽어, 기획서 값(코어 규칙 상세 기획서 7-1-1장, 게임 컨셉 기획서 2·9장, 딜러 AI 세부 기획서 7-1-1장)과 같은지 확인한다
public class SettingsAssetTests
{
    private const string Folder = "Assets/SO";

    [TestCase("Round1Settings", 1, 3, 0, 0, 0)]
    [TestCase("Round2Settings", 2, 4, 4, 1, 1)]
    [TestCase("Round3Settings", 3, 6, 6, 2, 1)]
    public void RoundSettings_MatchPlannedValues(string assetName, int roundNumber, int startHealth, int itemCapacity, int itemsPerShuffle, int startMagnifierCount)
    {
        var settings = AssetDatabase.LoadAssetAtPath<RoundSettings>($"{Folder}/{assetName}.asset");

        Assert.IsNotNull(settings);
        Assert.AreEqual(roundNumber, settings.RoundNumber);
        Assert.AreEqual(startHealth, settings.StartHealth);
        Assert.AreEqual(itemCapacity, settings.ItemCapacity);
        Assert.AreEqual(itemsPerShuffle, settings.ItemsPerShuffle);
        Assert.AreEqual(startMagnifierCount, settings.StartMagnifierCount);
    }

    [Test]
    public void RuleSettings_MatchPlannedValues()
    {
        var settings = AssetDatabase.LoadAssetAtPath<RuleSettings>($"{Folder}/RuleSettings.asset");

        Assert.IsNotNull(settings);
        Assert.AreEqual(10, settings.MaxHealth);
        Assert.AreEqual(1, settings.MatchDamage);
        Assert.AreEqual(2, settings.CoffeeMultiplier);
        Assert.AreEqual(1, settings.AttackFullSetDamage);
        Assert.AreEqual(1, settings.HealFullSetAmount);
        Assert.AreEqual(3, settings.TripleDamage);
    }

    // UI·UX 화면 흐름 세부 기획서 3-2장 판-07 승패 판정 표시 시작값, 연출·아트·사운드 세부 기획서 7-3장 딜러 고민 중 범위
    [Test]
    public void DisplayTimeSettings_MatchPlannedValues()
    {
        var settings = AssetDatabase.LoadAssetAtPath<DisplayTimeSettings>($"{Folder}/DisplayTimeSettings.asset");

        Assert.IsNotNull(settings);
        Assert.AreEqual(1.6f, settings.MatchResultSeconds, 0.0001f);
        Assert.AreEqual(0.8f, settings.DealerThinkingMinSeconds, 0.0001f);
        Assert.AreEqual(1.5f, settings.DealerThinkingMaxSeconds, 0.0001f);
    }

    // 딜러 AI 세부 기획서 7-1-1장 기본값. 시계 사용 확률은 규칙 설정 에셋에서 옮겨 온 값(0.85)을 유지한다
    [Test]
    public void DealerSettings_MatchPlannedValues()
    {
        var settings = AssetDatabase.LoadAssetAtPath<DealerSettings>($"{Folder}/DealerSettings.asset");

        Assert.IsNotNull(settings);
        Assert.AreEqual(1f, settings.StrengthCorrectionRate, 0.0001f);
        Assert.AreEqual(1f, settings.RuleComplianceRate, 0.0001f);
        Assert.AreEqual(0.85f, settings.ClockUseChance, 0.0001f);
        Assert.AreEqual(0.5f, settings.CoffeeUseThreshold, 0.0001f);
    }
}
