using System;
using AES.Tools;
using AES.Tools.Sample;
using AES.Tools.View;


public class TestPopupView : PopupView<PlayerViewModel>
{
    readonly DataContext _ctx = new DataContext();


    protected override void OnBind(PlayerViewModel vm)
    {
    }
}