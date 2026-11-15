using NUnit.Framework;

// EditMode 테스트 환경이 컴파일·실행되는지만 확인하는 테스트
public class SmokeTest
{
    [Test]
    public void TestEnvironment_Runs()
    {
        Assert.Pass();
    }
}
