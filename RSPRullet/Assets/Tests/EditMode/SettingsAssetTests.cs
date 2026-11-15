using NUnit.Framework;
using UnityEditor;

// Assets/SO의 설정 에셋을 실제로 읽어, 기획서 값(코어 규칙 상세 기획서 7-1-1장, 게임 컨셉 기획서 2·9장)과 같은지 확인한다
public class SettingsAssetTests
{
    private const string Folder = "Assets/SO";

    [TestCase("Round1Settings", 3, 0, 0, 0)]
    [TestCase("Round2Settings", 4, 4, 1, 1)]
    [TestCase("Round3Settings", 6, 6, 2, 1)]
    public void RoundSettings_MatchPlannedValues(string assetName, int startHealth, int itemCapacity, int itemsPerShuffle, int startMagnifierCount)
    {
        var settings = AssetDatabase.LoadAssetAtPath<RoundSettings>($"{Folder}/{assetName}.asset");

        Assert.IsNotNull(settings);
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
        Assert.AreEqual(0.85f, settings.DealerClockChance, 0.0001f);
    }
}
