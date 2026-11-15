// 카드 한 장. 손 모양과 "누가 이 카드를 아는가" 정보를 함께 가진다.
// 아는 사람 정보가 카드 자체에 있으므로, 카드를 다른 자리로 옮기면 정보도 함께 옮겨진다.
public class Card
{
    public HandShape Shape { get; }
    public bool KnownByPlayer { get; set; }
    public bool KnownByDealer { get; set; }

    public Card(HandShape shape)
    {
        Shape = shape;
    }
}
