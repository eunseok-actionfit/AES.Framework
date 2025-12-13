using UnityEditor;


namespace AES.Tools.Editor.Util
{
    /// <summary>
    /// 인스펙터 도움말 표시 기능을 메뉴에서 켜고 끄도록 관리한다.<br/>
    /// `EditorPrefs` 기반으로 상태를 저장한다.
    /// </summary>
    public static class MenuHelp
    {
        // 설정 값 캐싱 여부
        public static bool SettingCached = false;

        // 캐싱된 도움말 활성화 상태
        public static bool CachedHelpEnabled = false;


        [MenuItem("AES/Tools/Help/Enable Help in Inspectors", false, 0)]
        /// <summary>
        /// 인스펙터 도움말을 활성화한다.
        /// </summary>
        private static void EnableHelpInInspectors()
        {
            SetHelpEnabled(true);
        }

        [MenuItem("AES/Tools/Help/Enable Help in Inspectors", true)]
        /// <summary>
        /// 활성화 메뉴의 사용 가능 여부를 결정한다.
        /// </summary>
        private static bool EnableHelpInInspectorsValidation()
        {
            return !HelpEnabled;
        }


        [MenuItem("AES/Tools/Help/Disable Help in Inspectors", false, 1)]
        /// <summary>
        /// 인스펙터 도움말을 비활성화한다.
        /// </summary>
        private static void DisableHelpInInspectors()
        {
            SetHelpEnabled(false);
        }

        [MenuItem("AES/Tools/Help/Disable Help in Inspectors", true)]
        /// <summary>
        /// 비활성화 메뉴의 사용 가능 여부를 결정한다.
        /// </summary>
        private static bool DisableHelpInInspectorsValidation()
        {
            return HelpEnabled;
        }


        /// <summary>
        /// 저장된 도움말 활성화 여부를 반환한다.<br/>
        /// 캐시가 있으면 바로 반환한다.
        /// </summary>
        /// <returns>도움말 표시 상태</returns>
        public static bool HelpEnabled
        {
            get
            {
                if (SettingCached)
                {
                    return CachedHelpEnabled;
                }

                if (EditorPrefs.HasKey("ShowHelpInInspectors"))
                {
                    CachedHelpEnabled = EditorPrefs.GetBool("ShowHelpInInspectors");
                    SettingCached = true;
                    return CachedHelpEnabled;
                }

                EditorPrefs.SetBool("ShowHelpInInspectors", true);
                CachedHelpEnabled = true;
                SettingCached = true;
                return CachedHelpEnabled;
            }
        }


        /// <summary>
        /// 도움말 표시 상태를 저장하고 씬 뷰를 갱신한다.
        /// </summary>
        /// <param name="status">저장할 도움말 상태</param>
        private static void SetHelpEnabled(bool status)
        {
            EditorPrefs.SetBool("ShowHelpInInspectors", status);
            CachedHelpEnabled = status;
            SettingCached = true;
            SceneView.RepaintAll();
        }
    }
}
