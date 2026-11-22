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

    // 아이템 세부 기획서 7-1-1장 지급 가중치 기본값 (커피 40 / 돋보기 40 / 시계 20)
    [Test]
    public void ItemSettings_MatchPlannedValues()
    {
        var settings = AssetDatabase.LoadAssetAtPath<ItemSettings>($"{Folder}/ItemSettings.asset");

        Assert.IsNotNull(settings);
        Assert.AreEqual(40, settings.GetWeight(ItemType.Coffee));
        Assert.AreEqual(40, settings.GetWeight(ItemType.Magnifier));
        Assert.AreEqual(20, settings.GetWeight(ItemType.Clock));
    }

    // UI·UX 화면 흐름 세부 기획서 3-2장 판-07 승패 판정 표시 시작값.
    // 딜러 고민 중 범위는 연출·아트·사운드 세부 기획서 7-3장 시작 범위(0.8~1.5초)에서 플레이 테스트로 정한 값이다 (RSP-97)
    [Test]
    public void DisplayTimeSettings_MatchPlannedValues()
    {
        var settings = AssetDatabase.LoadAssetAtPath<DisplayTimeSettings>($"{Folder}/DisplayTimeSettings.asset");

        Assert.IsNotNull(settings);
        Assert.AreEqual(1.6f, settings.MatchResultSeconds, 0.0001f);
        Assert.AreEqual(1.5f, settings.CardPoolSeconds, 0.0001f);
        Assert.AreEqual(1.0f, settings.DecideFirstSeconds, 0.0001f);
        Assert.AreEqual(1.5f, settings.DealerThinkingMinSeconds, 0.0001f);
        Assert.AreEqual(2.0f, settings.DealerThinkingMaxSeconds, 0.0001f);
    }

    // 연출·아트·사운드 세부 기획서 7-1장 카드 연출 시간 시작값 (들어 올리기 기울기는 아래 주석 참고)
    [Test]
    public void DisplayTimeSettings_CardAnimationMatchPlannedValues()
    {
        var settings = AssetDatabase.LoadAssetAtPath<DisplayTimeSettings>($"{Folder}/DisplayTimeSettings.asset");

        Assert.IsNotNull(settings);
        Assert.AreEqual(0.5f, settings.CardMoveSeconds, 0.0001f);
        Assert.AreEqual(0.2f, settings.CardMoveDelaySeconds, 0.0001f);
        Assert.AreEqual(0.45f, settings.CardFlipSeconds, 0.0001f);
        Assert.AreEqual(0.6f, settings.CardSwapSeconds, 0.0001f);
        Assert.AreEqual(0.7f, settings.DeckShuffleSeconds, 0.0001f);
        Assert.AreEqual(0.6f, settings.CardLiftSeconds, 0.0001f);
        // 들어 올리기 기울기는 기획서 시작값 60°에서 Play 검증(RSP-63)으로 낮춘 값이다
        Assert.AreEqual(30f, settings.CardLiftTiltDegrees, 0.0001f);
    }

    // 진행 안내 배너 (RSP-86). 배너 종류마다 문구가 있어야 빈 배너가 뜨지 않는다
    [Test]
    public void BannerSettings_HasTextForEveryBannerType()
    {
        var settings = AssetDatabase.LoadAssetAtPath<BannerSettings>($"{Folder}/BannerSettings.asset");

        Assert.IsNotNull(settings);
        Assert.IsNotNull(settings.TransitionSprite);
        Assert.IsNotNull(settings.TintedPanelSprite);
        foreach (BannerType type in System.Enum.GetValues(typeof(BannerType)))
        {
            BannerEntry entry = settings.GetEntry(type);
            Assert.IsNotNull(entry, $"{type}");
            Assert.IsFalse(string.IsNullOrEmpty(entry.Text), $"{type}");
        }
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
