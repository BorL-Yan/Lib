namespace Lib.UI
{
    /// <summary>
    /// Обычная кнопка на базе <see cref="UIMobileButton"/> без своей логики.
    /// Что она делает, решает скрипт, который подписан на OnClick (например PauseMenu).
    /// </summary>
    public class MobileButton : UIMobileButton
    {
        protected override void Click()
        {
        }
    }
}
