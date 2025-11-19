using System;
using Core.Systems.UI;
using Core.Systems.UI.Core.UIManager;
using Core.Systems.UI.Core.UIRoot;
using Core.Systems.UI.Factory;
using Core.Systems.UI.Registry;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityUtils.Singleton;

public sealed class UIBootstrap : MonoBehaviour
{
    [Header("Global UIRoot")]
    [SerializeField] private UIRoot globalRootPrefab;

    [Header("Registry")]
    [SerializeField] private UIWindowRegistrySO windowRegistry;

    public IUIController Controller { get; private set; }

    private void Awake()
    {
        if (globalRootPrefab != null)
        {
            // 비활성 상태로 먼저 만들고 설정 후 활성화
            var globalRoot = Instantiate(globalRootPrefab);
            globalRoot.SetRole(UIRootRole.Global);
            DontDestroyOnLoad(globalRoot.gameObject);
        }
        else
        {
            Debug.LogWarning("[UIEntry] Global UIRoot prefab is not assigned.");
        }

        // 2) Controller 생성
        var controller = new UIController(
            UiServices.UIRootProvider,
            UIFactory.CreateInstance(),
            windowRegistry
        );

        Controller = controller;
        UiServices.UIController = controller;
    }

    private void Start()
    {
       SceneManager.LoadScene("HomeScene");
    }
}