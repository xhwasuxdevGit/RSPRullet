using UnityEngine;

// 딜러의 아이템 판단과 교환 판단 (딜러 AI 세부 기획서 4장, 7-2장 D2·D3).
// 딜러 AI(DealerAI)가 만들어 가지고, 바깥에서는 DealerAI를 거쳐서만 부른다 (딜러 AI 세부 기획서 2장 판단은 한 곳에).
// 카드를 세는 방법과 HP 변화 계산은 분배 판단과 같은 규칙(DealerEstimateRules)을 쓴다
public class DealerItemJudge
{
    // 아이템 검토 순서 (딜러 AI 세부 기획서 7-1-2장)
    private static readonly ItemType[] ItemReviewOrder = { ItemType.Magnifier, ItemType.Clock, ItemType.Coffee };

    private readonly RuleSettings _rules;
    private readonly DealerSettings _dealerSettings;
    private readonly System.Random _random;

    // 시계 사용 확률을 뽑는 난수는 딜러 AI와 같은 것을 받아, 같은 시드면 진행 전체가 같게 나온다
    public DealerItemJudge(RuleSettings rules, DealerSettings dealerSettings, System.Random random)
    {
        _rules = rules;
        _dealerSettings = dealerSettings;
        _random = random;
    }

    // 다음에 쓸 아이템을 돌려주고, 더 쓰지 않으면 null(아이템 끝내기)을 돌려준다.
    // 검토 순서대로 한 번씩만 본다: 이번 판에 딜러가 마지막으로 쓴 아이템의 다음 순서부터 본다.
    // 그래서 시계 사용 확률은 한 단계에 한 번만 뽑고, 뒤 순서 아이템을 쓴 뒤 앞 순서 아이템을 다시 보지 않는다.
    // items는 보유 아이템과 사용 기록(공개 정보)을 읽는 데만 쓰고 바꾸지 않는다
    public ItemType? ChooseItem(DealerKnownInfo knownInfo, ItemState items, Side turnPlayer)
    {
        for (int i = GetReviewStartIndex(items); i < ItemReviewOrder.Length; i++)
        {
            ItemType item = ItemReviewOrder[i];
            if (items.CanUseItem(Side.Dealer, item, turnPlayer) && ShouldUseItem(item, knownInfo, items))
            {
                return item;
            }
        }

        return null;
    }

    // 돋보기로 상대 카드를 안 뒤에 부르며, 교환하면 true를 돌려준다.
    // 교환 전과 교환 후를 비교해 교환 후가 더 좋을 때만 교환한다. 상대 카드를 모르면 비교할 수 없어 교환하지 않는다
    public bool ChooseExchange(DealerKnownInfo knownInfo, ItemState items)
    {
        HandShape? playerShape = GetMatchShape(knownInfo, Side.Player);
        if (playerShape == null) { return false; }

        int changeBefore = SumExchangeChange(knownInfo, items, playerShape.Value, exchange: false);
        int changeAfter = SumExchangeChange(knownInfo, items, playerShape.Value, exchange: true);
        bool exchange = changeAfter > changeBefore;
        Debug.Log($"[DealerAI] 교환 판단: 교환 전 {changeBefore}, 교환 후 {changeAfter} → {(exchange ? "교환" : "그대로")}");
        return exchange;
    }

    // 검토를 시작할 순서: 이번 판에 딜러가 마지막으로 쓴 아이템의 다음 순서. 아직 쓴 아이템이 없으면 처음부터
    private static int GetReviewStartIndex(ItemState items)
    {
        int startIndex = 0;
        foreach ((Side User, ItemType Item, bool Exchanged) use in items.ItemUses)
        {
            if (use.User == Side.Dealer)
            {
                startIndex = System.Array.IndexOf(ItemReviewOrder, use.Item) + 1;
            }
        }

        return startIndex;
    }

    // 아이템별 사용 규칙 (딜러 AI 세부 기획서 4장 표)
    private bool ShouldUseItem(ItemType item, DealerKnownInfo knownInfo, ItemState items)
    {
        switch (item)
        {
            case ItemType.Magnifier:
                // 상대 카드를 모르거나, 알고 교환하면 이득일 때 쓴다
                return GetMatchShape(knownInfo, Side.Player) == null || ChooseExchange(knownInfo, items);
            case ItemType.Clock:
                bool useClock = _random.NextDouble() < _dealerSettings.ClockUseChance;
                Debug.Log($"[DealerAI] 시계: 사용 확률 {_dealerSettings.ClockUseChance}, {(useClock ? "사용" : "사용 안 함")}");
                return useClock;
            default: // 커피
                return ShouldUseCoffee(knownInfo);
        }
    }

