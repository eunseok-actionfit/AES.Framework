using VContainer;


namespace AES.Tools.VContainer.Bootstrap.Framework
{
    // QuickAdd가 이 타입을 찾아 placeholder로 생성한다.
    [HideInFeatureMenu]
    public class MissingFeature : AppFeatureSO
    {
        public override bool IsEnabled(in FeatureContext ctx) => false; // placeholder는 실행 안 함
        public override void Install(IContainerBuilder builder, in FeatureContext ctx) { }
    }
}