using System;
using AES.Tools;
using AES.Tools.View;


using AES.Tools.View;
using UnityEngine;

public abstract class PopupViewBase : ContextViewBase
{
    protected virtual void Awake()
    {
        base.Awake();
        
        OnHideCompleted.AddListener(NotifyClosed);
    }

    void NotifyClosed()
    {
        // 씬에 있는 PopupManager 싱글톤으로 전달
        PopupManager.Instance.OnPopupClosed(this);
    }
}