    // 커피: 이번 판을 이길 확률이 커피 사용 기준 이상이면 쓴다 (딜러 AI 세부 기획서 4장 표 커피).
    // 모르는 자리에 후보 카드를 한 장씩 넣어 이기는 경우를 센다.
    // 양쪽 다 모르면 딜러 자리에 넣은 카드는 플레이어 자리 후보에서 한 장 뺀다 (같은 카드가 두 자리에 동시에 있을 수 없다)
    private bool ShouldUseCoffee(DealerKnownInfo knownInfo)
    {
        HandShape? dealerShape = GetMatchShape(knownInfo, Side.Dealer);
        HandShape? playerShape = GetMatchShape(knownInfo, Side.Player);
        bool isBothUnknown = dealerShape == null && playerShape == null;
        int winCases = 0;
        int totalCases = 0;

        foreach (HandShape dealerCandidate in System.Enum.GetValues(typeof(HandShape)))
        {
            int dealerCount = GetCandidateCount(knownInfo, dealerShape, dealerCandidate);
            foreach (HandShape playerCandidate in System.Enum.GetValues(typeof(HandShape)))
            {
                int playerCount = GetCandidateCount(knownInfo, playerShape, playerCandidate);
                if (isBothUnknown && dealerCandidate == playerCandidate) { playerCount--; }

                int cases = dealerCount * playerCount;
                totalCases += cases;
                if (HandShapeRules.Beats(dealerCandidate, playerCandidate)) { winCases += cases; }
            }
        }

        if (totalCases == 0) { return false; }

        float winRate = (float)winCases / totalCases;
        bool useCoffee = winRate >= _dealerSettings.CoffeeUseThreshold;
        Debug.Log($"[DealerAI] 커피: 이길 경우 {winCases}/{totalCases}, 기준 {_dealerSettings.CoffeeUseThreshold}, {(useCoffee ? "사용" : "사용 안 함")}");
        return useCoffee;
    }

    // 딜러 자리 카드 후보를 한 장씩 넣어 본 교환 전·후 값의 합. 교환하면 딜러는 상대 카드를, 상대는 딜러 카드 후보를 갖는다.
    // 1번째 판은 판 승패(이김 +1, 짐 −1), 2번째 판은 예상 HP 변화(판정 시 + 정산 시)를 더한다 (딜러 AI 세부 기획서 4장 표 돋보기 후 교환)
    private int SumExchangeChange(DealerKnownInfo knownInfo, ItemState items, HandShape playerShape, bool exchange)
    {
        HandShape? dealerShape = GetMatchShape(knownInfo, Side.Dealer);
        int sum = 0;

        foreach (HandShape dealerCandidate in System.Enum.GetValues(typeof(HandShape)))
        {
            HandShape dealerCard = exchange ? playerShape : dealerCandidate;
            HandShape playerCard = exchange ? dealerCandidate : playerShape;
            sum += GetExchangeValue(knownInfo, items, dealerCard, playerCard) * GetCandidateCount(knownInfo, dealerShape, dealerCandidate);
        }

        return sum;
    }

    private int GetExchangeValue(DealerKnownInfo knownInfo, ItemState items, HandShape dealerCard, HandShape playerCard)
    {
        MatchResult dealerResult = HandShapeRules.Compare(dealerCard, playerCard);
        if (knownInfo.MatchIndex != DealerEstimateRules.FirstMatchIndex)
        {
            return DealerEstimateRules.GetJudgmentValue(_rules, dealerResult, items)
                + DealerEstimateRules.GetSettlementChange(_rules, knownInfo, dealerCard, playerCard);
        }

        if (dealerResult == MatchResult.Win) { return 1; }
        if (dealerResult == MatchResult.Lose) { return -1; }
        return 0;
    }

    // 이번 판 카드 자리(조합 칸의 마지막 칸)의 손 모양. 딜러가 모르면 null
    private static HandShape? GetMatchShape(DealerKnownInfo knownInfo, Side side)
    {
        var slots = knownInfo.GetSlotShapes(side);
        return slots[slots.Count - 1];
    }

    // 이번 판 카드 자리에 candidate가 들어갈 수 있는 경우의 수. 딜러가 아는 자리면 그 카드 1장뿐이고,
    // 모르는 자리면 분배 판단과 같은 방법으로 센다 (1번째 판은 카드풀 구성, 2번째 판은 딜러가 모르는 카드. 딜러 AI 세부 기획서 3-1장)
    private static int GetCandidateCount(DealerKnownInfo knownInfo, HandShape? knownShape, HandShape candidate)
    {
        if (knownShape.HasValue) { return knownShape.Value == candidate ? 1 : 0; }

        return DealerEstimateRules.GetThisMatchCardCount(knownInfo, candidate);
    }
}
