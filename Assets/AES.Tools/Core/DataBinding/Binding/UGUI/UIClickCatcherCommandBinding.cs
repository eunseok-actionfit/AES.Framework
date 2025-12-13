using System;
using System.Globalization;
using UnityEngine;


namespace AES.Tools
{
    [RequireComponent(typeof(UIClickCatcher))]
    public sealed class UIClickCatcherCommandBinding : ContextBindingBase
    {
        [Header("Target")]
        [SerializeField] UIClickCatcher catcher;

        [Header("Parameter (optional)")]
        [SerializeField] bool useParameter;
        [SerializeField, ShowIf(nameof(useParameter))] ParameterType parameterType = ParameterType.None;
        [SerializeField, ShowIf(nameof(useParameter))] string stringParameter;
        [SerializeField] bool useInvariantCulture = true;

        ICommand _command;
        IBindingContext _ctx;
        bool _isRunning;

#if UNITY_EDITOR
        void Reset()
        {
            catcher = GetComponent<UIClickCatcher>();
        }
#endif

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            if (catcher == null)
                catcher = GetComponent<UIClickCatcher>();

            if (catcher == null)
            {
                LogBindingError("UIClickCatcherCommandBinding: UIClickCatcher 가 설정되지 않았습니다.");
                return;
            }

            _ctx = context;

            object value;
            try
            {
                value = context.GetValue(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"UIClickCatcherCommandBinding: Path '{path}' 조회 실패: {e.Message}", this);
                return;
            }

            if (value is ICommand cmd)
            {
                _command = cmd;
            }
            else
            {
                Debug.LogError(
                    $"UIClickCatcherCommandBinding: Path '{path}' 는 ICommand 타입이 아닙니다 ({value?.GetType().Name}).",
                    this);
                return;
            }

            catcher.OnClickedEvent.AddListener(OnClicked);
        }

        protected override void OnContextUnavailable()
        {
            if (catcher != null)
                catcher.OnClickedEvent.RemoveListener(OnClicked);

            _command = null;
            _ctx = null;
        }

        async void OnClicked()
        {
            if (_isRunning || _command == null)
                return;

            var param = GetParameterObject();
            if (!_command.CanExecute(param))
                return;

            _isRunning = true;

            try
            {
                if (_command is IAsyncCommand asyncCmd)
                    await asyncCmd.ExecuteAsync(param);
                else
                    _command.Execute(param);
            }
            finally
            {
                _isRunning = false;
            }
        }

        object GetParameterObject()
        {
            if (!useParameter)
                return null;

            if (string.IsNullOrEmpty(stringParameter))
                return null;

            var culture = useInvariantCulture
                ? CultureInfo.InvariantCulture
                : CultureInfo.CurrentCulture;

            try
            {
                switch (parameterType)
                {
                    case ParameterType.String:
                        return stringParameter;

                    case ParameterType.Int:
                        if (int.TryParse(stringParameter, NumberStyles.Integer, culture, out var i))
                            return i;
                        Debug.LogWarning($"UIClickCatcherCommandBinding: int 변환 실패 '{stringParameter}'", this);
                        return null;

                    case ParameterType.Float:
                        if (float.TryParse(stringParameter,
                                NumberStyles.Float | NumberStyles.AllowThousands,
                                culture, out var f))
                            return f;
                        Debug.LogWarning($"UIClickCatcherCommandBinding: float 변환 실패 '{stringParameter}'", this);
                        return null;

                    case ParameterType.Bool:
                        if (bool.TryParse(stringParameter, out var b))
                            return b;
                        if (stringParameter == "0") return false;
                        if (stringParameter == "1") return true;
                        Debug.LogWarning($"UIClickCatcherCommandBinding: bool 변환 실패 '{stringParameter}'", this);
                        return null;

                    case ParameterType.None:
                    default:
                        return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"UIClickCatcherCommandBinding: 파라미터 변환 실패 '{stringParameter}': {e.Message}", this);
                return null;
            }
        }
    }
}
